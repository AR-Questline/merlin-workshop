using Awaken.CommonInterfaces.Animations;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Stories {
    public partial class NpcInvolvement : Element<Story>, IAnimatorBridgeStateProvider {
        public sealed override bool IsNotSaved => true;

        public bool AlwaysAnimate => true;
        
        AnimatorBridge _npcAnimator;
        DialogueInvisibility _invisibility;
        IEventListener _damageTakenListener;
        IEventListener _npcStateChangeListener;
        bool _invulnerable;
        bool _talking;
        bool _isUpdating;

        public NpcElement Owner { get; }

        public new static class Events {
            public static readonly Event<NpcElement, NpcInvolvement> NpcInvolvementStopped = new(nameof(NpcInvolvementStopped));
        }

        public NpcInvolvement(NpcElement owner) {
            Owner = owner;
        }

        protected override void OnInitialize() {
            Owner.IsInDialogue = true;
        }

        public void MakeInvulnerable() {
            if (_invulnerable) {
                return;
            }

            _invulnerable = true;
            _invisibility = Owner.AddElement(new DialogueInvisibility(ParentModel));
            _damageTakenListener = Owner.HealthElement.ListenTo(HealthElement.Events.TakingDamage, OnDamageTaken, this);
        }

        public void StopInvulnerable() {
            if (!_invulnerable) {
                return;
            }

            _invulnerable = false;
            if (Owner is { HasBeenDiscarded: false }) {
                Owner.RemoveElement(_invisibility);
                Owner.Trigger(Events.NpcInvolvementStopped, this);
            }
            _invisibility = null;
            World.EventSystem.TryDisposeListener(ref _damageTakenListener);
        }

        public async UniTask DropToAnchor() {
            if (_isUpdating) {
                if (!await AsyncUtil.WaitUntil(Owner, () => !_isUpdating)) {
                    return;
                }
            }
            if (!_talking) {
                return;
            }
            _isUpdating = true;
            var behaviours = Owner.Behaviours;
            if (behaviours.HasAnchor) {
                await behaviours.DropToAnchor();
                behaviours.SetAsAnchor(false);
            }
            _isUpdating = false;
        }

        public async UniTask StartTalk(bool rotToHero = false, bool forceExitInteraction = false) {
            if (_isUpdating) {
                if (!await AsyncUtil.WaitUntil(Owner, () => !_isUpdating)) {
                    return;
                }
            }
            if (_talking && !forceExitInteraction) {
                return;
            }
            _isUpdating = true;
            _talking = true;
            
            _npcAnimator = AnimatorBridge.GetOrAddDefault(Owner.Movement.Controller.Animator);
            _npcAnimator.RegisterStateProvider(this);
            
            _npcStateChangeListener = Owner.ListenTo(NpcAI.Events.NpcStateChanged, OnNpcStateChanged, this);
            await Owner.Behaviours.StartTalk(ParentModel, rotToHero, forceExitInteraction);
            _isUpdating = false;
        }

        void OnNpcStateChanged(Change<IState> change) {
            if (change is (StateAIWorking, _)) {
                // If AI is disabled it's better to leave it for Idle Behaviour to immediately stop everything
                EndTalk(Owner.IsUnique).Forget();
            } else if (change is (StateIdle, not null)) {
                // If Idle is disabled it will stop interactions
                EndTalk().Forget();
            }
        }

        public async UniTask EndTalk(bool ignoreInteraction = false, bool rotReturnToInteraction = true) {
            if (_isUpdating) {
                if (!await AsyncUtil.WaitUntil(Owner, () => !_isUpdating)) {
                    return;
                }
            }
            if (!_talking) {
                return;
            }
            _isUpdating = true;
            _talking = false;

            if (!ignoreInteraction && Owner is { HasBeenDiscarded: false, IsUnique: true, Behaviours: { Active: true } }
                    or { HasBeenDiscarded: false, IsUnique: false } && Owner.Behaviours is not null) {
                await Owner.Behaviours.EndTalk(rotReturnToInteraction);
            }

            if (_npcAnimator != null) {
                _npcAnimator.UnregisterStateProvider(this);
                _npcAnimator = null;
            }
            
            World.EventSystem.TryDisposeListener(ref _npcStateChangeListener);
            _isUpdating = false;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (fromDomainDrop) {
                return;
            }
            Owner.IsInDialogue = false;
            EndTalk().Forget();
            StopInvulnerable();
        }
        
        void OnDamageTaken(HookResult<HealthElement, Damage> obj) {
            if (obj.Prevented) {
                return;
            }
            Damage dmg = obj.Value;
            if (dmg.DamageDealer is null && dmg.HitCollider is null && dmg.Item is null && dmg.Skill is null) {
                //if everything is null it's damage from story
                return;
            }
            obj.Prevent();
        }
        
        public static async UniTask<NpcInvolvement> GetOrCreateFor(Story api, NpcElement npc, bool invulnerable) {
            
            if (api is not Story story) {
                return null;
            }
            
            var involvement = story.Elements<NpcInvolvement>().FirstOrDefault(i => i.Owner == npc);
            if (involvement == null) {
                involvement = story.AddElement(new NpcInvolvement(npc));
            }

            if (invulnerable) {
                involvement.MakeInvulnerable();
            } else {
                involvement.StopInvulnerable();
            }
            
            await NpcInvolvementOwner.EnsureInvolvementOwned(involvement);

            return involvement;
        }

        public static NpcInvolvement GetFor(Story api, NpcElement npc) {
            return api is not Story story ? null : story.Elements<NpcInvolvement>().FirstOrDefault(i => i.Owner == npc);
        }
    }
}