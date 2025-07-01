using System.Reflection;
using Awaken.TG.Utility.Reflections;
using Awaken.Utility.Collections;
using Awaken.Utility.UI;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Awaken.ECS.Debugging {
    public class DebugSystemsWindow : UGUIWindowDisplay<DebugSystemsWindow> {
        static readonly FieldInfo UpdateListField = typeof(ComponentSystemGroup).GetField("m_MasterUpdateList", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
        static readonly FieldInfo UnmanagedSystemsInfo = typeof(ComponentSystemGroup).GetField("m_UnmanagedSystemsToUpdate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

        OnDemandCache<ComponentSystemGroup, bool> _expandedStates = new(static _ => false);

        protected override string Title => "Ecs systems";
        protected override bool WithSearch => false;

        protected override void DrawWindow() {
            var world = World.DefaultGameObjectInjectionWorld;
            ProcessManagedSystem(world.GetExistingSystemManaged<InitializationSystemGroup>());
            GUILayout.Space(8);
            ProcessManagedSystem(world.GetExistingSystemManaged<SimulationSystemGroup>());
            GUILayout.Space(8);
            ProcessManagedSystem(world.GetExistingSystemManaged<PresentationSystemGroup>());
        }

        void ProcessManagedSystem(ComponentSystemBase system) {
            if (system is ComponentSystemGroup group) {
                GUILayout.BeginHorizontal();
                var expanded = _expandedStates[group];
                string arrow = expanded ? "\u25BC" : "\u25B6";
                if (GUILayout.Button(arrow, TGGUILayout.LabelStyle, GUILayout.ExpandWidth(false))) {
                    _expandedStates[group] = !expanded;
                }
                DrawSystemData(system);
                GUILayout.EndHorizontal();
            } else {
                DrawSystemData(system);
                return;
            }

            if (!_expandedStates[group]) {
                return;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            UnsafeList<SystemHandle> unmanagedSystems = (UnsafeList<SystemHandle>)UnmanagedSystemsInfo.GetValue(group);

            UnsafeList<UpdateIndex> updateList = default;
            bool needToDeallocateUpdateList = false;
            var managedSystems = group.ManagedSystems;
            try {
                updateList = UpdateListField.GetStructureValueBitwise<UnsafeList<UpdateIndex>>(group);
            } catch {
                needToDeallocateUpdateList = true;
                updateList = new UnsafeList<UpdateIndex>(managedSystems.Count, ARAlloc.Temp);
                for (int i = 0; i < managedSystems.Count; i++) {
                    updateList.Add(new UpdateIndex(i, true));
                }
            }

            GUILayout.BeginVertical();
            for (int i = 0; i < updateList.Length; i++) {
                var updateThing = updateList[i];
                if (updateThing.IsManaged) {
                    var childSystem = managedSystems[updateThing.Index];
                    ProcessManagedSystem(childSystem);
                } else {
                    ref var child = ref unmanagedSystems.ElementAt(updateThing.Index);
                    DrawSystemData(ref child);
                }
            }

            if (needToDeallocateUpdateList) {
                updateList.Dispose();
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        void DrawSystemData(ComponentSystemBase systemBase) {
            systemBase.Enabled = GUILayout.Toggle(systemBase.Enabled, systemBase.GetType().Name);
        }

        void DrawSystemData(ref SystemHandle systemHandle) {
            ref var systemState =
                ref World.DefaultGameObjectInjectionWorld.Unmanaged.ResolveSystemStateRef(systemHandle);
            systemState.Enabled = GUILayout.Toggle(systemState.Enabled, systemState.DebugName.ToString());
        }

        // Copy from Unity
        struct UpdateIndex {
            ushort _data;

            public bool IsManaged => (_data & 0x8000) != 0;
            public int Index => _data & 0x7fff;

            public UpdateIndex(int index, bool managed) {
                _data = (ushort)index;
                _data |= (ushort)((managed ? 1 : 0) << 15);
            }

            public override string ToString() {
                return IsManaged ? "Managed: Index " + Index : "UnManaged: Index " + Index;
            }
        }
    }
}
