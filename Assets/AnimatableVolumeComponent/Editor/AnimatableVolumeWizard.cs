using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    /// <summary>
    /// Wizard for auto generating AnimatableVolumeComponent.
    /// </summary>
    public partial class AnimatableVolumeWizard : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        [SerializeField]
        private VisualTreeAsset m_ListItemAsset = default;

        private List<VolumeComponentInfoItem> volumeComponentInfoList = new();

        [MenuItem("Tools/Animatable Volume/Animatable Volume Wizard")]
        public static void ShowWindow()
        {
            AnimatableVolumeWizard wnd = GetWindow<AnimatableVolumeWizard>();
            wnd.titleContent = new GUIContent("Animatable Volume Wizard");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Instantiate UXML
            VisualElement treeRoot = m_VisualTreeAsset.Instantiate();
            root.Add(treeRoot);

            // initialize data
            InitializeList();

            // initialize UI
            InitializeListView(treeRoot);
            InitializeOtherUI(treeRoot);
        }

        /// <summary>
        /// Check the codebase, and initialize the list of existing VolumeComponents and corresponding AnimatableVolumeComponents.
        /// </summary>
        private void InitializeList()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var volumeComponents = assemblies.SelectMany(assembly => assembly.GetTypes()
                    .Where(myType => myType.IsClass
                        && !myType.IsAbstract
                        && myType.IsSubclassOf(typeof(VolumeComponent))
                        && (myType.IsPublic || myType.Assembly.FullName == "Assembly-CSharp")))
                .ToList();

            volumeComponentInfoList.AddRange(volumeComponents.Select(vc => new VolumeComponentInfoItem(vc)));

            var animatableComponents = assemblies.SelectMany(assembly => assembly.GetTypes()
                    .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(AnimatableVolumeComponentBase))))
                .ToList();

            var animatedVolumeComponentDic = volumeComponents
                .Join(animatableComponents,
                    vc => vc,
                    ac => ac.GetCustomAttribute<AnimatableOfAttribute>()?.volumeComponentType,
                    (vc, ac) => (vc, ac))
                .ToDictionary(pair => pair.vc, pair => pair.ac);
            foreach (var volumeComponentInfo in volumeComponentInfoList) {
                var vc = volumeComponentInfo.VolumeComponentType;
                if (!animatedVolumeComponentDic.TryGetValue(vc, out var avc)) continue;

                volumeComponentInfo.SetAnimatableComponentType(avc);
            }
        }

        /// <summary>
        /// Initialize the list view of VolumeComponents.
        /// </summary>
        private void InitializeListView(VisualElement treeRoot)
        {
            var listParent = treeRoot.Q("ListParent");
            foreach (var itemInfo in volumeComponentInfoList) {
                var item = m_ListItemAsset.Instantiate();
                listParent.Add(item);
                itemInfo.SetUICheckToggle(item.Q<Toggle>("Check"));
                itemInfo.SetUIVolumeComponentLabel(item.Q<Label>("VcName"));
                itemInfo.SetUIHasAcImage(item.Q<Image>("IsAnimatable"));
                itemInfo.SetUIAnimatableComponentLabel(item.Q<Label>("AcName"));
            }
        }

        /// <summary>
        /// Initialize the other UI elements, and localize.
        /// </summary>
        /// <param name="treeRoot"></param>
        private void InitializeOtherUI(VisualElement treeRoot)
        {
            var isJapanese = Application.systemLanguage == SystemLanguage.Japanese;

            SetElementEnable(treeRoot.Q("Description"), !isJapanese);
            SetElementEnable(treeRoot.Q("DescriptionJP"), isJapanese);

            var selectNotGeneratedBtn = treeRoot.Q<Button>("SelectNotGeneratedButton");
            selectNotGeneratedBtn.clicked += SelectNotGenerated;
            selectNotGeneratedBtn.text = isJapanese ? "未生成のみ選択" : "Select Not Generated";

            var selectAllBtn = treeRoot.Q<Button>("SelectAllButton");
            selectAllBtn.clicked += SelectAll;
            selectAllBtn.text = isJapanese ? "全て選択" : "Select All";

            var generateBtn = treeRoot.Q<Button>("GenerateButton");
            generateBtn.clicked += GenerateCode;
            generateBtn.text = isJapanese ? "Animatable Component を生成" : "Generate Animatable Component";
        }

        private void SetElementEnable(VisualElement ve, bool isEnable)
        {
            ve.style.display = isEnable ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SelectNotGenerated()
        {
            foreach (var item in volumeComponentInfoList) {
                if (item.AnimatableComponentType == null) {
                    item.SetIsChecked(true);
                }
            }
        }

        private void SelectAll()
        {
            foreach (var item in volumeComponentInfoList) {
                item.SetIsChecked(true);
            }
        }

        /// <summary>
        /// Generate AnimatableVolumeComponent code.
        /// </summary>
        private void GenerateCode()
        {
            List<Type> generatedVolumeComponentList = new();
            foreach (var item in volumeComponentInfoList) {
                if (item.IsChecked) {
                    AnimatableVolumeComponentCodeGenerator.GenerateVolumeComponentHelperCode(item.VolumeComponentType);
                    generatedVolumeComponentList.Add(item.VolumeComponentType);
                }
            }
            RefreshMappingCode(generatedVolumeComponentList);
            AssetDatabase.Refresh();

            Debug.Log("Generate done!");

            foreach (var item in volumeComponentInfoList) {
                item.SetIsChecked(false);
            }
        }

        private void RefreshMappingCode(List<Type> justGeneratedVolumeComponentList)
        {
            var mapping = volumeComponentInfoList
                .Where(info => info.AnimatableComponentType != null)
                .ToDictionary(info => info.VolumeComponentType, info => info.AnimatableComponentType);
            AnimatableVolumeComponentCodeGenerator.GenerateMapCode(mapping, justGeneratedVolumeComponentList);
        }
    }

    #region Item class

    public partial class AnimatableVolumeWizard
    {
        /// <summary>
        /// helper class to contain information of types and UIs.
        /// </summary>
        public class VolumeComponentInfoItem
        {
            // checkmark icon
            private static readonly GUIContent okIcon = EditorGUIUtility.IconContent("TestPassed");

            // data
            public bool IsChecked => checkedToggle.value;
            public Type VolumeComponentType { get; private set; }
            public Type AnimatableComponentType { get; private set; }

            // ui
            public Toggle checkedToggle;
            public Label vcLabel;
            public Image hasAcImage;
            public Label acLabel;

            public VolumeComponentInfoItem(Type volumeComponentType)
            {
                VolumeComponentType = volumeComponentType;
            }

            public void SetIsChecked(bool isChecked)
            {
                checkedToggle.value = isChecked;
            }

            public void SetAnimatableComponentType(Type animatableComponentType)
            {
                AnimatableComponentType = animatableComponentType;
                if (hasAcImage != null) {
                    hasAcImage.image = AnimatableComponentType != null ? okIcon.image : null;
                }

                if (acLabel != null) {
                    acLabel.text = AnimatableComponentType != null ? AnimatableComponentType.Name : "";
                }
            }

            // ui
            public void SetUICheckToggle(Toggle toggle)
            {
                checkedToggle = toggle;
                toggle.value = IsChecked;
            }

            public void SetUIVolumeComponentLabel(Label label)
            {
                vcLabel = label;
                label.text = VolumeComponentType.Name;
            }

            public void SetUIHasAcImage(Image image)
            {
                hasAcImage = image;
                hasAcImage.image = AnimatableComponentType != null ? okIcon.image : null;
            }

            public void SetUIAnimatableComponentLabel(Label label)
            {
                acLabel = label;
                label.text = AnimatableComponentType?.Name;
            }
        }
    }

    #endregion
}
