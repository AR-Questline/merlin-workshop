using System;
using System.Linq;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Debugging.AssetViewer.AssetGroup {
    public class LocationAssetsGroup : PreviewAssetsGroup<LocationTemplate> {
        [SerializeField] bool disableAI = true;
        [SerializeField] bool debugAnimation = true;
        
        public override async void SpawnTemplate(LocationTemplate template, Vector3 position) {
            Location location = template.SpawnLocation(position);

            if (disableAI) {
                ModifyGameObject(template, location, DisableAI);
            }
            if (debugAnimation) {
                ModifyGameObject(template, location, AddAnimationDebugger);
            }
            
            await UniTask.DelayFrame(3);
            location.MoveAndRotateTo(position, Quaternion.Euler(0,180,0) * transform.rotation, true);
        }
        
        public static void ModifyGameObject(LocationTemplate template, Location location, Action<VDynamicLocation> action) {
            View mainView = location.MainView;
            if (mainView is VDynamicLocation dynamicLocation) {
                location.ListenTo(Location.Events.BeforeVisualLoaded, () => action(dynamicLocation), dynamicLocation);
            } else {
                Log.Important?.Warning($"No view spawned for location template: {template.name}");
            }
        }

        public static async void DisableAI(VLocation locationObject) {
            if (locationObject.Target.Template.GetAttachments().FirstOrDefault(a => a is NpcAttachment) == null) return;
            await UniTask.WaitUntil(() => locationObject.Target.TryGetElement<NpcElement>()?.TryGetElement<NpcAI>() != null);
            var npcAI = locationObject.Target.TryGetElement<NpcElement>()?.TryGetElement<NpcAI>();
            if (npcAI != null) npcAI.Discard();
            var stateMachines = locationObject.GetComponentsInChildren<StateMachine>();
            stateMachines.ForEach(s => s.enabled = false);
            var scriptMachines = locationObject.GetComponentsInChildren<ScriptMachine>();
            scriptMachines.ForEach(s => s.enabled = false);
        }
        
        public static void AddAnimationDebugger(VDynamicLocation locationObject) {
            var parent = locationObject.transform.parent;
            var animators = parent.GetComponentsInChildren<Animator>();
            foreach (Animator animator in animators) {
                parent.AddComponent<DebugAnimationRunner>().Init(animator);
            }
        }
    }
}