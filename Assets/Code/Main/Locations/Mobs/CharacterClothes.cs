using Awaken.TG.Main.Character;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Mobs {
    public partial class CharacterClothes : BaseClothes<ICharacter> {
        public sealed override bool IsNotSaved => true;

        protected override Transform ParentTransform => ParentModel.ParentTransform;
    }
}