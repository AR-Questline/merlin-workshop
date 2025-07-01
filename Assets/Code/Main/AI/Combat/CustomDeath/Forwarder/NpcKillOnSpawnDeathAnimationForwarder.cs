using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.CustomDeath.Forwarder {
    public partial class NpcKillOnSpawnDeathAnimationForwarder : DeathBehaviourUpdater, IDeathAnimationForwarder {
        public override ushort TypeForSerialization => SavedModels.NpcKillOnSpawnDeathAnimationForwarder;

        // NpcKillOnSpawnElement is element of other Location (with a NpcPresence) than forwarder (this is on a Npc), but both locations should be present on the same scene.
        [Saved] WeakModelRef<NpcKillOnSpawnElement> _killOnSpawnElement;
        public CustomDeathAnimation CustomDeathAnimation => _killOnSpawnElement.TryGet(out var killOnSpawnElement) ? killOnSpawnElement.CustomDeathAnimation : null;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        NpcKillOnSpawnDeathAnimationForwarder() { }
        public NpcKillOnSpawnDeathAnimationForwarder(NpcKillOnSpawnElement element) {
            _killOnSpawnElement = element;
        }
        
        protected override void UpdateDeathBehaviours(GameObject visualGO, CustomDeathController customDeathController) {
            if (!_killOnSpawnElement.TryGet(out var killOnSpawnElement)) {
                return;
            }
            
            customDeathController.SetRagdollOnDeath(true, null, false);
            
            if (!visualGO.TryGetComponent(out CustomDeathAnimations animations)) {
                animations = visualGO.AddComponent<CustomDeathAnimations>();
            }
            animations.AddCustomDeathAnimation(killOnSpawnElement.CustomDeathAnimation);
        }
    }
}
