using Awaken.Utility;
using System.Linq;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Fights.NPCs {
    /// <summary>
    /// Element marking ICharacter invisible
    /// </summary>
    public abstract partial class Invisibility : Element<ICharacter> {
        public virtual bool BlocksPerception(NpcElement npc) => true;
    }

    public partial class DialogueInvisibility : Invisibility {
        public sealed override bool IsNotSaved => true;

        public Story StoryAPI { get; }
        public DialogueInvisibility(Story storyAPI) {
            StoryAPI = storyAPI;
        }

        protected override void OnInitialize() {
            StoryAPI.ListenTo(Events.AfterDiscarded, Discard, this);

            if (ParentModel is not NpcElement npc) {
                return;
            }
            foreach (var targeting in npc.GetTargeting().ToList()) {
                WaitForTargetInStoryBehaviour.Start(targeting);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (ParentModel.Elements<DialogueInvisibility>().CountEqualTo(1)) {
                if (ParentModel is NpcElement npc) {
                    foreach (var targeting in npc.GetTargeting().ToList()) {
                        WaitForTargetInStoryBehaviour.Stop(targeting);
                    }
                }
            }
            base.OnDiscard(fromDomainDrop);
        }

        public override bool BlocksPerception(NpcElement npc) {
            return !CrimeReactionUtils.IsGuard(npc);
        }
    }

    public partial class UnconsciousInvisibility : Invisibility {
        public sealed override bool IsNotSaved => true;

        public UnconsciousElement Owner { get; }
        public UnconsciousInvisibility(UnconsciousElement owner) {
            Owner = owner;
        }
        protected override void OnInitialize() {
            Owner.ListenTo(Events.AfterDiscarded, Discard, this);
        }
    }

    public partial class HeroFireplaceInvisibility : Invisibility {
        public sealed override bool IsNotSaved => true;
    }

    public partial class HeroSummonInvisibility : Invisibility {
        public sealed override bool IsNotSaved => true;
    }

    public partial class HeroDebugInvisibility : Invisibility {
        public sealed override bool IsNotSaved => true;
        
        protected override void OnInitialize() {
            ParentModel.OverrideFaction(Services.Get<CommonReferences>().InvisibleHeroFaction);
            base.OnInitialize();
        }

        protected override void OnRestore() {
            Discard();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.ResetFactionOverride();
            base.OnDiscard(fromDomainDrop);
        }
    }

    /// <summary>
    /// Marker class for ignoring this Character from perception checks, handled manually by VS.
    /// </summary>
    public partial class CharacterManualInvisibility : Invisibility {
        public override ushort TypeForSerialization => SavedModels.CharacterManualInvisibility;

        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static CharacterManualInvisibility AddToCharacter(ICharacter character) {
            return character.AddElement(new CharacterManualInvisibility());
        }
    }

    public partial class DefeatedInDuelInvisibility : Invisibility {
        public sealed override bool IsNotSaved => true;
    }
}