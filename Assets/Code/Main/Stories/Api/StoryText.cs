using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories.Core;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Stories.Api {
    /// <summary>
    /// Utility for working with text in stories. Handles styling and templating.
    /// </summary>
    public class StoryText {
        static readonly Regex TemplateReplacementRegex = new Regex(@"{{([a-zA-Z:]+)}}", RegexOptions.Compiled);
        static readonly Regex VariableRegex = new Regex(@"{\[(.+?)\]}", RegexOptions.Compiled);

        public static string Format([CanBeNull] Story story, string rawText, StoryTextStyle style, bool append = false) {
            if (string.IsNullOrWhiteSpace(rawText)) {
                return "";
            }
            
            string templated = TemplateText(story, rawText);
            templated = FormatVariables(story, templated);
            if (append)
                return style.FormatForAppend(textToDisplay: templated);
            return style.FormatFirst(textToDisplay: templated);
        }

        static string TemplateText([CanBeNull] Story story, string rawText) {
            return TemplateReplacementRegex.Replace(input: rawText, evaluator: match => {
                string tag = match.Groups[1].Value;
                switch (tag) {
                    case "location:name":
                        return story?.FocusedLocation?.DisplayName;
                    case "hero:name":
                        return Hero.Current.Name;
                    default:
                        return $"[[unrecognized tag: {tag}]]";
                }
            });
        }

        public static string FormatVariables([CanBeNull] Story story, string input) {
            if (string.IsNullOrWhiteSpace(input)) {
                return input;
            }
            return VariableRegex.Replace(input, match => {
                string variableName = match.Groups[1].Value;
                var variableReference = story.VariableReferences.FirstOrDefault(v => v.name == variableName);
                VariableDefine define = story.Variables.FirstOrDefault(v => v.name == variableName);
                return variableReference?.GetName() 
                       ?? define?.Create(story).GetValue().ToString(CultureInfo.InvariantCulture)
                       ?? story.Memory.Context(story).Get(variableName)?.ToString()
                       ?? string.Empty;
            });
        }
    }
}