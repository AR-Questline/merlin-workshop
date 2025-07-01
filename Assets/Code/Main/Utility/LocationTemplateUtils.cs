using System.Linq;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Utility {
    public static class LocationTemplateUtils {
        public static bool IsHostile(this LocationTemplate template, Faction faction) {
            using var directAttachments = template.DirectAttachments;
            var attachment = directAttachments.value.FirstOrDefault(a => a is NpcAttachment);
            if (attachment is not NpcAttachment npcAttachment) {
                return false;
            }

            var factionTemplate = npcAttachment.NpcTemplate.Faction;
            var locationFaction = World.Services.Get<FactionService>().FactionByTemplate(factionTemplate);
            return locationFaction.IsHostileTo(faction);
        }
    }
}
