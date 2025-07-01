using Awaken.TG.Main.Localization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Actors {
    public class ActorState : MonoBehaviour {
        [ShowInInspector]
        public string Id => gameObject.name;
        [LocStringCategory(Category.Actor), InfoBox("Leave fields empty to use default defined in actor")]
        public LocString nameOverride;
        
        public void Apply(ref Actor actor) {
            string translated = nameOverride.ToString();
            if (!string.IsNullOrWhiteSpace(translated)) {
                actor.Name = translated;
            }
        }
    }
}