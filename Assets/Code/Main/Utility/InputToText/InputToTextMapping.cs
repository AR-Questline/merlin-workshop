using System;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Rewired;
using UnityEngine;

namespace Awaken.TG.Main.Utility.InputToText {
    public class InputToTextMapping : ScriptableObject, IService {
        public ActionToTextBiding[] mouseBidings = Array.Empty<ActionToTextBiding>();
        public KeyToTextBiding[] keyToTextBidings = Array.Empty<KeyToTextBiding>();
        public GamepadIndexToTextBiding[] gamepadToTextBidings = Array.Empty<GamepadIndexToTextBiding>();

        static readonly Regex SpriteRegex = new(@"\{input:.+?\}", RegexOptions.Compiled);

        public string ReplaceInText(string text) {
            var spriteMatches = SpriteRegex.Matches(text).Cast<Match>().Where(m => m.Success);
            foreach (Match spriteMatch in spriteMatches) {
                string actionName = spriteMatch.Value.Substring(7, spriteMatch.Value.Length - 8).Trim();
                text = text.Replace(spriteMatch.Value, GetText(actionName));
            }

            return text;
        }

        public string GetText(string actionName) {
            var textSource = GetTextSource(actionName);
            return textSource != null ? textSource.Text : "";
        }

        ITextSource GetTextSource(string actionName) {
            return RewiredHelper.IsGamepad ? GetJoystickTextSource(actionName) : GetMouseOrKeyboardTextSource(actionName);
        }

        GamepadIndexToTextBiding GetJoystickTextSource(string actionName) {
            // foreach (var elementMap in RewiredHelper.Player.controllers.maps.ElementMapsWithAction(actionName, false)) {
            //     if (elementMap.controllerMap.controllerType == ControllerType.Joystick) {
            //         var toText = gamepadToTextBidings.FirstOrDefault(b =>
            //             b.identifierId == elementMap.elementIdentifierId);
            //         if (toText != null) {
            //             return toText;
            //         }
            //     }
            // }

            return null;
        }

        ITextSource GetMouseOrKeyboardTextSource(string actionName) {
            var mouseSource = mouseBidings.FirstOrDefault(b => b.actionName == actionName);
            if (mouseSource != null) {
                return mouseSource;
            }
            
            // foreach (var elementMap in RewiredHelper.Player.controllers.maps.ElementMapsWithAction(actionName, false)) {
            //     if (elementMap.controllerMap.controllerType == ControllerType.Keyboard) {
            //         var toText = keyToTextBidings.FirstOrDefault(b => b.keyCode == elementMap.keyCode);
            //         if (toText == null) {
            //             return new KeyToTextBiding() {text = Enum.GetName(typeof(KeyCode), elementMap.keyCode)};
            //         } else {
            //             return toText;
            //         }
            //     }
            // }

            return null;
        }

        [Serializable]
        public class KeyToTextBiding : ITextSource {
            public KeyCode keyCode;
            public string text;
            public string Text => text;
        }

        [Serializable]
        public class GamepadIndexToTextBiding : ITextSource {
            [Tooltip("Used to find mapping with just the action name")]
            public int identifierId;
            [RichEnumExtends(typeof(KeyBindings))][Tooltip("Used to find mapping with key binding")]
            public RichEnumReference[] keybidings;
            [Tooltip("Text used to replace key binding identifier in text")]
            public string text;
            public string Text => text;

            public bool HasKeybinding(KeyBindings biding) {
                return keybidings.Any(b => b.Enum == biding);
            }
        }

        [Serializable]
        public class ActionToTextBiding : ITextSource {
            public string actionName;
            public string text;
            public string Text => text;
        }


        public interface ITextSource {
            public string Text { get; }
        }
    }
}