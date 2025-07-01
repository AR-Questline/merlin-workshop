using System;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Patchers {
    [UnityEngine.Scripting.Preserve]
    public class Patcher041_042 : Patcher {
        const string GUIDFactionEvil = "315e8bd87cef248468f30612e93787c1";
        const string KingArthurActorID = "KingArthur";
        protected override Version MaxInputVersion => new Version(0, 41);
        protected override Version FinalVersion => new Version(0, 42);
        
        public override void AfterRestorePatch() {
            var kingArthur = World.All<NpcElement>().FirstOrDefault(npc => npc.Actor.Id == KingArthurActorID);
            if (kingArthur?.Faction.Template.GUID is GUIDFactionEvil) {
                kingArthur.ResetFactionOverride();
                if (!NpcPresence.InAbyss(kingArthur.ParentModel.SavedCoords)) {
                    kingArthur.ParentModel.OnVisualLoaded(_ => kingArthur.ParentModel.MoveAndRotateTo(NpcPresence.AbyssPosition, Quaternion.identity, true));
                }
            }
        }
    }
}