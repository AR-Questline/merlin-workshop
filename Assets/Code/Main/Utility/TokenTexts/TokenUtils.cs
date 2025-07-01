using System.Collections.Generic;
using System.Linq;

namespace Awaken.TG.Main.Utility.TokenTexts {
    public static class TokenUtils {
        /// <summary>
        /// Default converters, automatically applied to all tokens (unless converters are overriden) 
        /// </summary>
        static readonly TokenConverter[] Converters = {
            TokenConverter.HeroStats,
            TokenConverter.SkillMetadatas, 
            TokenConverter.SkillVariables, 
            TokenConverter.Keywords, 
        };
        
        public static string Construct(TokenText token, string input, IEnumerable<TokenConverter> allowedConverters = null) {
            string result = input;
            allowedConverters ??= Converters;
            foreach (TokenConverter converter in allowedConverters.Where(t => t.Regex != null)) {
                result = converter.Regex.Replace(result, match => {
                    TokenText newToken = converter.Replace(match, converter.TargetType);
                    token.AddToken(newToken, false);
                    return $"{{{(token.NextTokenIndex - 1).ToString()}}}";
                });
            }

            return result;
        }

        [UnityEngine.Scripting.Preserve]
        public static string RunTokens(this string text) {
            return new TokenText(text).GetValue(null, null);
        }
    }
}