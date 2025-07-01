using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using UnityEngine;

namespace Awaken.TG.Main.Animations {
    public partial class VCAnimationRiggingHandler {
        abstract class AnimationRigging {
            protected VCAnimationRiggingHandler Handler { get; private set; }
            
            protected AnimationRiggingData _data;

            public virtual ref readonly AnimationRiggingData Data => ref !Handler._inBand ? ref Handler._inactiveData : ref _data;

            protected float DefaultHeadRigUpdateSpeed => Handler.defaultHeadRigUpdateSpeed;
            protected float DefaultBodyRigUpdateSpeed => Handler.defaultBodyRigUpdateSpeed;
            protected float DefaultRootRigUpdateSpeed => Handler.defaultRootRigUpdateSpeed;
            protected float DefaultCombatRigUpdateSpeed => Handler.defaultCombatRigUpdateSpeed;
            [UnityEngine.Scripting.Preserve] protected float DefaultUpDownTurnSpeed => Handler.defaultUpDownTurnSpeed;
            
            protected Transform CombatIKPosUp => Handler.combatIKPosUp;
            protected Transform CombatIKPosLow => Handler.combatIKPosLow;
            protected Transform CombatIKTarget => Handler.combatIKTarget;
            
            protected float CombatIKSlider => Handler.combatIKSlider;
            protected float ReachMaxLookDownWhenHeroBelow => Handler.reachMaxLookDownWhenHeroBelow;
            protected float ReachMaxLookUpWhenHeroAbove => Handler.reachMaxLookUpWhenHeroAbove;
            protected float IgnoreHeightDifferenceThreshold => Handler.ignoreHeightDifferenceThreshold;
            
            protected FloatRange GlanceTimeRange => Handler.glanceTimeRange;
            protected FloatRange GlanceDelayRange => Handler.glanceDelayRange;

            protected INpcInteraction CurrentInteraction => Handler._currentInteraction;
            
            protected float DotTowardsHero { get {
                Vector3 normalizedDirectionTowardsHero = Vector3.Normalize(Hero.Current.Coords - Location.Coords);
                return Vector3.Dot(Location.Forward(), normalizedDirectionTowardsHero);
            }}
            
            protected Location Location => Handler.Target;
            protected NpcElement NpcElement => Handler.NPCElement;
            
            public void Init(VCAnimationRiggingHandler handler) {
                Handler = handler;
                OnInit();
            }
            protected virtual void OnInit() { }
            
            protected void SetupRigsWeightsFromCurrentInteraction() {
                SpineRotationType spineRotationType = SpineRotationType.FullRotation;
                if (NpcElement.Behaviours.CurrentUnwrappedInteraction is SimpleInteractionBase si) {
                    spineRotationType = si.SpineRotationType;
                }
            
                switch (spineRotationType) {
                    case SpineRotationType.FullRotation:
                    case SpineRotationType.UpperBody:
                        _data.headRigDesiredWeight = 1;
                        _data.bodyRigDesiredWeight = 1;
                        break;
                    case SpineRotationType.HeadOnly:
                        _data.headRigDesiredWeight = 1;
                        _data.bodyRigDesiredWeight = 0;
                        break;
                    case SpineRotationType.None:
                    default:
                        _data.headRigDesiredWeight = 0;
                        _data.bodyRigDesiredWeight = 0;
                        break;
                }
            }

            public virtual void Dispose() {}
        }
    }
}