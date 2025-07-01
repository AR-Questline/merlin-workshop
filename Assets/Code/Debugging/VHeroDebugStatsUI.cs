using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.StatsSummary;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Debugging {
    [UsesPrefab("CharacterSheet/Overview/" + nameof(VHeroDebugStatsUI))]
    public class VHeroDebugStatsUI : View<DebugStatsUI> {
        const string DamageFormat = "{0:F2}, {1:F2}, {2:F2}";
        const string DamageDealt = "Damage dealt";
        const string DamageTaken = "Damage taken";
        
        static Hero Hero => Hero.Current;
        
        float[] _damageDealtArr = {0,0,0};
        float[] _damageTakenArr = {0,0,0};
        
        [SerializeField] VCStatsSummaryEntryUI damageTaken;
        [SerializeField] VCStatsSummaryEntryUI damageDealt;
        
        public override Transform DetermineHost() => Hero.View<VHeroHUD>().transform;
        
        protected override void OnInitialize() {
            Hero.Element<HealthElement>().ListenTo(HealthElement.Events.OnDamageTaken, OnDamageTaken, this);
            Hero.ListenTo(HealthElement.Events.OnDamageDealt, OnDamageDealt, this);
            SetupDamageData(damageTaken, DamageTaken, ref _damageTakenArr);
            SetupDamageData(damageDealt, DamageDealt, ref _damageDealtArr);
        }
        
        void OnDamageTaken(DamageOutcome damage) {
            SetupDamageData(damageTaken, DamageTaken, ref _damageTakenArr, damage);
        }
        
        void OnDamageDealt(DamageOutcome damage) {
            SetupDamageData(damageDealt, DamageDealt, ref _damageDealtArr, damage);
        }

        static void SetupDamageData(VCStatsSummaryEntryUI entry, string damageTypeName, ref float[] data, DamageOutcome? damageOutcome = null) {
            if (damageOutcome != null) {
                (data[1], data[2]) = (data[0], data[1]);
                data[0] = damageOutcome.Value.Damage.RawData.CalculatedValue;
            }
            
            entry.Override(damageTypeName, string.Format(DamageFormat, data[0], data[1], data[2]));
        }
    }
}