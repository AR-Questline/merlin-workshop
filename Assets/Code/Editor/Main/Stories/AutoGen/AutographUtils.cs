using System.Text.RegularExpressions;

namespace Awaken.TG.Editor.Main.Stories.AutoGen {
    public static class AutographUtils {
        static readonly Regex RandomizeStartRegexMatch = new(@"\[randomize\]");
        static readonly Regex RandomizeEndRegexMatch = new(@"\[\/randomize\]");
        static readonly Regex StatementRegexMatch = new(@"\$");
        static readonly Regex RandomizeRegexMatch = new(@"\[randomize\](?<=\[randomize\])(.*?)(?=\[\/randomize\])\[\/randomize\]", RegexOptions.Singleline);
        const string RandMarker = "[rand]";

        /// <summary>
        /// Corrects the input string by replacing alternative markers with correct ones.
        /// </summary>
        public static string CorrectAlternativeMarkers(string input) {
            var randomizeMatches = RandomizeRegexMatch.Matches(input);
            foreach (var match in randomizeMatches) {
                var replacement = match.ToString();
                replacement = RandomizeStartRegexMatch.Replace(replacement, string.Empty);
                replacement = RandomizeEndRegexMatch.Replace(replacement, string.Empty);
                replacement = StatementRegexMatch.Replace(replacement,"$"+ RandMarker);
                input = input.Replace(match.ToString(), replacement);
            }
            return input.Trim();
        }
    }
}