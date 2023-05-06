// <auto-generated />

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    [AnimatableOf(typeof(UnityEngine.Rendering.Universal.WhiteBalance))]
    [ExecuteAlways]
    [RequireComponent(typeof(Volume)), RequireComponent(typeof(VolumeHelper))]
    [DisallowMultipleComponent]
    public class AnimatableWhiteBalance : AnimatableVolumeComponentBase
    {
        public override Type TargetType { get; } = typeof(UnityEngine.Rendering.Universal.WhiteBalance);

        public bool override_temperature;
        public System.Single temperature;
        public bool override_tint;
        public System.Single tint;


        private void WriteToVolumeComponent(UnityEngine.Rendering.Universal.WhiteBalance volumeComponent)
        {
            if (!volumeComponent) return;

            volumeComponent.active = active;
            volumeComponent.temperature.overrideState = override_temperature;
            volumeComponent.temperature.value = temperature;
            volumeComponent.tint.overrideState = override_tint;
            volumeComponent.tint.value = tint;

        }

        private void ReadFromVolumeComponent(UnityEngine.Rendering.Universal.WhiteBalance volumeComponent)
        {
            if (!volumeComponent) return;

            active = volumeComponent.active;
            override_temperature = volumeComponent.temperature.overrideState;
            temperature = volumeComponent.temperature.value;
            override_tint = volumeComponent.tint.overrideState;
            tint = volumeComponent.tint.value;

        }

        private void Reset()
        {
            var volume = GetComponent<Volume>();
            if (!volume || !volume.sharedProfile || !volume.sharedProfile.TryGet<UnityEngine.Rendering.Universal.WhiteBalance>(out var volumeComponent)) return;

            ReadFromVolumeComponent(volumeComponent);
        }

        private void OnValidate()
        {
            WriteToVolumeComponent();
        }

        private void OnDidApplyAnimationProperties()
        {
            WriteToVolumeComponent();
        }

        public override void WriteToVolumeComponent()
        {
            if (!volumeHelper.TryGet<UnityEngine.Rendering.Universal.WhiteBalance>(out var volumeComponent)) return;

            WriteToVolumeComponent(volumeComponent);
            ReadFromVolumeComponent(volumeComponent);
        }
    }
}