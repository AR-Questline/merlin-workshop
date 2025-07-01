using System;

namespace UnityEngine.Localization.PropertyVariants
{
    /// <summary>
    /// Indicates that the class is used to Track Property Variants for a particular Type.
    /// </summary>
    /// <example>
    /// This shows how to create a custom <see cref="TrackedObjects.TrackedObject"/> to support the <see cref="AudioSource"/> component.
    /// <code source="../../DocCodeSamples.Tests/GameObjectLocalizerSamples.cs" region="custom-audio"/>
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomTrackedObjectAttribute : Attribute
    {
        internal Type ObjectType { get; }
        internal bool SupportsInheritedTypes { get; }

        /// <summary>
        /// Creates a new instance of a CustomTrackedObjectAttribute.
        /// </summary>
        /// <param name="type">The Type of Object that this Tracked Object supports.</param>
        /// <param name="supportsInheritedTypes">Does this class also support types that inherit from <paramref name="type"/>?</param>
        public CustomTrackedObjectAttribute(Type type, bool supportsInheritedTypes)
        {
            ObjectType = type;
            SupportsInheritedTypes = supportsInheritedTypes;
        }
    }
}
