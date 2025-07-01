using System;
using Awaken.TG.Main.Skills;

namespace Awaken.TG.Main.Heroes.Development.WyrdPowers {
    [Serializable]
    public class WyrdSoulFragment {
        public WyrdSoulFragmentType fragmentType;
        public SkillReference skillReference;
    }
}