using System.IO;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    static class Resources
    {
        const string k_TemplateRoot = "Packages/com.unity.localization/Editor/UI/Templates";
        const string k_StyleRoot = "Packages/com.unity.localization/Editor/UI/Templates";

        static string StyleSheetPath(string filename) => $"{k_StyleRoot}/{filename}.uss";

        static string TemplatePath(string filename) => $"{k_TemplateRoot}/{filename}.uxml";

        public static VisualTreeAsset GetTemplateAsset(string templateFilename)
        {
            var path = TemplatePath(templateFilename);

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);

            if (asset == null)
                throw new FileNotFoundException("Failed to load UI Template at path " + path);
            return asset;
        }

        public static StyleSheet GetStyleSheetAsset(string name)
        {
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath(name));
        }

        public static VisualElement GetTemplate(string templateFilename)
        {
            return GetTemplateAsset(templateFilename).CloneTree();
        }
    }
}
