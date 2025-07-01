using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Character {
    /// <summary>
    /// This element hides enemy from player. For now it:
    /// - Hides enemy health bar
    /// - Hides enemy compass marker
    /// - Disables aggro music
    /// </summary>
    public partial class HideEnemyFromPlayer : Element<Location>, ICanMoveProvider {
        public sealed override bool IsNotSaved => true;

        public bool CanMove { get; }

        public HideEnemyFromPlayer() : this(false) { }
        
        public HideEnemyFromPlayer(bool canMove) {
            CanMove = canMove;
        }
        
        protected override void OnInitialize() {
            ParentModel.AddElement(new HideHealthBar());
            NpcElement npcElement = ParentModel.TryGetElement<NpcElement>();
            if (npcElement != null) {
                npcElement.AddElement(new HideCompassMarker());
                npcElement.AddElement(new DisableAggroMusicMarker());
                NpcCanMoveHandler.AddCanMoveProvider(npcElement, this);
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.RemoveElementsOfType<HideHealthBar>();
            NpcElement npcElement = ParentModel.TryGetElement<NpcElement>();
            if (npcElement != null) {
                npcElement.RemoveElementsOfType<HideCompassMarker>();
                npcElement.RemoveElementsOfType<DisableAggroMusicMarker>();
                NpcCanMoveHandler.RemoveCanMoveProvider(npcElement, this);
            }
        }
    }
}