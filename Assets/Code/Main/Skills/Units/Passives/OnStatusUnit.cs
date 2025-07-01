using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Extensions;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class OnStatusUnit : PassiveListenerUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public StatusAction action;
        
        [Serialize, Inspectable, UnitHeaderInspectable]
        public Target target;

        [Serialize, Inspectable, UnitHeaderInspectable]
        [TemplateType(typeof(StatusTemplate))]
        public TemplateReference statusTemplate;
        
        protected override IEnumerable<IEventListener> Listeners(Skill skill, Flow flow) {
            var self = this.Skill(flow).Owner;
            var pointer = flow.stack;
            return Events.Select(evt => World.EventSystem.ListenTo(EventSelector.AnySource, evt, this.Skill(flow), status => TryPreformAction(status, self, pointer)));
        }

        IEnumerable<Event<CharacterStatuses, Status>> Events {
            get {
                if ((action & StatusAction.Remove) != 0) {
                    yield return CharacterStatuses.Events.RemovedStatus;
                }
                if ((action & StatusAction.Add) != 0) {
                    yield return CharacterStatuses.Events.AddedStatus;
                }
                if ((action & StatusAction.Extinguish) != 0) {
                    yield return CharacterStatuses.Events.ExtinguishedStatus;
                }
                if ((action & StatusAction.Discard) != 0) {
                    yield return CharacterStatuses.Events.VanishedStatus;
                }
            }
        }
         
        void TryPreformAction(Status status, ICharacter self, GraphPointer pointer) {
            if (statusTemplate.GUID == status.Template.GUID && CorrectTarget(status, self)) {
                Trigger(pointer);
            }
        }

        bool CorrectTarget(Status status, ICharacter self) {
            return target.HasFlagFast(Target.Hero) && status.Character is Hero ||
                   target.HasFlagFast(Target.Self) && status.Character == self;
        }

        [Flags]
        public enum Target {
            Self,
            Hero,
        }
        
        [Flags]
        public enum StatusAction {
            Add = 1 << 0,
            Remove = 1 << 1,
            Extinguish = 1 << 2,
            Discard = 1 << 3,
        }
    }
}