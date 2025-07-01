using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.EditorOnly {
    public class DummifyNPCs : MonoBehaviour {
        public bool shouldRemoveHelmet = true;
        public int gridSize = 2;
        public int gridRows = 3;

        void Awake() {
            NpcElement.DEBUG_DoNotSpawnAI = true;
            if (shouldRemoveHelmet) {
                World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<NpcElement>(), OnNpcAdded);
            }
        }

        void OnNpcAdded(Model obj) {
            if (obj is not NpcElement npc) return;
            npc.ParentModel.ListenToLimited(Location.Events.VisualLoaded, _ => RemoveHelmets(npc).Forget(), null);
        }

        static async UniTaskVoid RemoveHelmets(NpcElement npc) {
            await UniTask.DelayFrame(3);
            
            if (npc.HasBeenDiscarded) return;
            
            npc.Inventory.Items.ToArray()
               .Where(item => item.EquipmentType == EquipmentType.Helmet)
               .ForEach(item => {
                   if (item.IsEquipped) {
                       npc.Inventory.Unequip(item);
                   }
                   item.Discard();
               });
        }

        void Update() {
            var iter = 0;
            foreach (NpcElement npc in World.All<NpcElement>().ToArraySlow().OrderBy(npc => npc.ParentModel.DebugName)) {
                npc.ParentModel.MoveAndRotateTo(new Vector3((iter / gridRows) * gridSize, 0, (iter % gridRows) * gridSize), npc.ParentModel.Rotation);
                iter++;
            }
        }
    }
}