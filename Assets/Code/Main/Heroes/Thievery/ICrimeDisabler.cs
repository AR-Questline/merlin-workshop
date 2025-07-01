using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Thievery {
    public interface ICrimeDisabler : IElement<Model> {
        public bool IsNoCrime(in CrimeArchetype archetype);

        public static bool IsCrimeDisabled(Model target, in CrimeArchetype archetype) {
            foreach (var crimeDisabler in target.Elements<ICrimeDisabler>()) {
                if (crimeDisabler.IsNoCrime(in archetype)) {
                    return true;
                }
            }
            foreach (var crimeDisabler in Hero.Current.Elements<ICrimeDisabler>()) {
                if (crimeDisabler.IsNoCrime(in archetype)) {
                    return true;
                }
            }
            return false;
        }
    }
}