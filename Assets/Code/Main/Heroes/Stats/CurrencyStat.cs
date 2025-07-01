using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Stats {
    public sealed partial class CurrencyStat : Stat {
        public override ushort TypeForSerialization => SavedTypes.CurrencyStat;

        public CurrencyStat(IWithStats owner, StatType type, float initialValue) : base(owner, type, initialValue) {
            
        }
        
        [UnityEngine.Scripting.Preserve]
        CurrencyStat() { } // serialization only
        
        public override bool SetTo(float newValue, bool runHooks = true, ContractContext context = null) {
            if (newValue < 0) {
                newValue = 0;
            }
            
            if (runHooks) {
                newValue = RunHooks(newValue, context, out bool prevented);
                if (prevented) {
                    return false;
                }
            }
            newValue = Mathf.Round(newValue);
            InternalSetTo(newValue, context);
            return true;
        }
    }
}
