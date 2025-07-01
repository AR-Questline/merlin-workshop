using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;

namespace Awaken.TG.Main.Heroes.Items {
    public partial class ItemSkillsInvoker : Element<Item> {
        public sealed override bool IsNotSaved => true;

        Item Item => ParentModel;
        ICharacter Character => Item.Owner?.Character;
        IEnumerable<Skill> AllSkills => ParentModel.ActiveSkills;
        bool HasToMeetRequirements => Character is Hero && !ParentModel.IsMagic;

        protected override void OnInitialize() {
            ParentModel.ListenTo(IItemOwner.Relations.OwnedBy.Events.AfterEstablished, OnLearn, this);
            ParentModel.ListenTo(IItemOwner.Relations.OwnedBy.Events.BeforeDisestablished, OnForget, this);
        }
        
        // === Performing
        public void PerformImmediate(ItemActionType actionType) {
            foreach (IItemAction itemAction in Item.ActionsFor(actionType).ToArray()) {
                StartPerforming(itemAction);
                CancelPerforming(itemAction);
                if (!itemAction.HasBeenDiscarded) {
                    itemAction.AfterPerformed();
                }
            }
            if (!WasDiscarded) {
                ParentModel.TriggerChange();
            } else {
                var itemsUI = World.Any<ItemsUI>();
                itemsUI?.Trigger(ItemsUI.Events.ItemsCollectionChanged, itemsUI.Items);
            }
        }

        public void StartPerforming(ItemActionType actionType) {
            foreach (IItemAction itemAction in Item.ActionsFor(actionType).ToArray()) {
                StartPerforming(itemAction);
            }
            ParentModel.TriggerChange();
        }
        
        void StartPerforming(IItemAction action) {
            if (action is IItemSkillOwner skillOwner) {
                if (Character != null) {
                    skillOwner.IncrementPerformCount();
                    ForEachSkill(skillOwner.Skills, static s => {
                        if (s.CanSubmit) {
                            s.Submit();
                        }
                    });
                }
            }
            ParentModel.Trigger(Item.Events.BeforeActionPerformed, new ItemActionEvent(action.Type, ParentModel));

            action.Submit();

            if (!HasBeenDiscarded && !ParentModel.HasBeenDiscarded) {
                ParentModel.Trigger(Item.Events.ActionPerformed, new ItemActionEvent(action.Type, ParentModel));
            }
        }

        public void EndPerforming(ItemActionType actionType) {
            foreach (IItemAction itemAction in Item.ActionsFor(actionType).ToArray()) {
                EndPerforming(itemAction);
            }

            ParentModel.TriggerChange();
        }
        
        void EndPerforming(IItemAction action) {
            if (action is IItemSkillOwner skillOwner) {
                if (Character != null) {
                    ForEachSkill(skillOwner.Skills, static s => s.Perform());
                }
            }
            action.Perform();
        }
        
        public void CancelPerforming(ItemActionType actionType) {
            foreach (IItemAction itemAction in Item.ActionsFor(actionType).ToArray()) {
                CancelPerforming(itemAction);
            }
            if (!WasDiscarded) {
                ParentModel.TriggerChange();
            }
        }
        
        void CancelPerforming(IItemAction action) {
            if (action is IItemSkillOwner skillOwner) {
                if (Character != null) {
                    ForEachSkill(skillOwner.Skills, static s => s.Cancel());
                }
            }
            action.Cancel();
        }
        
        // === Callbacks
        public void OnEquip() {
            if (Character is not null && (!HasToMeetRequirements || HeroRequirementsMet())) {
                ForEachSkill(static s => s.Equip());
            }
        }

        public void OnUnequip() {
            ForEachSkill(static s => s.Unequip());
        }

        void OnLearn(RelationEventData _) {
            if (Character == null) {
                return;
            }
            
            Character.Skills.ListenTo(Events.AfterChanged, RefreshContext, this);
            ForEachSkill(static s => s.Learn());
        }

        void OnForget(RelationEventData _) {
            if (Character != null) {
                World.EventSystem.RemoveAllListenersBetween(Character, this);
                World.EventSystem.RemoveAllListenersBetween(Character.Skills, this);
            }
            ForEachSkill(static s => s.Forget());
        }

        bool HeroRequirementsMet() {
            return ParentModel.StatsRequirements is not { RequirementsMet: false };
        }

        void ForEachSkill(Action<Skill> action) {
            ForEachSkill(AllSkills, action);
        }

        void ForEachSkill(IEnumerable<Skill> skills, Action<Skill> action) {
            foreach (var skill in skills) {
                action(skill);
                if (WasDiscarded || Item.WasDiscarded) {
                    break;
                }
            }
        }

        void RefreshContext() {
            foreach (var skill in AllSkills) {
                skill.Trigger(Skill.Events.ContextChanged, skill);
            }
        }
        
        public void RequirementsChanged(bool met) {
            if (!ParentModel.IsEquipped) {
                return;
            }

            if (!HasToMeetRequirements) {
                return;
            }
            
            if (met) {
                OnEquip();
            } else {
                OnUnequip();
            }
        }
    }
}