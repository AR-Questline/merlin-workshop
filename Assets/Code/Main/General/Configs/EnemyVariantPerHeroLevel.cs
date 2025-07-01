using System;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.General.Configs {
    [Serializable]
    public class EnemyVariantPerHeroLevel {
        public int maxHeroLevel;
        [TemplateType(typeof(LocationTemplate))] 
        public TemplateReference enemyVariant;
    }
}
