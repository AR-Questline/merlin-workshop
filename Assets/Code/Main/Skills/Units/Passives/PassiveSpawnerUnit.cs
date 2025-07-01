using Awaken.TG.Main.Skills.Passives;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    public abstract class PassiveSpawnerUnit : PassiveUnit, IGraphElementWithData {
        protected abstract IPassiveEffect Passive(Skill skill, Flow flow);
        protected virtual bool IsModified(IPassiveEffect currentPassive, Flow flow, out IPassiveEffect newPassive) {
            newPassive = null;
            return false;
        }

        public override void Enable(Skill skill, Flow flow) {
            EnableInternal(skill, flow, Passive(skill, flow));
        }

        void EnableInternal(Skill skill, Flow flow, IPassiveEffect passive) {
            if (passive == null) return;
            flow.stack.GetElementData<Data>(this).passive = passive;
            skill.AddElement(passive);
        }

        public override void Disable(Skill skill, Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            if (data.passive == null) return;
            DisableInternal(data);
        }

        void DisableInternal(Data data) {
            data.passive.Discard();
            data.passive = null;
        }
        
        public void Refresh(Skill skill, Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            if (!IsModified(data.passive, flow, out var newPassive)) {
                return;
            }
            DisableInternal(data);
            EnableInternal(skill, flow, newPassive);
        }
        
        public IGraphElementData CreateData() {
            return new Data();
        }

        protected class Data : IGraphElementData {
            public IPassiveEffect passive;
        }
    }
}