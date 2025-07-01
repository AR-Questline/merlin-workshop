using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.AI {
    public partial class TargetOverrideElement : Element<ICharacter> {
        public sealed override bool IsNotSaved => true;

        readonly ICharacter _target;
        protected readonly int _priority;
        protected readonly Status _status;

        protected bool _active;
        
        protected virtual ICharacter Target => _active ? _target : null;
        protected virtual int Priority => _priority;

        public virtual bool IsValid => !_active || _target is { IsAlive: true, HasBeenDiscarded: false };
        
        [UnityEngine.Scripting.Preserve]
        public static TargetOverrideElement AddTargetOverride(ICharacter character, ICharacter target, int priority, Status status) {
            return character.AddElement(new TargetOverrideElement(target, priority, status));
        }
        
        public TargetOverrideElement(ICharacter target, int priority, Status status = null) {
            _target = target;
            _priority = priority;
            _status = status;
        }

        protected override void OnInitialize() {
            if (_target is NpcElement targetNpc) {
                targetNpc.ListenTo(IAlive.Events.BeforeDeath, Discard, this);
                if (!targetNpc.ParentModel.IsVisualLoaded) {
                    targetNpc.ParentModel.OnVisualLoaded(Init);
                    return;
                }
            }

            _target.ListenTo(Events.BeforeDiscarded, Discard, this);
            Init(null);
        }

        void Init(Transform _) {
            _active = true;
            
            if (GetTarget(ParentModel) == Target) {
                if (ParentModel is NpcElement npc) {
                    npc.NpcAI.EnterCombatWith(Target, true);
                }
            }
        }

        public static ICharacter GetTarget(ICharacter npc) {
            var targetOverrides = npc.Elements<TargetOverrideElement>();
            int maxPriority = -1;
            ICharacter target = null;
            foreach (var targetOverride in targetOverrides.Reverse()) {
                if (!targetOverride.IsValid) {
                    targetOverride.Discard();
                    continue;
                }
                if (targetOverride.Priority > maxPriority) {
                    maxPriority = targetOverride.Priority;
                    target = targetOverride.Target;
                }
            }
            return target;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (fromDomainDrop) {
                return;
            }

            if (_status is { HasBeenDiscarded: false }) {
                _status.Discard();
            }

            if (IsValid && ParentModel is NpcElement npc) {
                npc.RecalculateTarget();
            }
        }
    }
}
