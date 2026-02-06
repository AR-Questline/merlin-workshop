using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;

namespace Awaken.TG.Main.Heroes.HUD.Enemies {
    public class EnemyStatusHUD : StatusHUD {
        public void Refresh(VCEnemyHealthBar.EnemyHealthBarData healthBarData) {
            if (!healthBarData.isShown) {
                Clear();
                return;
            }
            
            Location loc = healthBarData.location;
            if (loc == null) {
                Clear();
                return;
            }

            NpcElement npcElement = loc.TryGetElement<NpcElement>();
            if (!npcElement) {
                Clear();
                return;
            }
            
            Init(npcElement);
        }
    }
}
