using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    [ExecuteAlways]
    [RequireComponent(typeof(Volume)), RequireComponent(typeof(VolumeHelper))]
    public abstract class AnimatableVolumeComponentBase : MonoBehaviour
    {
        public abstract Type TargetType { get; }

        public bool active;

        protected VolumeHelper volumeHelper
        {
            get {
                if (!_cachedVolumeHelper) _cachedVolumeHelper = GetComponent<VolumeHelper>();
                return _cachedVolumeHelper;
            }
        }
        private VolumeHelper _cachedVolumeHelper;

        protected virtual void LateUpdate()
        {
#if UNITY_EDITOR
            // stop writing, only when reading from profile
            if (volumeHelper.editorSyncProfileToAnimatable) return;
#endif

            WriteToVolumeComponentAndRead();
        }

        private void OnValidate()
        {
            // if no runtime profile, create one
            volumeHelper.CreateRuntimeProfile();
            WriteToVolumeComponentAndRead();
        }

        public abstract void WriteToVolumeComponentAndRead();
        public abstract void ReadFromVolumeComponent();
    }
}
