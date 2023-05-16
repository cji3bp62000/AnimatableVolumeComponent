using System;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    /// <summary>
    /// Attribute to specify the type of the volume component that is animated by this class.
    /// </summary>
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
