using Awaken.Utility.Enums;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Awaken.Utility {
    /// <summary>
    /// Colors we are using in UIs
    /// </summary>
    public class ARColor : RichEnum {
        public const string EditorLightYellow = "#FFE785";
        public const string EditorLightBlue = "#D3F7FF";
        public const string EditorLightBrown = "#C3A296";
        public const string EditorMediumBlue = "#BAE3EA";
        public const string EditorDarkBlue = "#2874A6";
        public const string EditorMediumGreen = "#9AD4BC";
        public const string EditorLightRed = "#FF8A89";
        
        public Color Color { get; }
        public Color32 Color32 { [UnityEngine.Scripting.Preserve] get; }
        public string Hex { get; }
        
        protected ARColor(string enumName, Color color) : base(enumName) {
            Color = color;
            Color32 = color;
            Hex = ColorUtility.ToHtmlStringRGB(Color);
        }
        protected ARColor(string enumName, Color32 color) : base(enumName) {
            Color = color;
            Color32 = color;
            Hex = ColorUtility.ToHtmlStringRGB(Color);
        }

        [UnityEngine.Scripting.Preserve]
        public static readonly ARColor
            // --- Main
            MainGrey = new(nameof(MainGrey), new Color32(0xA1, 0xA1, 0xA1, 0xFF)),
            DarkerGrey = new(nameof(DarkerGrey), new Color32(0x4E, 0x4E, 0x4E, 0xFF)),
            LightGrey = new(nameof(LightGrey), new Color32(0xD9, 0xD9, 0xD9, 0xFF)),
            BlackBackground = new(nameof(BlackBackground), new Color32(0x0E, 0x0E, 0x0E, 0xFF)),
            FullBlack = new(nameof(FullBlack), new Color32(0x00, 0x00, 0x00, 0xFF)),
            MainBlack = new(nameof(MainBlack), new Color32(0x06, 0x06, 0x06, 0xFF)),
            SecondaryBlack = new(nameof(SecondaryBlack), new Color32(0x0A, 0x0A, 0x0A, 0xFF)),
            MainWhite = new(nameof(MainWhite), new Color32(0xFF, 0xFF, 0xFF, 0xFF)),
            MainAccent = new(nameof(MainAccent), new Color32(0xCA, 0x7F, 0x30, 0xFF)),
            DarkerMainAccent = new(nameof(DarkerMainAccent), new Color32(0xA5, 0x69, 0x00, 0xFF)),
            SecondaryAccent = new(nameof(SecondaryAccent), new Color32(0xB7, 0x80, 0x47, 0xFF)),
            SpecialAccent = new(nameof(SpecialAccent), new Color32(0x04, 0x95, 0x9A, 0xFF)),
            MainGreen = new(nameof(MainGreen), new Color32(0x4C, 0x7A, 0x48, 0xFF)),
            MainRed = new(nameof(MainRed), new Color32(0xAC, 0x2E, 0x2E, 0xFF)),
            SubtitlesYellow = new(nameof(SubtitlesYellow), new Color32(255, 214, 0, 255)),
            ChoiceDisableText = new(nameof(ChoiceDisableText), new Color32(0x70, 0x70, 0x70, 0xFF)),
            Transparent = new(nameof(Transparent), new Color32(0, 0, 0, 0)),

            // --- Quality
            QualityGarbage = new(nameof(QualityGarbage), new Color32(0x4E, 0x4E, 0x4E, 0xFF)),
            QualityNormal = new(nameof(QualityNormal), new Color32(0x29, 0x29, 0x29, 0xFF)),
            QualityMagic = new(nameof(QualityMagic), new Color32(0x2E, 0x58, 0x77, 0xFF)),
            QualityMagicText = new(nameof(QualityMagicText), new Color32(0x4B, 0x84, 0xAF, 0xFF)), 
            QualityStory = new(nameof(QualityStory), new Color32(0x59, 0x24, 0x66, 0xFF)),
            QualityStoryText = new(nameof(QualityStoryText), new Color32(0xA2, 0x69, 0xBB, 0xFF)), 
            QualityQuest = new(nameof(QualityQuest), new Color32(0xA7, 0x66, 0x21, 0xFF)),

            // --- Crosshair
            DefaultCrosshair = new(nameof(DefaultCrosshair), new Color32(0xFF, 0xFF, 0xFF, 0xFF)),
            HostileCrosshair = new(nameof(HostileCrosshair), new Color32(0xE8, 0x58, 0x3C, 0xFF)),
            NonHostileCrosshair = new(nameof(NonHostileCrosshair), new Color32(0x8D, 0xD5, 0x7A, 0xFF)),

            // --- Editor & Debug
            EditorGrey = new(nameof(EditorGrey), new Color(0.70588f, 0.70588f, 0.70588f, 1)),
            EditorRed = new(nameof(EditorRed), new Color(0.81568f, 0.109803f, 0.031372f, 1)),
            EditorSecondaryRed = new(nameof(EditorSecondaryRed), new Color(0.80784315f, 0.22745098f, 0.22745098f, 1f)),
            EditorBlue = new(nameof(EditorBlue), new Color(0.196f, 0.698f, 0.929f, 1f));
            
        public static implicit operator Color(ARColor arColor) => arColor.Color;
        public static implicit operator Color32(ARColor arColor) => arColor.Color;
    }
}
