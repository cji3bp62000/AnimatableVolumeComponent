using System;
using UnityEngine;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    public abstract class AnimatableVolumeComponentBase : MonoBehaviour
    {
        public abstract Type TargetType { get; }
        [Header("Settings")]
        public bool writeValuesOnLateUpdate;

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
            if (!Application.isPlaying) return;
            if (writeValuesOnLateUpdate) {
                WriteToVolumeComponent();
            }
        }

        public abstract void WriteToVolumeComponent();
    }
}
