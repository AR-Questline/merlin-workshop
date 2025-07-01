using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.WyrdArthur.SoulsOverview {
    public class VCWyrdTalentType : ViewComponent {
        [SerializeField] WyrdSoulFragmentType fragmentType;
        public WyrdSoulFragmentType FragmentType => fragmentType;
    }
}
