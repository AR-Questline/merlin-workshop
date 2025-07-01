using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Fights.Duels {
    public partial class DuelistCrimeDisabler : Element<Location>, ICrimeDisabler {
        public sealed override bool IsNotSaved => true;

        readonly bool _canDie; // The character can die in duel
        readonly DuelController _duelController;

        public DuelistCrimeDisabler(bool canDie, DuelController controller) {
            _canDie = canDie;
            _duelController = controller;
        }

        public bool IsNoCrime(in CrimeArchetype archetype) {
            if (!_duelController.Started) {
                return false;
            }

            switch (archetype.CrimeType) {
                case CrimeType.Combat:
                    return true;
                case CrimeType.Murder when _canDie:
                    return true;
                default:
                    return false;
            }
        }

        Model IElement<Model>.ParentModel => ParentModel;
    }
}
