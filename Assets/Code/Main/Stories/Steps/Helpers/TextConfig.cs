using System.Text.RegularExpressions;
using Awaken.TG.Assets;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Api;

namespace Awaken.TG.Main.Stories.Steps.Helpers {
    public class TextConfig {
        const string GesturePattern = @"\$G_[a-zA-Z0-9_]*\$";
        const string EmotePattern = @"\$e_[a-zA-Z0-9]*\$";
        
        // === State
        Regex GestureRegex { get; } = new(GesturePattern);
        Regex EmoteRegex { get; } = new(EmotePattern);
        public Location Location { get; private set; }
        public Actor Actor { get; private set; }
        public string Text { get; }
        public StoryTextStyle Style { get; }
        public bool HasLink { get; }
        public string GestureKey { get; private set; }
        public string EmoteKey { get; private set; }
        public ShareableSpriteReference Icon { get; private set; }

        // === Static constructors
        public static TextConfig WithText(string text) => new(text);
        public static TextConfig WithTextAndStyle(string text, StoryTextStyle textStyle) => new(text, textStyle);
        public static TextConfig WithEverything(string text, StoryTextStyle textStyle, ShareableSpriteReference icon) => new(text, textStyle, icon);
        
        // === Constructor
        TextConfig(string text, StoryTextStyle textStyle = null, ShareableSpriteReference icon = null) {
            Text = ExtractBodyLanguage(text);
            Style = textStyle ?? StoryTextStyle.Plain;
            HasLink = Text.Contains("<link=");
            Icon = icon;
        }

        // === Operations
        string ExtractBodyLanguage(string text) {
            GestureKey = ExtractActivityKey(GestureRegex, ref text);
            EmoteKey = ExtractActivityKey(EmoteRegex, ref text);

            return text;
        }

        static string ExtractActivityKey(Regex regex, ref string text) {
            var match = regex.Match(text);
            if (match.Success) {
                text = regex.Replace(text, "").Trim();
                return match.Value;
            }

            return string.Empty;
        }

        // === Fluent API
        public TextConfig ByLocation(Location location, Actor actor) {
            Location = location;
            Actor = actor;
            return this;
        }
    }
}