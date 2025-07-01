namespace UnityEngine.Localization.Bridge
{
    /// <summary>
    /// The Driven Property Manager is used to mark serialized properties as driven.
    /// When a property is marked as driven a snapshot is taken of its current value.
    /// Any subsequent changes made will be recorded however they will not be saved into the asset.
    /// When the property is unregistered it is reverted back to its original value and future changes will be saved.
    /// </summary>
    internal class DrivenPropertyManagerBridge
    {
        public static void RegisterProperty(Object driver, Object target, string propertyPath)
        {
            #if UNITY_2020_1_OR_NEWER
            // Safer version that does not throw errors if a property is missing.
            DrivenPropertyManager.TryRegisterProperty
            #else
            DrivenPropertyManager.RegisterProperty
            #endif
                (driver, target, propertyPath);
        }

        // Same as RegisterProperty but produces an error if the property could not be found.
        //public static void TryRegisterProperty(Object driver, Object target, string propertyPath) => DrivenPropertyManager.TryRegisterProperty(driver, target, propertyPath);
        public static void UnregisterProperty(Object driver, Object target, string propertyPath) => DrivenPropertyManager.UnregisterProperty(driver, target, propertyPath);
        public static void UnregisterProperties(Object driver) => DrivenPropertyManager.UnregisterProperties(driver);
    }
}
