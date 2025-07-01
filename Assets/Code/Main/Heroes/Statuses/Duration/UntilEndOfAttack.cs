using System;
using System.Threading;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public partial class UntilEndOfAttack : Element<IWithDuration>, IDuration, IEquatable<UntilEndOfAttack> {
        public sealed override bool IsNotSaved => true;

        public bool Elapsed => false;
        public string DisplayText => string.Empty;

        ICharacter Character { get; }
        float DelayBeforeDiscard { get; }
        CancellationTokenSource _cancellationToken;
        
        // === Constructor
        public UntilEndOfAttack(ICharacter character, float delay = 0) {
            Character = character;
            DelayBeforeDiscard = delay;
        }
        
        // === Initialization
        protected override void OnFullyInitialized() {
            Character.ListenTo(ICharacter.Events.OnAttackEnd, _ => DiscardAfter().Forget(), this);
            Character.ListenTo(EnemyBaseClass.Events.AttackInterrupted, _ => DiscardAfter().Forget(), this);
        }

        async UniTaskVoid DiscardAfter() {
            _cancellationToken = new CancellationTokenSource();
            if (await AsyncUtil.DelayTime(this, DelayBeforeDiscard, _cancellationToken.Token)) {
                Discard();
            }
        }
        
        // === IDuration
        public void Prolong(IDuration duration) {
            _cancellationToken?.Cancel();
            _cancellationToken = null;
        }
        public void Renew(IDuration duration) {
            _cancellationToken?.Cancel();
            _cancellationToken = null;
        }
        public void ResetDuration() {
            _cancellationToken?.Cancel();
            _cancellationToken = null;
        }
        public void ReduceTime(float percentage) { }

        public bool Equals(UntilEndOfAttack other) {
            return Character == other?.Character;
        }
    }
}