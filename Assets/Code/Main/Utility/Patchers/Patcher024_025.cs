using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Newtonsoft.Json.Linq;

namespace Awaken.TG.Main.Utility.Patchers {
    [UnityEngine.Scripting.Preserve]
    public class Patcher024_025 : Patcher {
        protected override Version MaxInputVersion => new Version(0, 24);
        protected override Version FinalVersion => new Version(0, 25);

        Dictionary<IModel, List<Element>> _modelElements = new();

        public override bool AfterDeserializedModel(Model model) =>
            model switch {
                DisableAggroMusicMarker disableAggroMusic => MaintainOnlyOne(disableAggroMusic),
                HideCompassMarker hideCompass => MaintainOnlyOne(hideCompass),
                HideHealthBar hideHealthBar => MaintainOnlyOne(hideHealthBar),
                _ => true
            };

        bool MaintainOnlyOne<T>(T element) where T : Element {
            if (_modelElements.ContainsKey(element.GenericParentModel)) {
                if (_modelElements[element.GenericParentModel]?.Any(e => e.GetType() == element.GetType()) ?? false) {
                    return false;
                }
                _modelElements[element.GenericParentModel].Add(element);
            } else {
                _modelElements[element.GenericParentModel] = new List<Element> { element };
            }
            return true;
        }
    }
}