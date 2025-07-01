using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Parts {
    [Serializable]
    public class TextPart : BasePart {
        public SmartTextSettings settings = new SmartTextSettings();
        [RichEnumExtends(typeof(KeyBindings))]
        public List<RichEnumReference> keys = new List<RichEnumReference>();

        public override bool IsTutorialBlocker => true;

        IEnumerable<KeyBindings> Bindings => keys?.Select(k => k.EnumAs<KeyBindings>()) ?? Enumerable.Empty<KeyBindings>();
        IEnumerable<string> ActionNames => Bindings.Select(b => (string) b);

        public override UniTask<bool> OnRun(TutorialContext context) {
            var smartText = settings.Spawn(context.target);
            string textValue = $"{smartText.textMesh.text}\n";

            string keyCodesAvailable = "";
            var player = RewiredHelper.Player;
            for (int i = 0; i < ActionNames.Count(); i++) {
                // var element = player.controllers.maps.GetFirstElementMapWithAction(ActionNames.ElementAt(i), true).elementIdentifierName;
                // keyCodesAvailable += i == ActionNames.Count() - 1 ? $"{element}" : $"{element} or ";
            }

            keyCodesAvailable = keyCodesAvailable.ColoredText(ARColor.MainAccent);
            textValue = RichTextUtil.SmartFormatParams(textValue, keyCodesAvailable).Trim();
            smartText.Fill(textValue);
            
            context.onFinish += smartText.Discard;
            return UniTask.FromResult(true);
        }

        public override void TestRun(TutorialContext context) {
            var text = settings.TestSpawn();
            if (text == null) return;
            context.onFinish += () => GameObjects.DestroySafely(text.gameObject);
        }
    }
}