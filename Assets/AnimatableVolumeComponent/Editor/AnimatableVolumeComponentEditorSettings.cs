using UnityEditor;
using UnityEngine;

namespace TsukimiNeko.AnimatableVolumeComponent.Internal
{
    /// <summary>
    /// Settings for internal Editor-use only.
    /// </summary>
    public class AnimatableVolumeComponentEditorSettings : ScriptableObject
    {
        private const string DefaultEditorFolder = "Assets/AnimatableVolumeComponent/Editor/";

        [Header("Editor Settings")]
        public bool isFirstImport = true;

        private const string Title_en = "Create Animatable Volume Component";
        private const string Title_jp = "Animatable Volume Component の作成";

        private const string Description_en =
            "Do you want to create Animatable Volume Components for animating?\n" +
            "\n" +
            "You can still create them later, from Tools > Animatable Volume > Animatable Volume Wizard.";
        private const string Description_jp =
            "アニメーション用の Animatable Volume Component を作成しますか？\n" +
            "\n" +
            "今作成しなくても、後で次のメニューからでも作成できます：\n" +
            "Tools > Animatable Volume > Animatable Volume Wizard";

        private const string Cancel_en = "Create them later";
        private const string Cancel_jp = "後で作成";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // sometimes it may cause error when doing something on importing, so wait 1 frame
            EditorApplication.update += NavigateOnFirstImport;
        }

        /// <summary>
        /// Ask user to create Animatable Volume Components on first import.
        /// </summary>
        private static void NavigateOnFirstImport()
        {
            EditorApplication.update -= NavigateOnFirstImport;

            var settings = GetOrCreateSettings();
            if (!settings) return;

            if (settings.isFirstImport) {
                settings.isFirstImport = false;
                EditorUtility.SetDirty(settings);

                var isJapanese = Application.systemLanguage == SystemLanguage.Japanese;
                var title = isJapanese ? Title_jp : Title_en;
                var description = isJapanese ? Description_jp : Description_en;
                var cancel = isJapanese ? Cancel_jp : Cancel_en;
                if (EditorUtility.DisplayDialog(title, description, "OK", cancel)) {
                    AnimatableVolumeWizard.ShowWindow();
                }
            }
        }

        private static AnimatableVolumeComponentEditorSettings GetOrCreateSettings()
        {
            string folder = null;
            if (AssetDatabase.IsValidFolder(DefaultEditorFolder)) {
                folder = DefaultEditorFolder;
            }
            else {
                var guids = AssetDatabase.FindAssets($"{nameof(AnimatableVolumeComponentEditorSettings)} t:script");
                foreach (var guid in guids) {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                    if (monoScript && monoScript.GetClass() == typeof(AnimatableVolumeComponentEditorSettings)) {
                        folder = assetPath.Substring(0, assetPath.LastIndexOf('/') + 1);
                    }
                }
            }

            if (folder == null) return null;

            var soPath = $"{folder}{nameof(AnimatableVolumeComponentEditorSettings)}.asset";
            var so = AssetDatabase.LoadAssetAtPath<AnimatableVolumeComponentEditorSettings>(soPath);
            if (so) return so;

            var instance = CreateInstance<AnimatableVolumeComponentEditorSettings>();
            AssetDatabase.CreateAsset(instance, soPath);
            instance = AssetDatabase.LoadAssetAtPath<AnimatableVolumeComponentEditorSettings>(soPath);
            return instance;
        }
    }
}
