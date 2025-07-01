using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Sketching;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class CharacterSketchbook : CharacterWeapon {
        static readonly int SketchHash = Animator.StringToHash("Sketch");
        static readonly int InventoryHash = Animator.StringToHash("Inventory");
        
        [SerializeField] Transform offhandPart;
        [SerializeField] Renderer renderPlane;
        [SerializeField] int materialId;
        [SerializeField] float drawingFromSnapShotDelay;
        [SerializeField] float drawingTime;
        [SerializeField] Animator bookAnimator;
        Sketchbook _sketchbook;
        bool _inInventory;
        CharacterWeapon _offHandWeapon;
        ToolInteractionFSM _toolInteractionFSM;
        LowerFancyPanelNotification _lastFailNotification;

        public Material Material => renderPlane.materials[materialId];

        // === Initialization
        protected override void OnEnable() {
            if (_inInventory) {
                bookAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                bookAnimator.SetBool(InventoryHash, true);
            }
        }
        
        protected override void OnInitialize() {
            base.OnInitialize();
            _sketchbook = new Sketchbook(this);
            
            if (Owner?.Character != null) {
                AttachWeaponEventsListener();
            }

            if (offhandPart != null) {
                _offHandWeapon = offhandPart.GetComponent<CharacterWeapon>();
                World.BindView(Target, _offHandWeapon);
            }
            
            _toolInteractionFSM = Hero.Current.Element<ToolInteractionFSM>();
        }
        
        protected override void OnAttachedToNpc(NpcElement npcElement) {
            // NPCs cant use sketchbook
        }

        protected override void OnAttachedToHero(Hero hero) {
            offhandPart.SetParent(hero.OffHand);
            offhandPart.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            base.OnAttachedToHero(hero);
        }
        
        protected override void OnAttachedToCustomHeroClothes(CustomHeroClothes renderer, ItemEquip equip) {
            AttachOffHandPartToSocket(renderer.OffHandSocket);
            _inInventory = true;
            base.OnAttachedToCustomHeroClothes(renderer, equip);
        }
        
        protected override void OnDetachedFromCustomHeroClothes(CustomHeroClothes renderer) {
            DiscardOffHandPart();
            _inInventory = false;
            base.OnDetachedFromCustomHeroClothes(renderer);
        }

        void AttachOffHandPartToSocket(Transform socket) {
            if (offhandPart) {
                offhandPart.SetParent(socket, false);
                offhandPart.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        void DiscardOffHandPart() {
            if (!offhandPart.IsChildOf(transform)) {
                Destroy(offhandPart.gameObject);
            }
        }

        void ShowSketchFailPopup() {
            if (_lastFailNotification is { HasBeenDiscarded: false }) {
                _lastFailNotification.Discard();
            }
            _lastFailNotification = new LowerFancyPanelNotification(LocTerms.SketchingFail.Translate(), typeof(VLowerFancyPanelNotification));
            AdvancedNotificationBuffer.Push<LowerMiddleScreenNotificationBuffer>(_lastFailNotification);
        }

        // === Animation Event Callbacks
        public override void OnToolInteractionStart() {
            _sketchbook.HideSketchedDrawing();
            if (_sketchbook.TryDrawSketch(drawingFromSnapShotDelay, drawingTime)) {
                bookAnimator.SetTrigger(SketchHash);
            } else {
                ShowSketchFailPopup();
                _toolInteractionFSM.SetCurrentState(HeroStateType.None);
            }
        }
        
        public override void OnToolInteractionEnd() {
            _sketchbook.HideSketchedDrawing();
        }

        // === Discarding
        protected override IBackgroundTask OnDiscard() {
            if (_offHandWeapon != null) {
                _offHandWeapon.Discard();
                _offHandWeapon = null;
            }
            DiscardOffHandPart();
            _sketchbook = null;
            return base.OnDiscard();
        }
    }
}