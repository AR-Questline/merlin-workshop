using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.Utility.Automation;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.Tests.Items {
    public class ItemsAttackPerSecondValidation : IAutomation {
        const string Name = "attacksPerSecond";
        
        [RuntimeInitializeOnLoadMethod]
        static void Register() {
            Automations.TryRegister(Name, new ItemsAttackPerSecondValidation());
        }
        
        public UniTask Run(string[] parameters) {
            return WeaponsTestAttacksPerSecondForHero.TestAllWeapons(false);
        }
    }
}
