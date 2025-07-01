using System;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging {
    public class NpcComponentsDisableWindow : UGUIWindowDisplay<NpcComponentsDisableWindow> {
        Type[] _allComponentsType;
        OnDemandCache<Type, bool> _states = new(static _ => false);
        void Awake() {
            _allComponentsType = World.All<NpcElement>()
                .ToArraySlow()
                .SelectMany(static npc => npc.ParentTransform.GetComponentsInChildren<Behaviour>())
                .Select(static c => c.GetType())
                .Distinct()
                .ToArray();
        }

        protected override void DrawWindow() {
            if (GUILayout.Button("Remove npcs outside visual")) {
                World.All<NpcElement>()
                    .Where(static npc => !LocationCullingGroup.InNpcVisibilityBand(npc.CurrentDistanceBand))
                    .ToArray()
                    .ForEach(static npc => npc.Discard());
            }
            if (GUILayout.Button("Remove npcs inside visual")) {
                World.All<NpcElement>()
                    .Where(static npc => LocationCullingGroup.InNpcVisibilityBand(npc.CurrentDistanceBand))
                    .ToArray()
                    .ForEach(static npc => npc.Discard());
            }
            if (GUILayout.Button("Pause all npc's animancer")) {
                foreach (var npc in World.All<NpcElement>()) {
                    npc.Movement.Controller.ARNpcAnimancer.Playable.PauseGraph();
                }
            }
            GUILayout.Space(12);
            foreach (var componentType in _allComponentsType) {
                var componentName = componentType.Name;
                var nextState = _states[componentType];
                GUI.color = nextState ? Color.green : Color.red;
                if (SearchContext.HasSearchInterest(componentName) && GUILayout.Button(componentName)) {
                    _states[componentType] = !nextState;
                    World.All<NpcElement>()
                        .Select(static npc => npc.ParentTransform)
                        .SelectMany(tr => tr.GetComponentsInChildren(componentType))
                        .OfType<Behaviour>()
                        .ForEach(c => c.enabled = nextState);
                }
            }
        }

        [StaticMarvinButton(state: nameof(WindowIsShown))]
        static void ToggleNpcPerformanceWindow() {
            NpcComponentsDisableWindow.Toggle(UGUIWindowUtils.WindowPosition.Center);
        }

        static bool WindowIsShown() => NpcComponentsDisableWindow.IsShown;
    }
}
