using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Clearing {
    [UnityEngine.Scripting.Preserve]
    public class ConditionNpcKilled : Condition {
        [SerializeField, DisableInPlayMode, Indent] LocationSpec location;
        
        Location Location => World.ByID<Location>(location.GetLocationId());
        
        protected override void Setup() {
            if (Location == null) {
                Fulfill();
            } else if (!Location.TryGetElement(out NpcElement npc)) {
                Log.Important?.Error($"Cannot find NpcElement in {Location}", Location.Spec);
                Fulfill();
            } else {
                npc.ListenTo(IAlive.Events.BeforeDeath, _ => {
                    Fulfill();
                }, Owner);
            }
        }
    }
}
