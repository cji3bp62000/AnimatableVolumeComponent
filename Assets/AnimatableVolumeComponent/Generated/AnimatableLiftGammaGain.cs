// <auto-generated />

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    [AnimatableOf(typeof(UnityEngine.Rendering.Universal.LiftGammaGain))]
    [ExecuteAlways]
    [RequireComponent(typeof(Volume)), RequireComponent(typeof(VolumeHelper))]
    [DisallowMultipleComponent]
    public class AnimatableLiftGammaGain : AnimatableVolumeComponentBase
    {
        public override Type TargetType { get; } = typeof(UnityEngine.Rendering.Universal.LiftGammaGain);

        public bool override_lift;
        public UnityEngine.Vector4 lift;
        public bool override_gamma;
        public UnityEngine.Vector4 gamma;
        public bool override_gain;
        public UnityEngine.Vector4 gain;


        private void WriteToVolumeComponent(UnityEngine.Rendering.Universal.LiftGammaGain volumeComponent)
        {
            if (!volumeComponent) return;

            volumeComponent.active = active;
            volumeComponent.lift.overrideState = override_lift;
            volumeComponent.lift.value = lift;
            volumeComponent.gamma.overrideState = override_gamma;
            volumeComponent.gamma.value = gamma;
            volumeComponent.gain.overrideState = override_gain;
            volumeComponent.gain.value = gain;

        }

        private void ReadFromVolumeComponent(UnityEngine.Rendering.Universal.LiftGammaGain volumeComponent)
        {
            if (!volumeComponent) return;

            active = volumeComponent.active;
            override_lift = volumeComponent.lift.overrideState;
            lift = volumeComponent.lift.value;
            override_gamma = volumeComponent.gamma.overrideState;
            gamma = volumeComponent.gamma.value;
            override_gain = volumeComponent.gain.overrideState;
            gain = volumeComponent.gain.value;

        }

        private void Reset()
        {
            var volume = GetComponent<Volume>();
            if (!volume || !volume.sharedProfile || !volume.sharedProfile.TryGet<UnityEngine.Rendering.Universal.LiftGammaGain>(out var volumeComponent)) return;

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
            if (!volumeHelper.TryGet<UnityEngine.Rendering.Universal.LiftGammaGain>(out var volumeComponent)) return;

            WriteToVolumeComponent(volumeComponent);
            ReadFromVolumeComponent(volumeComponent);
        }
    }
}