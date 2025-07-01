using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using UnityEngine;
using Compass = Awaken.TG.Main.Maps.Compasses.Compass;

namespace Awaken.TG.Main.Heroes.Spyglass {
    public partial class Spyglass : Element<Item>, IDisableHeroActions {
        public sealed override bool IsNotSaved => true;

        SpyglassFSM _spyglassFSM;

        public bool IsInZoom { get; private set; }

        public bool HeroActionsDisabled(IHeroAction _) {
            return IsInZoom;
        }

        // === Initialization
        protected override void OnInitialize() {
            Hero hero = Hero.Current;
            _spyglassFSM = hero.Element<SpyglassFSM>();
            hero.ListenTo(Hero.Events.HideWeapons, InstantSpyglassExit, this);
            hero.ListenTo(Hero.Events.ShowWeapons, InstantSpyglassExit, this);
            hero.ListenTo(HeroItems.Events.QuickSlotItemUsedWithDelay, InstantSpyglassExit, this);
            hero.ListenTo(CrimePenalties.Events.CrimePenaltyWentToJailFromCombat, InstantSpyglassExit, this);
            hero.ListenTo(SCloseHeroEyes.Events.EyesClosed, InstantSpyglassExit, this);
            hero.HeroItems.ListenTo(HeroLoadout.Events.LoadoutChanged, InstantSpyglassExit, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<IHeroInvolvement>(), this, InstantSpyglassExit);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<QuickUseWheelUI>(), this, InstantSpyglassExit);
            base.OnInitialize();
        }

        // === Listener Callbacks
        void InstantSpyglassExit() {
            DisableSpyglassZoom();
            _spyglassFSM.SetCurrentState(HeroStateType.Empty, 0);
        }

        // === Public API
        public void EnableSpyglassZoom() {
            if (IsInZoom) {
                return;
            }

            IsInZoom = true;
            ParentModel.AddElement<SpyglassMask>();
            PacifistMarker pacifistMarker = Hero.Current.AddElement<PacifistMarker>();
            pacifistMarker.MarkedNotSaved = true;
            Hero.Current.FoV.ApplySpyglassZoom(true);
            Hero.Current.Hide();
        }

        public void DisableSpyglassZoom() {
            if (!IsInZoom) {
                return;
            }

            ParentModel.RemoveElementsOfType<SpyglassMask>();
            Hero.Current.RemoveElementsOfType<PacifistMarker>();
            Hero.Current.FoV.ApplySpyglassZoom(false);
            Hero.Current.Show();
            IsInZoom = false;
        }

        public static void TryPlaceMarker() {
            if (World.Services.Get<SceneService>().IsOpenWorld) {
                var raycaster = Hero.Current.VHeroController.Raycaster;
                var raycasterTransform = raycaster.gameObject.transform;
                var markerPlacementPoint = raycaster.MarkerPlacementDetection.Raycast(raycasterTransform.position, raycasterTransform.forward, 1000f).Point;

                var compass = World.Any<Compass>();
                if (compass?.SpyglassMarkerCoords != null && Vector3.SqrMagnitude(markerPlacementPoint - (Vector3)compass.SpyglassMarkerCoords) < 1) {
                    compass.TryRemoveSpyglassMarker(compass.SpyglassMarkerLocation);
                } else if (markerPlacementPoint != Vector3.zero) {
                    compass?.CreateSpyglassMarker(markerPlacementPoint);
                }
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop) {
                InstantSpyglassExit();
            }

            base.OnDiscard(fromDomainDrop);
        }
    }
}