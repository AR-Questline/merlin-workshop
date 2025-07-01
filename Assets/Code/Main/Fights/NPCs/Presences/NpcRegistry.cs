using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Fights.NPCs.Presences {
    public class NpcRegistry : IDomainBoundService {
        public Domain Domain => Domain.Gameplay;

        const string DeadNpcsKey = "dead.npcs";
        static ContextualFacts DeadNpcs => World.Services.Get<GameplayMemory>().Context(DeadNpcsKey);

        readonly Dictionary<LocationTemplate, NpcElement> _npcByLocationTemplate = new();

        public bool RemoveOnDomainChange() {
            _npcByLocationTemplate.Clear();
            return true;
        }

        public void RegisterNpc(NpcElement npc) {
            if (npc.ParentModel.Template is { } template) {
                _npcByLocationTemplate.TryAdd(template, npc);
            }
        }
        
        public void UnregisterNpc(NpcElement npc) {
            if (npc.ParentModel.Template is {} template) {
                _npcByLocationTemplate.Remove(template);
            }
        }

        public bool TryGetNpc(LocationTemplate template, out NpcElement npc) {
            return _npcByLocationTemplate.TryGetValue(template, out npc);
        }
        
        public bool IsAlive(ActorRef actorRef) {
            Actor actor = actorRef.Get();
            (LocationTemplate location, NpcElement npc) = _npcByLocationTemplate.FirstOrDefault(x => x.Value.Actor == actor);
            return npc != null && IsAlive(location);
        }

        public static void MarkAsDead(LocationTemplate template) {
            if (template != null) {
                DeadNpcs.Set(template.GUID, true);
            }
        }
        
        public static void Resurrect(LocationTemplate template) {
            var dead = DeadNpcs.Get(template.GUID, false);
            if (!dead) {
                return;
            }
            DeadNpcs.Set(template.GUID, false);
            foreach (var presence in World.All<NpcPresence>()) {
                if (presence.Template == template) {
                    presence.Init();
                }
            }
        }

        public static bool IsAlive(LocationTemplate template) {
            if (template != null) {
                return !DeadNpcs.Get(template.GUID, false);
            } else {
                return false;
            }
        }
    }
}