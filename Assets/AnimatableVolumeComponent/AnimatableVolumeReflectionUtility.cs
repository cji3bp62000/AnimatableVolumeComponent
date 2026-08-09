using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    /// <summary>
    /// Reflection helper to access non-public VolumeParameter fields of a VolumeComponent. <br/>
    /// A non-public VolumeParameter can't be written by generated code directly, so we grab the VolumeParameter
    /// instance itself and write to it. The generated AnimatableVolumeComponent caches the returned instance,
    /// so reflection only happens when the runtime profile is (re)created.
    /// </summary>
    public static class AnimatableVolumeReflectionUtility
    {
        /// <summary> cached FieldInfo of (VolumeComponent type, field name) </summary>
        private static readonly Dictionary<(Type, string), FieldInfo> fieldInfoDic = new();

        /// <summary>
        /// Get a non-public VolumeParameter of a VolumeComponent.
        /// </summary>
        /// <param name="volumeComponent">target VolumeComponent</param>
        /// <param name="fieldName">name of the non-public VolumeParameter field</param>
        /// <typeparam name="T">VolumeParameter type</typeparam>
        /// <returns>found VolumeParameter, or null if it does not exist</returns>
        public static T GetNonPublicParameter<T>(VolumeComponent volumeComponent, string fieldName)
            where T : VolumeParameter
        {
            if (!volumeComponent) return null;

            var key = (volumeComponent.GetType(), fieldName);
            if (!fieldInfoDic.TryGetValue(key, out var fieldInfo)) {
                // may be null; we cache the "not found" result too, so we don't search it over and over
                fieldInfo = key.Item1.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                fieldInfoDic[key] = fieldInfo;
            }

            return fieldInfo?.GetValue(volumeComponent) as T;
        }
    }
}
