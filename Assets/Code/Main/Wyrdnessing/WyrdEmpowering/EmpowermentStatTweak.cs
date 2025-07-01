using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Wyrdnessing.WyrdEmpowering {
    public sealed partial class EmpowermentStatTweak : StatTweak {
        public override bool IsNotSaved => true;

        public EmpowermentStatTweak(Stat tweakedStat, float modifier, OperationType operation, Model parentModel) 
            : base(tweakedStat, modifier, null, operation, parentModel) { }
    }
}