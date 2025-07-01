using System;
using System.Collections.Generic;
using System.Text;
using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Stories.Api {
    /// <summary>
    ///     Lets user of the story API choose different styles for the text
    ///     they output in the UI.
    /// </summary>
    public class StoryTextStyle : RichEnum {
        // === Instances

        public static readonly StoryTextStyle
            Plain = new(nameof(Plain), PlainFormat),
            StatChange = new(nameof(StatChange), StatChangeFormat),
            Aside = new(nameof(Aside), AsideFormat),
            KnownEffects = new(nameof(KnownEffects), KnownEffectsFormat),
            NpcDialogue = new(nameof(NpcDialogue), PlainFormat);

        // === Creation

        readonly Func<string, bool, string> _formatter;

        StoryTextStyle(string enumName, Func<string, bool, string> formatter) : base(enumName) {
            _formatter = formatter;
        }

        // === Functions

        public string FormatFirst(string textToDisplay) {
            textToDisplay = textToDisplay.FormatSprite();
            return _formatter(textToDisplay, false);
        }

        public string FormatForAppend(string textToDisplay) {
            textToDisplay = textToDisplay.FormatSprite();
            return _formatter(textToDisplay, true);
        }

        public string FormatList(in StructList<string> textsToDisplay, bool enabled) {
            StringBuilder costsOutput = new();
            StringBuilder rewardsOutput = new();
            StringBuilder neutralOutput = new();
            string requiredKeyword = $"{LocTerms.Requires.Translate()}: ";
            string divider = $"{LocTerms.Divider.Translate()}\n";
            rewardsOutput.Append(LocTerms.Get.Translate());
            costsOutput.Append(enabled ? LocTerms.Give.Translate() : requiredKeyword);

            List<string> costsToDisplay = new();
            List<string> rewardsToDisplay = new();
            List<string> neutralsToDisplay = new();

            int costsCount = 0;
            bool anyCosts = false;
            int rewardsCount = 0;
            bool anyRewards = false;

            foreach (var text in textsToDisplay) {
                if (text.Contains("-")) {
                    costsToDisplay.Add(text);
                    costsCount++;
                    anyCosts = true;
                } else if (text.Contains("+")) {
                    rewardsToDisplay.Add(text);
                    rewardsCount++;
                    anyRewards = true;
                } else {
                    neutralsToDisplay.Add(text);
                }
            }

            for (int i = 0; i < costsToDisplay.Count; i++) {
                string text = costsToDisplay[i];
                if (costsCount > 1 && i > 0) {
                    costsOutput.Append(divider);
                }

                costsOutput.Append($"{text.Replace("-", string.Empty)}");
            }

            for (int i = 0; i < rewardsToDisplay.Count; i++) {
                string text = rewardsToDisplay[i];
                if (rewardsCount > 1 && i > 0) {
                    rewardsOutput.Append(divider);
                }

                rewardsOutput.Append($"{text.Replace("+", string.Empty)}");
            }

            if (anyCosts || anyRewards) {
                neutralOutput.Append("\n");
            }

            if (!enabled && (!anyCosts && !anyRewards)) {
                neutralOutput.Append(requiredKeyword);
            }
            
            for (int i = 0; i < neutralsToDisplay.Count; i++) {
                string text = neutralsToDisplay[i];
                if (neutralsToDisplay.Count > 1 && i > 0) {
                    neutralOutput.Append(divider);
                }

                neutralOutput.Append(enabled ? text : $"{text.Replace(":", string.Empty)}");
            }
            
            string costsOutputString = FormatOutput (costsOutput.ToString(), enabled);
            string neutralOutputString = FormatOutput(neutralOutput.ToString(), enabled);
            
            if (anyCosts && anyRewards) {
                return rewardsOutput.Append($"\n{costsOutputString}").Append(neutralOutputString).ToString();
            }

            if (anyCosts) {
                return FormatOutput(costsOutput.Append($"{neutralOutput}").ToString(), enabled);
            }

            if (anyRewards) {
                return rewardsOutput.Append(neutralOutputString).ToString();
            }

            return neutralOutputString;
        }

        static string FormatOutput(string output, bool enabled) {
            return enabled ? output : output.ColoredText(ARColor.MainRed);
        }

        static string PlainFormat(string text, bool append) {
            /*Regex paragraphStarts = new Regex("^", RegexOptions.Multiline);
            text = paragraphStarts.Replace(text, "<space=1em>");*/
            if (append)
                text = "\n" + text;

            return text;
        }

        static string StatChangeFormat(string text, bool append) {
            string formatted = "<color=#7799cc><size=80%>" + text + "</size></color>";
            string prefix = append ? "<space=0.5em>" : "<space=1em>";
            return prefix + formatted;
        }

        static string AsideFormat(string text, bool append) {
            string formatted = "<color=#7799cc><size=80%>" + text + "</size></color>";
            string prefix = append ? "\n<space=1em>" : "<space=1em>";
            return prefix + formatted;
        }

        static string KnownEffectsFormat(string text, bool _) {
            string formatted = "<nobr><size=100%>" + text + "</size></nobr>";
            return formatted;
        }
    }
}