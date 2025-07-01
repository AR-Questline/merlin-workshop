using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.Utility.Animations;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Spyglass {
    public class CharacterSpyglass : CharacterHand {
        [SerializeField] GameObject visuals;
        Spyglass _spyglass;
        
        // === Initialization
        protected override void OnInitialize() {
            base.OnInitialize();
            
            _spyglass = Target.AddElement<Spyglass>();
            if (Owner?.Character != null) {
                AttachWeaponEventsListener();
            }
        }
        
        protected override void OnAttachedToNpc(NpcElement npcElement) {
            // Spyglass can't be used by NPC
        }
        
        // === LifeCycle
        public override void ShowWeapon() {
            visuals.SetActive(true);
            base.ShowWeapon();
        }

        // === Animation Event Callbacks
        public override void OnToolInteractionStart() {
            if (_spyglass is { HasBeenDiscarded: false, IsInZoom: false }) {
                _spyglass.EnableSpyglassZoom();
                visuals.SetActive(false);
            }
        }
        
        public override void OnToolInteractionEnd() {
            if (_spyglass is { HasBeenDiscarded: false, IsInZoom: true }) {
                _spyglass.DisableSpyglassZoom();
                visuals.SetActive(true);
            }
        }
        
        // === Discarding
        protected override IBackgroundTask OnDiscard() {
            _spyglass?.Discard();
            _spyglass = null;
            return base.OnDiscard();
        }
    }
}