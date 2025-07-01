using Awaken.TG.Main.AI;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Maps.Compasses {
    public partial class NpcCompassMarker : CompassMarker {
        NpcMarker NpcMarker => (NpcMarker)Marker;
        NpcElement NpcElement => _npcElement ??= NpcMarker.ParentModel.TryGetElement<NpcElement>();
        NpcElement _npcElement;

        bool _isHostileToHero;
        bool _isFriendlyToHero;
        bool _inCombat;
        bool _hideMarker;
        bool _showOutsideOfCombat;
        bool _hasAggro;

        protected override bool CalculateEnabled => !_hideMarker && !NpcPresence.InAbyss(NpcElement.Coords) && base.CalculateEnabled && (!_inCombat || _hasAggro) && (_showOutsideOfCombat || _inCombat);
        bool IsHeroSummon => NpcElement?.IsHeroSummon ?? false;

        public NpcCompassMarker(LocationMarker marker) : base(marker) {}

        protected override void OnInitialize() {
            _showOutsideOfCombat = Marker.MarkerData is not NpcMarkerData { HideOutsideOfCombat: true };
            
            base.OnInitialize();
            NpcElement.ListenTo(FactionService.Events.AntagonismChanged, UpdateAntagonism, this);
            NpcElement.ListenTo(DisableAggroMusicMarker.Events.AggroSettingsChanged, UpdateAntagonism, this);
            NpcElement.ListenTo(ICharacter.Events.CombatEntered, OnEnterCombat, this);
            NpcElement.ListenTo(ICharacter.Events.CombatExited, OnExitCombat, this);
            NpcElement.ListenTo(HideCompassMarker.Events.HideCompassChanged, UpdateHideMarker, this);
            NpcElement.ParentModel.ListenTo(GroundedEvents.AfterTeleported, AfterLocationTeleported, this);

            UpdateHideMarker(NpcElement);
        }

        protected override void OnFullyInitialized() {
            UpdateAntagonism();
        }

        public void RegisterAI(NpcAI ai) {
            ai.ListenTo(NpcAI.Events.HeroVisibilityChanged, UpdateAggro, this);
            ai.ListenTo(Model.Events.BeforeDiscarded, UpdateAggro, this);
            UpdateAggro(ai);
            UpdateAntagonism();
        }

        void AfterLocationTeleported() {
            UpdateVisibility();
        }

        void OnEnterCombat(ICharacter _) {
            if (IsHeroSummon) {
                return;
            }
            
            if (!_inCombat) {
                _inCombat = true;
                UpdateVisibility();
                MarkerIconsUpdate();
                SetAlwaysVisibleExternal(true);
            }
        }
        
        void OnExitCombat(ICharacter _) {
            if (IsHeroSummon) {
                return;
            }
            
            if (_inCombat) {
                _inCombat = false;
                UpdateVisibility();
                MarkerIconsUpdate();
                SetAlwaysVisibleExternal(false);
            }
        }

        void UpdateAntagonism() {
            bool isHostileToHero = NpcElement.IsHostileTo(Hero.Current);
            bool isFriendlyToHero = NpcElement.IsFriendlyTo(Hero.Current);
            if (_isHostileToHero != isHostileToHero || _isFriendlyToHero != isFriendlyToHero) {
                _isHostileToHero = isHostileToHero;
                _isFriendlyToHero = isFriendlyToHero;
                MarkerIconsUpdate();
            }
        }

        void MarkerIconsUpdate() {
            NpcMarker.Update(_inCombat, _isHostileToHero, _isFriendlyToHero);
            UpdateIcon();
        }

        void UpdateHideMarker(ICharacter _) {
            bool hideMarker = NpcElement?.HasElement<HideCompassMarker>() ?? true;
            if (_hideMarker != hideMarker) {
                _hideMarker = hideMarker;
                UpdateVisibility();
            }
        }

        void UpdateAggro(IModel model) {
            var ai = (NpcAI)model;
            bool hasAggro = !ai.HasBeenDiscarded && ai.HeroVisibility > 0;
            if (_hasAggro != hasAggro) {
                _hasAggro = hasAggro;
                UpdateVisibility();
            }
        }
    }
}