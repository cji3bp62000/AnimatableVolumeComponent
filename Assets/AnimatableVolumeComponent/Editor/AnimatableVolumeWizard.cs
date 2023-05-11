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
    public partial class AnimatableVolumeWizard : EditorWindow
    {
        private const string RefreshMappingCodeKey = "AnimatableVolumeWizard.RefreshMappingCodeKey";

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
            if (ShouldRefreshMappingCode()) {
                RefreshMappingCode();
            }
            // initialize UI
            InitializeListView(treeRoot);
            InitializeOtherUI(treeRoot);
        }

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

        private void InitializeOtherUI(VisualElement treeRoot)
        {
            var selectNotGeneratedBtn = treeRoot.Q<Button>("SelectNotGeneratedButton");
            selectNotGeneratedBtn.clicked += SelectNotGenerate;

            var generateBtn = treeRoot.Q<Button>("GenerateButton");
            generateBtn.clicked += GenerateCode;
        }

        private void SelectNotGenerate()
        {
            foreach (var item in volumeComponentInfoList) {
                if (item.AnimatableComponentType == null) {
                    item.SetIsChecked(true);
                }
            }
        }

        private void GenerateCode()
        {
            foreach (var item in volumeComponentInfoList) {
                if (item.IsChecked) {
                    AnimatableVolumeComponentCodeGenerator.GenerateVolumeComponentHelperCode(item.VolumeComponentType);
                }
            }
            AssetDatabase.Refresh();

            Debug.Log("Generate done!");
            RefreshMappingCode();
            EditorPrefs.SetBool(RefreshMappingCodeKey, true);

            foreach (var item in volumeComponentInfoList) {
                item.SetIsChecked(false);
            }
        }

        private bool ShouldRefreshMappingCode()
        {
            return EditorPrefs.GetBool(RefreshMappingCodeKey, false);
        }

        private void RefreshMappingCode()
        {
            EditorPrefs.DeleteKey(RefreshMappingCodeKey);
            var mapping = volumeComponentInfoList
                .Where(info => info.AnimatableComponentType != null)
                .ToDictionary(info => info.VolumeComponentType, info => info.AnimatableComponentType);
            AnimatableVolumeComponentCodeGenerator.GenerateMapCode(mapping);
            AssetDatabase.Refresh();
        }
    }

    #region Item class

    public partial class AnimatableVolumeWizard
    {
        public class VolumeComponentInfoItem
        {
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
