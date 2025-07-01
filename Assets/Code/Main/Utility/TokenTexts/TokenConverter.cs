using System.Text.RegularExpressions;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Utility.Skills;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Utility.TokenTexts {
    /// <summary>
    /// These are used to automatically convert known patterns into tokens.
    /// </summary>
    public class TokenConverter : RichEnum {
        public delegate TokenText ReplaceMethod(Match match, TokenType type);
        
        // === Regex
        public static readonly Regex SkillVariableRegex = new(@"\[(.+?)(?:_(\d)|)(?:\:(.+?)|)\]", RegexOptions.Compiled);
        public static readonly Regex SkillMetadataRegex = new(@"\[\{(.+?)(?:_(\d)|)(?:\:(.+?)|)\}\]", RegexOptions.Compiled);
        public static readonly Regex HeroStatRegex = new(@"\|[^|]+\|", RegexOptions.Compiled);
        public static readonly Regex KeywordRegex = new(@"<k=(.*?)>(.*?)<\/k>", RegexOptions.Compiled);
        
        // === Properties
        public Regex Regex { get; }
        public ReplaceMethod Replace { get; }
        public TokenType TargetType { get; }
        
        // === Converters
        public static readonly TokenConverter
            HeroStats = new(nameof(HeroStats), HeroStatRegex, DefaultReplace, TokenType.HeroStats),
            SkillVariables = new(nameof(SkillVariables), SkillVariableRegex, ReplaceWithAdditionalInfo, TokenType.SkillVariable),
            SkillMetadatas = new(nameof(SkillMetadatas), SkillMetadataRegex, ReplaceWithAdditionalInfo, TokenType.SkillMetadata),
            Keywords = new(nameof(Keywords), KeywordRegex, ReplaceKeyword, TokenType.PlainText);

        TokenConverter(string enumName, Regex regex, ReplaceMethod replace, TokenType targetType) : base(enumName) {
            Regex = regex;
            Replace = replace;
            TargetType = targetType;
        }
        
        // === Implementations
        static TokenText DefaultReplace(Match match, TokenType type) {
            string input = match.Value;
            string text = input.Substring(1, input.Length - 2);
            var token = new TokenText(type, text);
            return token;
        }

        static TokenText ReplaceWithAdditionalInfo(Match match, TokenType type) {
            string text = match.Groups[1].Value;
            string additionalString = match.Groups[2].Value;
            string formatSpecifier = match.Groups[3].Value; 
            int.TryParse(additionalString, out int additionalInt);
            var token = new TokenText(type, text);
            token.AdditionalInt = additionalInt;
            token.FormatSpecifier = formatSpecifier;
            return token;
        }

        static TokenText ReplaceKeyword(Match match, TokenType type) {
            // Group 1 - keyword enum, Group 2 - text to replace
            var keyword = SkillsUtils.StringToKeyword(match.Groups[1].Value);
            string text = match.Groups[2].Value;
            text = text.ColoredText(keyword?.DescColor ?? ARColor.SpecialAccent);
            var token = new TokenText(type, text);
            return token;
        }
    }
}