using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using Awaken.TG.Utility.Attributes.Tags;
using UnityEngine;

namespace Awaken.TG.Graphics.ScriptedEvents.Triggers {
    [RequireComponent(typeof(IHeroTrigger))]
    public class HeroTriggerSetFlag : SceneSpec {
        [SerializeField, BoxGroup("Trigger")] Stage stage;
        [SerializeField, BoxGroup("Trigger")] bool onlyOnce;
        [SerializeField, DisableInPlayMode, Indent, Tags(TagsCategory.Flag)] string flag;
        [SerializeField] bool setTo = true;

        void Awake() {
            var trigger = GetComponent<IHeroTrigger>();
            if (stage == Stage.OnEnter) {
                trigger.OnHeroEnter += Trigger;
            } else if (stage == Stage.OnExit) {
                trigger.OnHeroExit += Trigger;
            }
        }
        
        void Trigger() {
            if (onlyOnce) {
                bool added = World.Services.Get<SceneSpecCaches>().AddTriggeredSpec(SceneId);
                if (!added) {
                    return;
                }
            }
            
            StoryFlags.Set(flag, setTo);
        }
        
        enum Stage : byte {
            OnEnter,
            OnExit,
        }
    }
}