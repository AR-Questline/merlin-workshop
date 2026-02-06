using Awaken.TG.Main.Skills.Passives;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    public abstract class PassiveSpawnerUnit : PassiveUnit, IGraphElementWithData {
        bool _isRefreshing;
        
        protected abstract IPassiveEffect Passive(Skill skill, Flow flow);
        protected virtual bool IsModified(IPassiveEffect currentPassive, Flow flow, out IPassiveEffect newPassive) {
            newPassive = null;
            return false;
        }

        public override void Enable(Skill skill, Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            if (data.passive != null) return;
            EnableInternal(skill, data, Passive(skill, flow));
        }

        void EnableInternal(Skill skill, Data data, IPassiveEffect passive) {
            if (passive == null) return;
            data.passive = passive;
            skill.AddElement(passive);
        }

        public override void Disable(Skill skill, Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            if (data.passive == null) return;
            DisableInternal(data);
        }

        void DisableInternal(Data data) {
            var passive = data.passive;
            data.passive = null;
            passive.Discard();
        }
        
        public void Refresh(Skill skill, Flow flow) {
            if (_isRefreshing) {
                return;
            }
            var data = flow.stack.GetElementData<Data>(this);
            if (!IsModified(data.passive, flow, out var newPassive)) {
                return;
            }

            _isRefreshing = true;
            DisableInternal(data);
            EnableInternal(skill, data, newPassive);
            _isRefreshing = false;
        }
        
        public IGraphElementData CreateData() {
            return new Data();
        }

        protected class Data : IGraphElementData {
            public IPassiveEffect passive;
        }
    }
}