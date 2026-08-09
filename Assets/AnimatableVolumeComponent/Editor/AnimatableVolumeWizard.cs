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

        /// <summary> color of a non-public VolumeParameter name </summary>
        private static readonly Color NonPublicColor = new Color(1f, 0.68f, 0.26f);
        /// <summary> color of the gray note text </summary>
        private static readonly Color NoteColor = new Color(0.6f, 0.6f, 0.6f);

        private List<VolumeComponentInfoItem> volumeComponentInfoList = new();

        /// <summary> localization flag; the list view is built before InitializeOtherUI(), so it is a field </summary>
        private bool isJapanese;

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

            isJapanese = Application.systemLanguage == SystemLanguage.Japanese;

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
            volumeComponentInfoList.Clear();

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

            InitializeParameterList();
        }

        /// <summary>
        /// Initialize the VolumeParameter list of each VolumeComponent, and decide which parameters are checked
        /// by default. <br/>
        /// For an already generated VolumeComponent, we restore the check state from the fields of the generated
        /// class, so that regenerating it without touching anything does not silently remove fields
        /// (which would break the animation clips that are bound to those field names).
        /// </summary>
        private void InitializeParameterList()
        {
            foreach (var volumeComponentInfo in volumeComponentInfoList) {
                var parameterFieldInfoList =
                    AnimatableVolumeComponentCodeGenerator.FindParameters(volumeComponentInfo.VolumeComponentType);

                // already generated: check only the parameters that exist in the generated class
                // not generated yet: null, which means "check them all"
                var generatedParameterNames = volumeComponentInfo.AnimatableComponentType != null
                    ? AnimatableVolumeComponentCodeGenerator.GetGeneratedParameterNames(
                        volumeComponentInfo.AnimatableComponentType)
                    : null;

                volumeComponentInfo.SetParameters(parameterFieldInfoList, generatedParameterNames);
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

                var paramParent = item.Q("ParamParent");
                itemInfo.SetUIFoldout(item.Q<Label>("FoldoutArrow"), paramParent);

                // the parameter selection buttons sit at the top of the parameter list
                if (itemInfo.ParameterList.Count > 0) {
                    itemInfo.SetUIParameterHeader(CreateParameterHeader(itemInfo));
                    paramParent.Add(itemInfo.parameterHeader);
                }
                // parameter rows are all created here, and just hidden until the row is expanded
                foreach (var parameterInfo in itemInfo.ParameterList) {
                    paramParent.Add(CreateParameterRow(parameterInfo));
                }
                // the rows exist only now, so the initial enabled state has to be applied here
                itemInfo.RefreshParameterEnabled();
            }
        }

        /// <summary>
        /// Create the header of the parameter list: the parameter selection buttons, and a note about
        /// how non-public VolumeParameters are written.
        /// </summary>
        private VisualElement CreateParameterHeader(VolumeComponentInfoItem itemInfo)
        {
            var header = new VisualElement();
            header.style.flexShrink = 0f;
            header.style.marginLeft = 32f; // indent by the foldout arrow (16px) and the check toggle (16px)

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.flexShrink = 0f;

            var selectPublicBtn = new Button(() => itemInfo.SelectParameters(publicOnly: true));
            selectPublicBtn.text = isJapanese ? "public のパラメータを選択" : "Select Public Parameters";
            var selectAllBtn = new Button(() => itemInfo.SelectParameters(publicOnly: false));
            selectAllBtn.text = isJapanese ? "全て選択" : "Select All";
            buttonRow.Add(selectPublicBtn);
            buttonRow.Add(selectAllBtn);
            header.Add(buttonRow);

            // the note only makes sense when there actually is a non-public VolumeParameter
            if (itemInfo.ParameterList.Any(parameter => parameter.FieldInfo.isNonPublic)) {
                var note = new Label(isJapanese
                    ? "※ オレンジ＝非 public。開始時に Reflection で取得・キャッシュした VolumeParameter に直接書き込みます"
                    : "* Orange = non-public. Resolved by reflection and cached at start,"
                    + " then written to the VolumeParameter directly.");
                note.style.color = NoteColor;
                note.style.fontSize = 10f;
                note.style.whiteSpace = WhiteSpace.Normal;
                note.style.paddingLeft = 3f;
                note.style.maxWidth = 500f;
                header.Add(note);
            }

            return header;
        }

        /// <summary>
        /// Create one VolumeParameter row (a toggle and its name).
        /// </summary>
        private VisualElement CreateParameterRow(VolumeParameterInfoItem parameterInfo)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.height = 16f;
            row.style.flexShrink = 0f;
            row.style.marginLeft = 32f; // indent by the foldout arrow (16px) and the check toggle (16px)

            var toggle = new Toggle();
            toggle.style.width = 16f;
            toggle.style.minWidth = 16f;
            toggle.style.maxWidth = 16f;

            var label = new Label(parameterInfo.ParameterName);
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.paddingLeft = 3f;
            if (parameterInfo.FieldInfo.isNonPublic) {
                // written through a VolumeParameter that is resolved by reflection and cached at start
                label.text += "  (non-public)";
                label.style.color = NonPublicColor;
            }
            if (parameterInfo.FieldInfo.isNonAnimatableType) {
                // only the override state can be generated for these; the value is not animatable
                label.text += "  (override only)";
            }

            row.Add(toggle);
            row.Add(label);
            parameterInfo.row = row;
            parameterInfo.SetUICheckToggle(toggle);
            return row;
        }

        /// <summary>
        /// Initialize the other UI elements, and localize.
        /// </summary>
        /// <param name="treeRoot"></param>
        private void InitializeOtherUI(VisualElement treeRoot)
        {
            SetElementEnable(treeRoot.Q("Description"), !isJapanese);
            SetElementEnable(treeRoot.Q("DescriptionJP"), isJapanese);

            var selectNotGeneratedBtn = treeRoot.Q<Button>("SelectNotGeneratedButton");
            selectNotGeneratedBtn.clicked += SelectNotGenerated;
            selectNotGeneratedBtn.text = isJapanese ? "未生成のみ選択" : "Select Not Generated";

            var selectAllBtn = treeRoot.Q<Button>("SelectAllButton");
            selectAllBtn.clicked += SelectAll;
            selectAllBtn.text = isJapanese ? "全て選択" : "Select All";

            var expandAllBtn = treeRoot.Q<Button>("ExpandAllButton");
            expandAllBtn.clicked += () => SetAllExpanded(true);
            expandAllBtn.text = isJapanese ? "全て展開" : "Expand All";

            var collapseAllBtn = treeRoot.Q<Button>("CollapseAllButton");
            collapseAllBtn.clicked += () => SetAllExpanded(false);
            collapseAllBtn.text = isJapanese ? "全て折りたたむ" : "Collapse All";

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

        private void SetAllExpanded(bool isExpanded)
        {
            foreach (var item in volumeComponentInfoList) {
                item.SetExpanded(isExpanded);
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
                    AnimatableVolumeComponentCodeGenerator.GenerateVolumeComponentHelperCode(
                        item.VolumeComponentType, item.GetSelectedParameterNames());
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
            /// <summary>
            /// Whether to generate this VolumeComponent in this run. <br/>
            /// This is independent from the parameter check states; checking a parameter does not check this,
            /// and checking this does not change the parameters.
            /// </summary>
            public bool IsChecked { get; private set; }
            public bool IsExpanded { get; private set; }
            public Type VolumeComponentType { get; private set; }
            public Type AnimatableComponentType { get; private set; }
            public List<VolumeParameterInfoItem> ParameterList { get; private set; } = new();

            // ui
            public Toggle checkedToggle;
            public Label vcLabel;
            public Image hasAcImage;
            public Label acLabel;
            public Label foldoutArrowLabel;
            public VisualElement parameterParent;
            public VisualElement parameterHeader;

            public VolumeComponentInfoItem(Type volumeComponentType)
            {
                VolumeComponentType = volumeComponentType;
            }

            public void SetIsChecked(bool isChecked)
            {
                IsChecked = isChecked;
                checkedToggle?.SetValueWithoutNotify(isChecked);
                RefreshParameterEnabled();
            }

            /// <summary>
            /// Set the VolumeParameter list, and decide the initial check state of each parameter.
            /// </summary>
            /// <param name="parameterFieldInfoList">parameters found in the VolumeComponent</param>
            /// <param name="generatedParameterNames">
            /// Parameters contained in the already generated AnimatableVolumeComponent.
            /// Pass null if it is not generated yet, then all parameters are checked.
            /// </param>
            public void SetParameters(
                List<AnimatableVolumeComponentCodeGenerator.VolumeParameterFieldInfo> parameterFieldInfoList,
                HashSet<string> generatedParameterNames)
            {
                ParameterList = parameterFieldInfoList
                    .Select(fieldInfo =>
                    {
                        var isGenerated = generatedParameterNames != null
                            && generatedParameterNames.Contains(fieldInfo.helperFieldName);
                        // not generated yet: check them all. already generated: check what is in the generated class
                        var isChecked = generatedParameterNames == null || isGenerated;
                        return new VolumeParameterInfoItem(fieldInfo, isChecked, isGenerated);
                    })
                    .ToList();
            }

            /// <summary>
            /// Number of VolumeParameters that are contained in the generated AnimatableVolumeComponent.
            /// </summary>
            public int GeneratedParameterCount => ParameterList.Count(parameter => parameter.IsGenerated);

            /// <summary>
            /// The parameter toggles can only be edited when this VolumeComponent is checked.
            /// </summary>
            public void RefreshParameterEnabled()
            {
                parameterHeader?.SetEnabled(IsChecked);
                foreach (var parameter in ParameterList) {
                    // disable the row, not the toggle, so that the parameter name is grayed out too
                    parameter.row?.SetEnabled(IsChecked);
                }
            }

            /// <summary>
            /// Check the VolumeParameters of this VolumeComponent.
            /// </summary>
            /// <param name="publicOnly">check only the public ones, and uncheck the non-public ones</param>
            public void SelectParameters(bool publicOnly)
            {
                foreach (var parameter in ParameterList) {
                    parameter.SetIsChecked(!publicOnly || !parameter.FieldInfo.isNonPublic);
                }
            }

            public HashSet<string> GetSelectedParameterNames()
            {
                return ParameterList.Where(parameter => parameter.IsChecked)
                    .Select(parameter => parameter.ParameterName)
                    .ToHashSet();
            }

            public void SetExpanded(bool isExpanded)
            {
                // a VolumeComponent without any parameter has nothing to expand
                if (ParameterList.Count == 0) isExpanded = false;

                IsExpanded = isExpanded;
                if (foldoutArrowLabel != null) {
                    foldoutArrowLabel.text = isExpanded ? "▼" : "▶";
                }
                if (parameterParent != null) {
                    parameterParent.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            public void SetAnimatableComponentType(Type animatableComponentType)
            {
                AnimatableComponentType = animatableComponentType;
                if (hasAcImage != null) {
                    hasAcImage.image = AnimatableComponentType != null ? okIcon.image : null;
                }

                if (acLabel != null) {
                    acLabel.text = GetAnimatableComponentLabelText();
                }
            }

            /// <summary>
            /// Name of the generated AnimatableVolumeComponent, with how many VolumeParameters it contains.
            /// </summary>
            private string GetAnimatableComponentLabelText()
            {
                if (AnimatableComponentType == null) return "";

                return $"{AnimatableComponentType.Name}  ({GeneratedParameterCount}/{ParameterList.Count})";
            }

            // ui
            public void SetUICheckToggle(Toggle toggle)
            {
                checkedToggle = toggle;
                toggle.SetValueWithoutNotify(IsChecked);
                toggle.RegisterValueChangedCallback(evt =>
                {
                    IsChecked = evt.newValue;
                    RefreshParameterEnabled();
                    // checking it means we are going to generate it, so show what will be generated.
                    // this is not in SetIsChecked(), otherwise "Select All" would expand every row at once
                    if (evt.newValue) SetExpanded(true);
                });
            }

            public void SetUIParameterHeader(VisualElement header)
            {
                parameterHeader = header;
                header.SetEnabled(IsChecked);
            }

            public void SetUIFoldout(Label arrowLabel, VisualElement paramParent)
            {
                foldoutArrowLabel = arrowLabel;
                parameterParent = paramParent;

                if (ParameterList.Count == 0) {
                    // keep the layout, but there is nothing to expand
                    arrowLabel.style.visibility = Visibility.Hidden;
                }
                RegisterFoldoutToggle(arrowLabel);

                SetExpanded(IsExpanded);
            }

            /// <summary>
            /// Make pressing the element toggle the foldout. <br/>
            /// Both the arrow and the VolumeComponent name column use this, so the whole left side of the row
            /// behaves like one foldout header.
            /// </summary>
            private void RegisterFoldoutToggle(VisualElement element)
            {
                // a VolumeComponent without any parameter has nothing to expand
                if (ParameterList.Count == 0) return;

                // PointerDownEvent instead of ClickEvent: a plain Label has no Clickable manipulator,
                // and reacting on press is how the built-in Foldout arrow behaves anyway
                element.RegisterCallback<PointerDownEvent>(_ => SetExpanded(!IsExpanded));
            }

            public void SetUIVolumeComponentLabel(Label label)
            {
                vcLabel = label;
                label.text = VolumeComponentType.Name;
                RegisterFoldoutToggle(label);
            }

            public void SetUIHasAcImage(Image image)
            {
                hasAcImage = image;
                hasAcImage.image = AnimatableComponentType != null ? okIcon.image : null;
            }

            public void SetUIAnimatableComponentLabel(Label label)
            {
                acLabel = label;
                label.text = GetAnimatableComponentLabelText();
            }
        }

        /// <summary>
        /// helper class to contain information of a VolumeParameter and its UI.
        /// </summary>
        public class VolumeParameterInfoItem
        {
            // data
            /// <summary>
            /// Whether to generate this VolumeParameter.
            /// </summary>
            public bool IsChecked { get; private set; }
            /// <summary>
            /// Whether this VolumeParameter is already contained in the generated AnimatableVolumeComponent.
            /// Unlike <see cref="IsChecked"/>, this does not change when the user toggles the checkbox.
            /// </summary>
            public bool IsGenerated { get; private set; }
            public AnimatableVolumeComponentCodeGenerator.VolumeParameterFieldInfo FieldInfo { get; private set; }

            public string ParameterName => FieldInfo.helperFieldName;

            // ui
            public VisualElement row;
            public Toggle checkedToggle;

            public VolumeParameterInfoItem(
                AnimatableVolumeComponentCodeGenerator.VolumeParameterFieldInfo fieldInfo,
                bool isChecked,
                bool isGenerated)
            {
                FieldInfo = fieldInfo;
                IsChecked = isChecked;
                IsGenerated = isGenerated;
            }

            public void SetIsChecked(bool isChecked)
            {
                IsChecked = isChecked;
                checkedToggle?.SetValueWithoutNotify(isChecked);
            }

            // ui
            public void SetUICheckToggle(Toggle toggle)
            {
                checkedToggle = toggle;
                toggle.SetValueWithoutNotify(IsChecked);
                toggle.RegisterValueChangedCallback(evt => IsChecked = evt.newValue);
            }
        }
    }

    #endregion
}
