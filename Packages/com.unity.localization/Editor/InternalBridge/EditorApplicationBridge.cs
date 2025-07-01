#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

namespace UnityEditor.Localization.Bridge
{
    static class EditorApplicationBridge
    {
        public static void CallDelayFunctions() => EditorApplication.Internal_CallDelayFunctions();
    }
}

#endif
