using Awaken.TG.Main.AI.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.AI.SummonsAndAllies {
    public partial class NpcAISummon : Element<NpcElement>, INpcSummon {
        public override ushort TypeForSerialization => SavedModels.NpcAISummon;

        [Saved] float _manaExpended;
        [Saved] WeakModelRef<ICharacter> _owner;
        public ICharacter Owner => _owner.TryGet(out ICharacter owner) ? owner : null;
        public CharacterLimitedLocationType Type => CharacterLimitedLocationType.NpcSummon;
        public float ManaExpended => _manaExpended;
        public bool IsAlive => !HasBeenDiscarded && ParentModel.IsAlive;
        Location Location => ParentModel.ParentModel;

        // === Constructing
        [JsonConstructor, UnityEngine.Scripting.Preserve] public NpcAISummon() {}
        
        public NpcAISummon(ICharacter owner, float manaExpended) {
            _owner = new WeakModelRef<ICharacter>(owner);
            _manaExpended = manaExpended;
        }

        protected override void OnFullyInitialized() {
            Location.RemoveElementsOfType<SearchAction>();
            Location.RemoveElementsOfType<PickpocketAction>();
            Location.OnVisualLoaded(Init);
        }

        void Init(Transform parentTransform) {
            Owner?.Trigger(INpcSummon.Events.SummonSpawned, this);
        }

        // === ICharacterLimitedLocation

        public int LimitForCharacter(ICharacter character) => GameConstants.Get.npcSummonLimit;

        public void Destroy() {
            ParentModel.ParentModel.Kill();
        }
    }
}