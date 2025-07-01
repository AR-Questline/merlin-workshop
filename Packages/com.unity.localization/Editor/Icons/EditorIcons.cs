using UnityEngine;

namespace UnityEditor.Localization
{
    class EditorIcons
    {
        public static Texture2D AssetTableCollection { get; private set; }
        public static Texture2D Csv { get; private set; }
        public static Texture2D GameObjectLocalizer { get; private set; }
        public static Texture2D GoogleSheets { get; private set; }
        public static Texture2D Locale { get; private set; }
        public static Texture2D Metadata { get; private set; }
        public static Texture2D StringTableCollection { get; private set; }
        public static Texture2D TableWindow { get; private set; }
        public static Texture2D WarningIcon { get; private set; }

        public static GUIStyle SelectionRect { get; private set; }

        static EditorIcons()
        {
            AssetTableCollection = GetTexture("Asset Table Collection/AssetTableCollection.png");
            Csv = GetTexture("CSV Extension/CSVExtension.png");
            GameObjectLocalizer = GetTexture("Game Object Localizer/GameObjectLocalizer.png");
            GoogleSheets = GetTexture("Google Sheets Provider/GoogleSheetsProvider.png");
            Locale = GetTexture("Locale/Locale.png");
            Metadata = GetTexture("Metadata/Metadata.png");
            TableWindow = GetTexture("Localization Tables Window/LocalizationTablesWindow.png");
            StringTableCollection = GetTexture("String Table Collection/StringTableCollection.png");
            WarningIcon = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;

            SelectionRect = new GUIStyle { name = "selectionrect" };
            SelectionRect.border = new RectOffset(2, 2, 2, 2);
            SelectionRect.normal.background = GetTexture("SelectionRect/selectionrect.png");
        }

        static Texture2D GetTexture(string path) => EditorGUIUtility.FindTexture("Packages/com.unity.localization/Editor/Icons/" + path);
    }
}
