using Awaken.Utility;
using System;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public partial class UntilHeroEndOfCombat: NonEditableDuration<IWithDuration>, IEquatable<UntilHeroEndOfCombat> {
        public override ushort TypeForSerialization => SavedModels.UntilHeroEndOfCombat;

        public override bool Elapsed => false;
        public override string DisplayText => string.Empty;

        static bool IsInCombat => Hero.Current.IsInCombat() || NpcDangerTracker.FleeingFromHeroTimer > 0;
        
        protected override void OnFullyInitialized() {
            Hero.Current.ListenTo(ICharacter.Events.CombatExited, TryDiscard, this);
            Hero.Current.ListenTo(NpcDangerTracker.Events.FleeingFromHeroChanged, TryDiscard, this);
            CheckIfOutOfCombat().Forget();
        }
        
        async UniTaskVoid CheckIfOutOfCombat() {
            // Two frames are required because hero enters combat one frame after attacking npc etc.
            if (!await AsyncUtil.DelayFrame(this, 2)) {
                return;
            }
            
            TryDiscard();
        }

        void TryDiscard() {
            if (!IsInCombat) {
                Discard();
            }
        }
        
        public bool Equals(UntilHeroEndOfCombat other) {
            return other != null;
        }
    }
}