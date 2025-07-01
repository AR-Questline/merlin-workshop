using Awaken.Utility;
using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Fights.Factions.Markers {
    public partial class CharacterAntagonism : AntagonismMarker, IEquatable<CharacterAntagonism> {
        public override ushort TypeForSerialization => SavedModels.CharacterAntagonism;

        [Saved] public ICharacter Character { get; private set; }

        [JsonConstructor, UnityEngine.Scripting.Preserve] CharacterAntagonism() { }
        public CharacterAntagonism(AntagonismLayer layer, AntagonismType type, ICharacter character, Antagonism antagonism) : base(layer, type, antagonism) {
            Character = character;
        }

        protected override void OnInitialize() {
            ParentModel.Trigger(FactionService.Events.AntagonismChanged, ParentModel);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (!ParentModel.HasBeenDiscarded) {
                ParentModel.Trigger(FactionService.Events.AntagonismChanged, ParentModel);
            }
        }

        protected override bool RefersTo(IWithFaction withFaction) {
            return withFaction == Character;
        }

        public bool Equals(CharacterAntagonism other) {
            return Character == other?.Character && base.Equals(other);
        }
    }
}