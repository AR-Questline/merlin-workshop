using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.ItemBasedBehaviours;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemAddNpcBehaviour : Element<Item>, IRefreshedByAttachment<ItemAddNpcBehaviourSpec> {
        public override ushort TypeForSerialization => SavedModels.ItemAddNpcBehaviour;

        EnemyBehaviourBase _enemyBehaviourBase;
        ItemAddNpcBehaviourSpec _spec;
        
        public void InitFromAttachment(ItemAddNpcBehaviourSpec spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            if (ParentModel?.Character is NpcElement npcElement) {
                OnLearn(npcElement.ParentModel);
            }
            ParentModel.ListenTo(IItemOwner.Relations.OwnedBy.Events.Changed, OnOwnedByChanged, this);
        }

        void OnOwnedByChanged(RelationEventData data) {
            if (data.to is Location location && location.HasElement<NpcElement>()) {
                OnOwnerChanged(location);
                return;
            } 
            OnOwnerChanged(null);
        }
        
        void OnOwnerChanged(Location location) {
            if (location == null) {
                OnForget();
            } else {
                OnLearn(location);
            }
        }

        void OnLearn(Location location) {
            if (_enemyBehaviourBase != null) {
                if (_enemyBehaviourBase.ParentModel == location) {
                    return;
                } else {
                    OnForget();
                }
            }
            
            if (location.TryGetElement<EnemyBaseClass>(out var baseClass)) {
                _enemyBehaviourBase = _spec.Behaviour.Copy();
                if (_enemyBehaviourBase is ItemRequiringBehaviourForwarder itemRequiringBehaviourForwarder) {
                    itemRequiringBehaviourForwarder.ItemItsAddedTo = ParentModel;
                }
                baseClass.AddTemporaryBehaviour(_enemyBehaviourBase);
            }
        }

        void OnForget() {
            if (_enemyBehaviourBase is {HasBeenDiscarded: false}) {
                _enemyBehaviourBase.ParentModel.RemoveTemporaryBehaviour(_enemyBehaviourBase);
                _enemyBehaviourBase = null;
            }
        }
    }
}