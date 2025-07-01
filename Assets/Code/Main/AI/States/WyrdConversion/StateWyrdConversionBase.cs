using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.AI.States.WyrdConversion {
    public abstract class StateWyrdConversionBase : NpcState<StateWyrdConversion> {
        NoMove _noMove;
        
        public bool ConversionEnded { get; private set; }
        protected abstract bool IsConvertingIntoWyrd { get; }
        protected abstract NpcStateType StateToEnter { get; }

        public override void Init() {
            _noMove = new NoMove();
        }
        
        protected override void OnEnter() {
            base.OnEnter();
            AI.InWyrdConversion = true;
            ConversionEnded = false;
            Movement.ChangeMainState(_noMove);
            Npc.SetAnimatorState(NpcFSMType.GeneralFSM, StateToEnter);
            
            IBehavioursOwner behavioursOwner = Npc.ParentModel.TryGetElement<IBehavioursOwner>();
            if (behavioursOwner != null) {
                behavioursOwner.ListenTo(EnemyBaseClass.Events.AnimationEvent, OnAnimationEvent, this);
            } else {
                Npc.ConvertToWyrd();
            }
        }
        
        void OnAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                ProcessConversionFromAnimation().Forget();
            }
        }
        
        public override void Update(float deltaTime) {
            if (ConversionEnded) {
                return;
            }
            
            if (Npc.Element<NpcGeneralFSM>().CurrentAnimatorState.Type != StateToEnter) {
                ConversionEnded = true;
            }
        }

        protected override void OnExit() {
            base.OnExit();

            bool npcIsAlive = !Npc.HasBeenDiscarded && Npc.IsAlive;
            if (npcIsAlive) {
                if (!Npc.WyrdConverted && IsConvertingIntoWyrd) {
                    Npc.ConvertToWyrd();
                } else if (Npc.WyrdConverted && !IsConvertingIntoWyrd) {
                    Npc.UnConvertFromWyrd();
                }
            }
            
            AI.InWyrdConversion = false;
            ConversionEnded = false;

            if (npcIsAlive) {
                AI.UpdateHeroVisibility(AI.HeroVisibility);
            }
        }

        async UniTaskVoid ProcessConversionFromAnimation() {
            // Animation is triggered in wrong loop point, so we need to wait for safe harbor - Update
            await UniTask.NextFrame(PlayerLoopTiming.Update);

            if (Npc?.HasBeenDiscarded ?? true) {
                return;
            }

            if (IsConvertingIntoWyrd) {
                Npc.ConvertToWyrd();
            } else {
                Npc.UnConvertFromWyrd();
            }
        }
    }
}