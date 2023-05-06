using System;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AnimatableOfAttribute : Attribute
    {
        public readonly Type volumeComponentType;

        public AnimatableOfAttribute(Type volumeComponentType)
        {
            this.volumeComponentType = volumeComponentType;
        }
    }
}
