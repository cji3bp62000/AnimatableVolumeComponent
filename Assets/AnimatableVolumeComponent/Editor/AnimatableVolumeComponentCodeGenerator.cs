using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    /// <summary>
    /// Code generator for AnimatableVolumeComponent and mapping.
    /// </summary>
    public static class AnimatableVolumeComponentCodeGenerator
    {
        public class VolumeParameterFieldInfo
        {
            public Type fieldType;
            public Type valueType;
            // may also be property name, but I will just call it field name
            public string volumeComponentFieldName;
            public string helperFieldName;

            public bool isVolumeComponentProperty;
            public bool isNonAnimatableType;
        }

        /// <summary>
        /// Prefix of the generated "override state" field. This is a contract between the generated code and
        /// AnimatableVolumeComponentBaseEditor.
        /// </summary>
        public const string OverridePrefix = "override_";

        /// <summary>
        /// Types that can't be animated (due to technical limitations)
        /// </summary>
        private static readonly string[] NonAnimatableTypeNames = new[]
        {
            "TextureCurve",
        };

        /// <summary>
        /// Generate AnimatableVolumeComponent for the given VolumeComponent type.
        /// </summary>
        /// <param name="type">VolumeComponent type</param>
        /// <param name="selectedParameterNames">
        /// Names of the VolumeParameters to generate (<see cref="VolumeParameterFieldInfo.helperFieldName"/>).
        /// Pass null to generate all of them.
        /// </param>
        public static void GenerateVolumeComponentHelperCode(Type type, ICollection<string> selectedParameterNames = null)
        {
            var vcType = typeof(VolumeComponent);
            if (!vcType.IsAssignableFrom(type)) {
                Debug.LogError($"{type.Name} does not inherit from {vcType.Name}!");
                return;
            }

            var fieldInfoList = FindParameters(type);
            if (selectedParameterNames != null) {
                fieldInfoList = fieldInfoList
                    .Where(fieldInfo => selectedParameterNames.Contains(fieldInfo.helperFieldName))
                    .ToList();
            }

            var fieldIndent = "        ";
            var methodIndent = "            ";
            var fieldDeclareCode = "";
            var readMethodCode = "";
            var writeMethodCode = "";
            foreach (var fieldInfo in fieldInfoList) {
                // code generation for special case
                if (fieldInfo.isVolumeComponentProperty) {
                    // property only; we can't set the override value
                    fieldDeclareCode += $"{fieldIndent}public {GetFullName(fieldInfo.valueType)} {fieldInfo.helperFieldName};\n";
                    // strip the ".value" part
                    readMethodCode +=
                        $"{methodIndent}{fieldInfo.helperFieldName} = volumeComponent.{fieldInfo.volumeComponentFieldName};\n";
                    writeMethodCode +=
                        $"{methodIndent}volumeComponent.{fieldInfo.volumeComponentFieldName} = {fieldInfo.helperFieldName};\n";
                    continue;
                }
                else if (fieldInfo.isNonAnimatableType) {
                    // override state only; we can't set the value of non animatable types
                    fieldDeclareCode += $"{fieldIndent}public bool {OverridePrefix}{fieldInfo.helperFieldName};\n";
                    readMethodCode +=
                        $"{methodIndent}{OverridePrefix}{fieldInfo.helperFieldName} = volumeComponent.{fieldInfo.volumeComponentFieldName}.overrideState;\n";
                    writeMethodCode +=
                        $"{methodIndent}volumeComponent.{fieldInfo.volumeComponentFieldName}.overrideState = {OverridePrefix}{fieldInfo.helperFieldName};\n";
                    continue;
                }

                // normal code generation
                // field definition
                fieldDeclareCode += $"{fieldIndent}public bool {OverridePrefix}{fieldInfo.helperFieldName};\n";
                fieldDeclareCode += $"{fieldIndent}public {GetFullName(fieldInfo.valueType)} {fieldInfo.helperFieldName};\n";
                // read from profile
                readMethodCode +=
                    $"{methodIndent}{OverridePrefix}{fieldInfo.helperFieldName} = volumeComponent.{fieldInfo.volumeComponentFieldName}.overrideState;\n";
                readMethodCode +=
                    $"{methodIndent}{fieldInfo.helperFieldName} = volumeComponent.{fieldInfo.volumeComponentFieldName}.value;\n";
                // write to profile
                writeMethodCode +=
                    $"{methodIndent}volumeComponent.{fieldInfo.volumeComponentFieldName}.overrideState = {OverridePrefix}{fieldInfo.helperFieldName};\n";
                writeMethodCode +=
                    $"{methodIndent}volumeComponent.{fieldInfo.volumeComponentFieldName}.value = {fieldInfo.helperFieldName};\n";
            }

            var typeFullName = GetFullName(type);

            var generatedCode = $@"// <auto-generated />

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{{
    [AnimatableOf(typeof({typeFullName}))]
    [DisallowMultipleComponent]
    public class Animatable{type.Name} : AnimatableVolumeComponentBase
    {{
        public override Type TargetType {{ get; }} = typeof({typeFullName});

{fieldDeclareCode}

        private void WriteToVolumeComponent({typeFullName} volumeComponent)
        {{
            if (!volumeComponent) return;

            volumeComponent.active = active;
{writeMethodCode}
        }}

        private void ReadFromVolumeComponent({typeFullName} volumeComponent)
        {{
            if (!volumeComponent) return;

            active = volumeComponent.active;
{readMethodCode}
        }}

        private void Reset()
        {{
            var volume = GetComponent<Volume>();
            if (!volume || !volume.sharedProfile || !volume.sharedProfile.TryGet<{typeFullName}>(out var volumeComponent)) return;

            ReadFromVolumeComponent(volumeComponent);
        }}

        public override void WriteToVolumeComponent()
        {{
            if (!volumeHelper.TryGet<{typeFullName}>(out var volumeComponent)) return;

            WriteToVolumeComponent(volumeComponent);
        }}

        public override void ReadFromVolumeComponent()
        {{
            if (!volumeHelper.TryGet<{typeFullName}>(out var volumeComponent)) return;

            ReadFromVolumeComponent(volumeComponent);
        }}
    }}
}}
";

            // 生成したコードをファイルに書き込む
            var path = $"{Application.dataPath}/AnimatableVolumeComponent/Generated/";
            var fileName = $"Animatable{type.Name}.cs";
            System.IO.Directory.CreateDirectory(path);
            Debug.Log($"writing to {path + fileName}");
            System.IO.File.WriteAllText(path + fileName, generatedCode);
        }

        /// <summary>
        /// One-layer-only VolumeParameter field searching. <br/>
        /// This is the single source of the parameter list; the wizard uses it to show the parameter toggles,
        /// so the order here also decides the order of the generated fields.
        /// </summary>
        /// <param name="targetVolumeComponentType"></param>
        /// <returns>found VolumeParameter fields, in declaration order</returns>
        public static List<VolumeParameterFieldInfo> FindParameters(Type targetVolumeComponentType)
        {
            var parameterFieldList = new List<VolumeParameterFieldInfo>();

            // also search for NonPublic fields; if there is corresponding public property, we can use it
            var fields = targetVolumeComponentType
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .OrderBy(t => t.MetadataToken); // Guaranteed order
            var properties = targetVolumeComponentType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();

            foreach (var field in fields) {
                if (!field.FieldType.IsSubclassOf(typeof(VolumeParameter))) continue;
                // skip obsolete fields
                if (field.GetCustomAttribute<ObsoleteAttribute>() != null) continue;

                var parameterValueType = GetVolumeParameterValueType(field.FieldType);
                // not derived from VolumeParameter<T>; we can't know the value type
                if (parameterValueType == null) continue;

                var volumeComponentMemberName = field.Name;
                if (field.IsPublic == false) {
                    if (TryFindCorrespondingProperty(field, parameterValueType, properties, out var property)) {
                        volumeComponentMemberName = property.Name;
                    }
                    else {
                        // field not public and no corresponding property found; we can't access it
                        continue;
                    }
                }
                parameterFieldList.Add(new VolumeParameterFieldInfo()
                {
                    fieldType = field.FieldType,
                    valueType = parameterValueType,
                    volumeComponentFieldName = volumeComponentMemberName,
                    helperFieldName = field.Name,
                    isVolumeComponentProperty = !field.IsPublic,
                    isNonAnimatableType = NonAnimatableTypeNames.Contains(parameterValueType.Name),
                });
            }

            return parameterFieldList;
        }

        /// <summary>
        /// Find which VolumeParameters are contained in an already generated AnimatableVolumeComponent,
        /// by looking at its fields. <br/>
        /// Note that a parameter may be generated as "override_xxx" only (non animatable type) or as "xxx" only
        /// (VolumeComponent property), so both forms count as "contained".
        /// </summary>
        /// <param name="animatableComponentType">generated AnimatableVolumeComponent type</param>
        /// <returns>contained VolumeParameter names</returns>
        public static HashSet<string> GetGeneratedParameterNames(Type animatableComponentType)
        {
            var parameterNames = new HashSet<string>();
            if (animatableComponentType == null) return parameterNames;

            // DeclaredOnly: we only want the generated fields, not "active" etc. of the base class
            var fields = animatableComponentType
                .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var field in fields) {
                var parameterName = field.Name;
                if (parameterName.StartsWith(OverridePrefix)) {
                    parameterName = parameterName.Substring(OverridePrefix.Length);
                }
                parameterNames.Add(parameterName);
            }
            return parameterNames;
        }

        /// <summary>
        /// Get "SomeType" from VolumeParameter＜SomeType＞ (or child class of VolumeParameter)
        /// </summary>
        /// <param name="volumeParameterType"></param>
        /// <returns>containing type</returns>
        private static Type GetVolumeParameterValueType(Type volumeParameterType)
        {
            var directChildType = volumeParameterType;
            while (!directChildType.IsGenericType || directChildType.GetGenericTypeDefinition() != typeof(VolumeParameter<>)) {
                directChildType = directChildType.BaseType;
                if (directChildType == null) {
                    return null;
                }
            }
            return directChildType.GetGenericArguments()[0];
        }

        /// <summary>
        /// If there is corresponding public property of field, return it.
        /// We can't precisely know if there is a corresponding property, so the result may be wrong. <br/>
        /// This check is mainly for HDRP default VolumeComponents that have private Parameter fields but corresponding
        /// public Properties.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="fieldParameterValueType"></param>
        /// <param name="properties"></param>
        /// <param name="correspondingProperty"></param>
        /// <returns></returns>
        private static bool TryFindCorrespondingProperty(
            FieldInfo field,
            Type fieldParameterValueType,
            PropertyInfo[] properties,
            out PropertyInfo correspondingProperty)
        {
            correspondingProperty = null;
            var lowerCaseFieldName = field.Name.ToLower();
            foreach (var property in properties) {
                if (property.PropertyType != fieldParameterValueType) continue;
                var checkName = "m_" + property.Name.ToLower();
                if (lowerCaseFieldName == checkName) {
                    correspondingProperty = property;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Generate Mapping
        /// </summary>
        /// <param name="vcAvcMap"></param>
        /// <param name="vcList"></param>
        public static void GenerateMapCode(Dictionary<Type, Type> vcAvcMap, List<Type> vcList = null)
        {
            // existing Animatable Component map
            var mapDicItemCode = string.Join(
                "\n",
                vcAvcMap.Select(kvp => $"            Map[typeof({GetFullName(kvp.Key)})] = typeof({GetFullName(kvp.Value)});")
            );
            // just generated (not compiled) Animatable Component's original VolumeComponent list
            var listItemCode = "";
            if (vcList != null) {
                foreach (var type in vcList.Except(vcAvcMap.Keys)) {
                    var avcTypeFullName = $"TsukimiNeko.AnimatableVolumeComponent.Animatable{type.Name}";
                    listItemCode += $"            Map[typeof({GetFullName(type)})] = typeof({avcTypeFullName});\n";
                }
            }

            var generatedCode = $@"// <auto-generated />

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TsukimiNeko.AnimatableVolumeComponent
{{
    public static partial class AnimatableVolumeComponentMapping
    {{
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeMap()
        {{
{mapDicItemCode}
{listItemCode}
        }}
    }}
}}
";

            var path = $"{Application.dataPath}/AnimatableVolumeComponent/Generated/";
            var fileName = "AnimatableVolumeComponentMapping.g.cs";
            System.IO.Directory.CreateDirectory(path);
            Debug.Log($"writing to {path + fileName}");
            System.IO.File.WriteAllText(path + fileName, generatedCode);
        }

        /// <summary>
        /// Get full name of type, replacing "+" with "."
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string GetFullName(Type type)
        {
            return type.FullName.Replace("+", ".");
        }
    }
}
