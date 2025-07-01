using Awaken.Utility;
using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public partial class UntilEndOfFightDuration : NonEditableDuration<IWithDuration>, IEquatable<UntilEndOfFightDuration> {
        public override ushort TypeForSerialization => SavedModels.UntilEndOfFightDuration;

        [Saved] ICharacter _character;
        
        public override bool Elapsed => false;
        public override string DisplayText => LocTerms.UntilEndOfFightDuration;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] UntilEndOfFightDuration() { }

        public UntilEndOfFightDuration(ICharacter character) {
            _character = character;
        }

        protected override void OnFullyInitialized() {
            _character.ListenTo(ICharacter.Events.CombatExited, Discard, this);
        }

        public bool Equals(UntilEndOfFightDuration other) {
            return _character == other?._character;
        }
    }
}