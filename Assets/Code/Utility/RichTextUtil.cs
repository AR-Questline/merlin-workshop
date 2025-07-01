using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Awaken.Utility;
using Awaken.Utility.Maths;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.Localization;

namespace Awaken.TG.Utility {
    [Il2CppEagerStaticClassConstruction]
    public static class RichTextUtil {
        // regex for {text}
        const string RegexPattern = @"\{.+?\}";
        static readonly Regex SpriteRegex = new(RegexPattern, RegexOptions.Compiled);
        static readonly Dictionary<string, string> TranslationsCache = new();
        static Locale s_cacheLocale;

        [UnityEngine.Scripting.Preserve]
        public static string CenteredText(this string text) {
            return $"<align=center>{text}</align>\n";
        }
        
        public static string ColoredText(this string text, Color baseColor, float whiteTint = 0f) {
            if(whiteTint != 0f) {
                baseColor = Color.Lerp(baseColor, Color.white, whiteTint);
            }
            
            string hexColor = ColorUtility.ToHtmlStringRGB(baseColor);
            return ColoredText(text, hexColor);
        }
        
        public static string ColoredText(this string text, ARColor baseColor) {
            return ColoredText(text, baseColor.Hex);
        }
        
        public static string ColoredTextIf(this string text, bool condition, Color baseColor, float whiteTint = 0f) {
            return condition ? text.ColoredText(baseColor, whiteTint) : text;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static string FontExtraLight(this string text) {
            return  text.FontWeight(200);
        }
        
        public static string FontLight(this string text) {
            return  text.FontWeight(300);
        }
        
        public static string FontRegular(this string text) {
            return  text.FontWeight(400);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static string FontMedium(this string text) {
            return  text.FontWeight(500);
        }
        
        public static string FontSemiBold(this string text) {
            return  text.FontWeight(600);
        }
        
        public static string FontBold(this string text) {
            return  text.FontWeight(700);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FontWeight(this string text, int fontWeight) {
            return $"<font-weight={fontWeight}>{text}</font-weight>";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        public static string TextStyle(this string text, string style) {
            return $"<style={style}>{text}</style>";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ColoredText(this string text, string hexColor) {
            return $"<color=#{hexColor}>{text}</color>";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        public static string SizeText(this string text, int size) {
            return $"<size={size}>{text}</size>";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        public static string RelativeSizeText(this string text, int size) {
            string sign = size >= 0 ? "+" : "-";
            return $"<size={sign}{size}>{text}</size>";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string PercentSizeText(this string text, float percent) {
            return $"<size={percent}%>{text}</size>";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        public static string Monospace(this string text, float size) {
            return $"<mspace={size.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}em>{text}</mspace>";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Bold(this string text) {
            return $"<b>{text}</b>";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Italic(this string text) {
            return $"<i>{text}</i>";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string StrikeThrough(this string text) {
            return $"<s>{text}</s>";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string LineHeight(this string text, int percent) {
            return $"<line-height={percent}%>{text}</line-height>";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string WithSprite(this string text, string name, Color tint, bool before = true) {
            return before ? $"<sprite name=\"{name}\" color={tint.ToHex()}> {text}" : $"{text} <sprite name={name}>";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string WithSprite(this string text, int index, bool before = true) {
            return before ? $"<sprite index={index}> {text}" : $"{text} <sprite index={index}>";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToSprite(this string name) {
            return $"<sprite name={name}>";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToSprite(this int index) {
            return $"<sprite index={index}>";
        }

        public static string FormatSprite(this string text) {
            // get all success matches
            var spriteMatches = SpriteRegex.Matches(text);
            // change {text} to <sprite name=text> for all matches
            foreach (Match spriteMatch in spriteMatches) {
                if (!spriteMatch.Success) {
                    continue;
                }
                
                string content = spriteMatch.Value.Substring(1, spriteMatch.Value.Length - 2);
                string spriteName;
                string tooltip = "";
                string technical = "";
                string color = null;
                if (content.Contains("tooltip:")) {
                    string[] array = content.Split(';');
                    tooltip = array[0].Replace("tooltip:", "");
                    technical = array[1].Replace("technical:", "");
                    spriteName = array[2].Replace("sprite:", "");
                    string colorFromArray = array[3].Replace("color:", "");
                    if (ColorUtility.TryParseHtmlString(colorFromArray, out _)) {
                        color = colorFromArray;
                    }
                } else {
                    spriteName = content;
                }

                bool showTooltip = !string.IsNullOrWhiteSpace(tooltip) || !string.IsNullOrWhiteSpace(technical);
                string linkData = showTooltip ? $"\"tooltip:{tooltip.Translate()};technical:{technical.Translate()}\"" : null;
                string preColorTag = color == null ? "" : $"<color={color}>";
                string postColorTag = color == null ? "" : "</color>";
                string preLinkTag = linkData == null ? "" : $"<link={linkData}>";
                string postLinkTag = linkData == null ? "" : "</link>";
                string replaceWith = $"<size=130%>{preLinkTag}{preColorTag}<sprite tint=1 name={spriteName}>{postColorTag}{postLinkTag}</size>";
                text = text.Replace(spriteMatch.Value, replaceWith);
            }
            return text;
        }

        public static string AddTooltip(this string text, string tooltip, string technicalTooltip = "") {
            string linkData = $"\"tooltip:{tooltip};technical:{technicalTooltip}\"";
            return $"<link={linkData}>{text}</link>";
        }
        
        public static string Translate(this string id) {
            string translation = GetTranslation(id);
            return translation != null ? Regex.Unescape(translation) : id;
        }

        public static string TranslateWithFallback(this string id, string fallback) {
            string translation = GetTranslation(id);
            return translation != null ? Regex.Unescape(translation) : (fallback ?? id);
        }
        
        public static string Translate(this string id, params object[] parameters) {
            string translation = GetTranslation(id);
            if (translation != null) {
                translation = SmartFormat(translation, parameters);
                return Regex.Unescape(translation);
            }
            return id;
        }

        static string GetTranslation(string id) {
            // try cache
            if (TryGetTranslationFromCache(id, out string cachedTranslation)) {
                return cachedTranslation;
            }

            // nothing in cache, get it
            string translation = LocalizationHelper.Translate(id, s_cacheLocale);
            if (string.IsNullOrWhiteSpace(translation)) {
                return null;
            }
            
            TranslationsCache[id] = translation;
            return translation;
        }

        static bool TryGetTranslationFromCache(string id, out string translation) {
            // validate cache language
            Locale locale = LocalizationHelper.SelectedLocale;
            if (locale != s_cacheLocale) {
                TranslationsCache.Clear();
                s_cacheLocale = locale;
            }

            return TranslationsCache.TryGetValue(id, out translation);
        }
        
        public static string SmartFormatParams(string fullText, params object[] args) => SmartFormat(fullText, args);
        public static string SmartFormat(string fullText, IReadOnlyList<object> args) {
            return RegexForParameters.Replace(fullText, match => {
                string textInBrackets = match.Groups[0].Value;
                string text = textInBrackets.Substring(1, textInBrackets.Length - 2);

                return int.TryParse(text, out int index) && index >= 0 && index < args.Count
                    ? args[index]?.ToString()
                    : textInBrackets;
            });
        }

        static readonly Regex RegexForParameters = new(@"{\d+}", RegexOptions.Compiled);
        
        //https://stackoverflow.com/a/23303475
        public static string ToRomanNumeral(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("Please use a positive integer greater than zero.");

            StringBuilder sb = new StringBuilder();
            int remain = value;
            while (remain > 0)
            {
                if (remain >= 1000) { sb.Append("M"); remain -= 1000; }
                else if (remain >= 900) { sb.Append("CM"); remain -= 900; }
                else if (remain >= 500) { sb.Append("D"); remain -= 500; }
                else if (remain >= 400) { sb.Append("CD"); remain -= 400; }
                else if (remain >= 100) { sb.Append("C"); remain -= 100; }
                else if (remain >= 90) { sb.Append("XC"); remain -= 90; }
                else if (remain >= 50) { sb.Append("L"); remain -= 50; }
                else if (remain >= 40) { sb.Append("XL"); remain -= 40; }
                else if (remain >= 10) { sb.Append("X"); remain -= 10; }
                else if (remain >= 9) { sb.Append("IX"); remain -= 9; }
                else if (remain >= 5) { sb.Append("V"); remain -= 5; }
                else if (remain >= 4) { sb.Append("IV"); remain -= 4; }
                else if (remain >= 1) { sb.Append("I"); remain -= 1; }
                else throw new Exception("Unexpected error."); // <<-- shouldn't be possible to get here, but it ensures that we will never have an infinite loop (in case the computer is on crack that day).
            }

            return sb.ToString();
        }
    }
}
