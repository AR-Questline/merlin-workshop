using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Stats
{
    /// <summary>
    /// This stat is used as blind stat in characters that don't use given stat.
    /// For example if we have a skill that does 
    /// </summary>
    public sealed partial class InvalidStat : Stat {
        public override ushort TypeForSerialization => SavedTypes.InvalidStat;

        // === Constructors

        public InvalidStat(IWithStats owner, StatType type, float baseValue) : base(owner, type, baseValue) {}

        [UnityEngine.Scripting.Preserve]
        InvalidStat() { }

        // === Increases/decreases

        public override bool IncreaseBy(float amount, ContractContext context = null) {
            return false;
        }

        public override bool DecreaseBy(float amount, ContractContext context = null) {
            return false;
        }

        // === New logic 

        public override bool SetTo(float newValue, bool runHooks = true, ContractContext context = null) {
            return false;
        }

        public override float ModifiedValue => BaseValue;
    }
}
