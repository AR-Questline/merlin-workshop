using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class CombatPreventionTimedElement : CombatPreventionElementBase {
        public sealed override bool IsNotSaved => true;

        readonly float _time;

        public CombatPreventionTimedElement(float time) {
            _time = time;
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            DiscardAfterTime(_time).Forget();
        }

        async UniTaskVoid DiscardAfterTime(float time) {
            if (!await AsyncUtil.DelayTime(this, time)) {
                return;
            }
            Discard();
        }

        public override bool OnBeforeTakingFinalDamage(HealthElement healthElement, Damage damage) {
            return true;
        }
    }
}