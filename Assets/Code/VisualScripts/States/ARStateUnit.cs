using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.States {
    public class ARStateUnit : State, IARStateUnit {
        public virtual string Summary => null;
        
        protected GameObject GameObject(Flow flow) => flow.stack.gameObject;
        protected IModel Model(Flow flow) => Variables.Object(GameObject(flow)).Get<IModel>(VGUtils.ModelVariableName);
        [UnityEngine.Scripting.Preserve] 
        protected NpcElement NpcElement(Flow flow) => Model(flow).Element<NpcElement>();
        [UnityEngine.Scripting.Preserve] 
        protected IView View(Flow flow) => GameObject(flow).GetComponentInParent<IView>();
    }
}