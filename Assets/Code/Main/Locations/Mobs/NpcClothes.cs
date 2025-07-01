using Awaken.TG.Main.Fights.NPCs;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Mobs {
    public partial class NpcClothes : BaseClothes<Location> {
        NpcElement _parentNpc;
        public sealed override bool IsNotSaved => true;
        protected NpcElement ParentNpc => _parentNpc ??= ParentModel.TryGetElement<NpcElement>();

        protected override Transform ParentTransform {
            get {
                var npcElement = ParentNpc;
                if (npcElement != null) {
                    return npcElement.ParentTransform;
                }
                var npcDummy = ParentModel.TryGetElement<NpcDummy>();
                if (npcDummy != null) {
                    return npcDummy.ParentTransform;
                }
                return null;
            }
        }

        protected override bool IsKandraHidden {
            get {
                var npcElement = ParentNpc;
                if (npcElement == null) {
                    return false;
                }
                return npcElement.IsKandraHidden;
            }
        }
    }
}
