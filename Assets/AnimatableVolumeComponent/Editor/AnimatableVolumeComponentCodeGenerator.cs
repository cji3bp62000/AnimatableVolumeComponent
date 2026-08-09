using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

            public bool isNonPublic;
            public bool isNonAnimatableType;
        }

        /// <summary>
        /// Prefix of the generated "override state" field. This is a contract between the generated code and
        /// AnimatableVolumeComponentBaseEditor.
        /// </summary>
        public const string OverridePrefix = "override_";

        /// <summary>
        /// Prefix of the generated field that caches a non-public VolumeParameter resolved by reflection.
        /// </summary>
        private const string CachedPrefix = "cached_";

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
            var cacheMethodCode = "";
            // "how to reach the VolumeParameter" (public field / cached non-public field) and "is the value animatable"
            // are independent, so both are handled by the same code path instead of one branch per combination
            foreach (var fieldInfo in fieldInfoList) {
                // a non-public VolumeParameter is written through the instance we resolved by reflection
                var parameterCode = fieldInfo.isNonPublic
                    ? $"{CachedPrefix}{fieldInfo.helperFieldName}"
                    : $"volumeComponent.{fieldInfo.volumeComponentFieldName}";
                // reflection may fail (e.g. the field was renamed in another package version), so guard it
                var bodyIndent = fieldInfo.isNonPublic ? methodIndent + "    " : methodIndent;

                // field definition
                fieldDeclareCode += $"{fieldIndent}public bool {OverridePrefix}{fieldInfo.helperFieldName};\n";
                if (!fieldInfo.isNonAnimatableType) {
                    fieldDeclareCode += $"{fieldIndent}public {GetFullName(fieldInfo.valueType)} {fieldInfo.helperFieldName};\n";
                }
                if (fieldInfo.isNonPublic) {
                    var parameterTypeName = GetVolumeParameterTypeName(fieldInfo);
                    fieldDeclareCode += $"{fieldIndent}private {parameterTypeName} {parameterCode};\n";
                    cacheMethodCode +=
                        $"{methodIndent}{parameterCode} = AnimatableVolumeReflectionUtility\n"
                        + $"{methodIndent}    .GetNonPublicParameter<{parameterTypeName}>(volumeComponent, \"{fieldInfo.volumeComponentFieldName}\");\n";
                }

                // read from profile
                if (fieldInfo.isNonPublic) readMethodCode += $"{methodIndent}if ({parameterCode} != null) {{\n";
                readMethodCode +=
                    $"{bodyIndent}{OverridePrefix}{fieldInfo.helperFieldName} = {parameterCode}.overrideState;\n";
                if (!fieldInfo.isNonAnimatableType) {
                    readMethodCode += $"{bodyIndent}{fieldInfo.helperFieldName} = {parameterCode}.value;\n";
                }
                if (fieldInfo.isNonPublic) readMethodCode += $"{methodIndent}}}\n";

                // write to profile
                if (fieldInfo.isNonPublic) writeMethodCode += $"{methodIndent}if ({parameterCode} != null) {{\n";
                writeMethodCode +=
                    $"{bodyIndent}{parameterCode}.overrideState = {OverridePrefix}{fieldInfo.helperFieldName};\n";
                if (!fieldInfo.isNonAnimatableType) {
                    writeMethodCode += $"{bodyIndent}{parameterCode}.value = {fieldInfo.helperFieldName};\n";
                }
                if (fieldInfo.isNonPublic) writeMethodCode += $"{methodIndent}}}\n";
            }

            // no non-public parameter: don't emit the cache machinery at all
            var refreshCacheCode = cacheMethodCode == ""
                ? ""
                : $"{methodIndent}RefreshParameterCache(volumeComponent);";
            var cacheMethodDeclareCode = cacheMethodCode == ""
                ? ""
                : $"\n{fieldIndent}protected override void CacheNonPublicParameters(VolumeComponent volumeComponent)\n"
                + $"{fieldIndent}{{\n{cacheMethodCode}{fieldIndent}}}\n";

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
{refreshCacheCode}
            volumeComponent.active = active;
{writeMethodCode}
        }}

        private void ReadFromVolumeComponent({typeFullName} volumeComponent)
        {{
            if (!volumeComponent) return;
{refreshCacheCode}
            active = volumeComponent.active;
{readMethodCode}
        }}
{cacheMethodDeclareCode}
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

            // also search for NonPublic fields; the generated code reaches them by reflection
            // (same BindingFlags and ordering as VolumeComponent.FindParameters() of the core package, so the
            // parameters we find are exactly the ones VolumeManager blends)
            var fields = targetVolumeComponentType
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .OrderBy(t => t.MetadataToken); // Guaranteed order

            foreach (var field in fields) {
                if (!field.FieldType.IsSubclassOf(typeof(VolumeParameter))) continue;
                // skip obsolete fields
                if (field.GetCustomAttribute<ObsoleteAttribute>() != null) continue;
                // skip compiler generated fields (auto property backing fields); their names are not valid identifiers
                if (field.GetCustomAttribute<CompilerGeneratedAttribute>() != null) continue;

                var parameterValueType = GetVolumeParameterValueType(field.FieldType);
                // not derived from VolumeParameter<T>; we can't know the value type
                if (parameterValueType == null) continue;

                parameterFieldList.Add(new VolumeParameterFieldInfo()
                {
                    fieldType = field.FieldType,
                    valueType = parameterValueType,
                    volumeComponentFieldName = field.Name,
                    helperFieldName = field.Name,
                    isNonPublic = !field.IsPublic,
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
        /// Type name to declare the cached non-public VolumeParameter with. <br/>
        /// This is built from the value type, not from the actual field type: a field may be declared as a closed
        /// generic (VolumeParameter＜float＞), and Type.FullName of a closed generic is assembly qualified and does
        /// not compile. VolumeParameter＜T＞ has both "overrideState" and "value", so it is enough anyway.
        /// </summary>
        /// <param name="fieldInfo">VolumeParameter field</param>
        /// <returns>type name to write in the generated code</returns>
        private static string GetVolumeParameterTypeName(VolumeParameterFieldInfo fieldInfo)
        {
            return $"UnityEngine.Rendering.VolumeParameter<{GetFullName(fieldInfo.valueType)}>";
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
