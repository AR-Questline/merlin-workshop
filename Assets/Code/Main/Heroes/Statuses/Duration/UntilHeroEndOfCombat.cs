using Awaken.Utility;
using System;
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
        
        protected override void OnFullyInitialized() {
            Hero.Current.ListenTo(ICharacter.Events.CombatExited, Discard, this);
            CheckIfOutOfCombat().Forget();
        }
        
        async UniTaskVoid CheckIfOutOfCombat() {
            // Two frames are required because hero enters combat one frame after attacking npc etc.
            if (!await AsyncUtil.DelayFrame(this, 2)) {
                return;
            }
            
            if (!Hero.Current.IsInCombat()) {
                Discard();
            }
        }
        
        public bool Equals(UntilHeroEndOfCombat other) {
            return other != null;
        }
    }
}