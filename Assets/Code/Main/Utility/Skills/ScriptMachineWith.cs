using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Skills {
    /// <summary>
    /// This class is override for ScriptMachine class.
    /// It exists because we need to know which Skill owns this component, and setting graph variables in ScriptMachines is buggy :angry:
    /// </summary>
    [RequireComponent(typeof(Variables))]
    [DisableAnnotation]
    public class ScriptMachineWith<T> : EventMachine<FlowGraph, ScriptGraphAsset>, IListenerOwner where T : class {
        public T Owner { get; set; }
        
        protected override void OnDestroy() {
            base.OnDestroy();
            World.EventSystem?.RemoveAllListenersOwnedBy(this);
            Owner = null;
        }

        // --- Everything below is copied from ScriptMachine, because we can't inherit from ScriptMachine since it's sealed class.
        public override FlowGraph DefaultGraph() {
            return FlowGraph.WithStartUpdate();
        }

        protected override void OnEnable() {
            if (hasGraph) {
                graph.StartListening(reference);
            }

            base.OnEnable();
        }

        protected override void OnInstantiateWhileEnabled() {
            if (hasGraph) {
                graph.StartListening(reference);
            }

            base.OnInstantiateWhileEnabled();
        }

        protected override void OnUninstantiateWhileEnabled() {
            base.OnUninstantiateWhileEnabled();

            if (hasGraph) {
                graph.StopListening(reference);
            }
        }

        protected override void OnDisable() {
            base.OnDisable();

            if (hasGraph) {
                graph.StopListening(reference);
            }
        }

        [ContextMenu("Show Data...")]
        protected override void ShowData() {
            base.ShowData();
        }
    }
}