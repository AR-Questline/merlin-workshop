using Awaken.TG.Main.Character;

namespace Awaken.TG.Main.Locations {
    public struct LocationInteractionData {
        public ICharacter character;
        public Location location;

        public LocationInteractionData(ICharacter c, Location l) {
            character = c;
            location = l;
        }
    }
}