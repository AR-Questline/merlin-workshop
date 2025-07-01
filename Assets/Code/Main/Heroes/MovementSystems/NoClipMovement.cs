using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Sources;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    public partial class NoClipMovement : HeroMovementSystem, IUIAware {
        public override ushort TypeForSerialization => SavedModels.NoClipMovement;

        const float DefaultSpeedModifier = 1.8f;
        const float MaxSpeedModifier = 10f;
        const float MaxBoostSpeedModifier = 100f;
        bool _accelerating = false;
        bool _boosting = false;
        
        Vector3 _noClipMoveIntent = Vector3.zero;
        CharacterController CController { get; set; }
        StatTweak _swimSpeedTweak;

        public override MovementType Type => MovementType.NoClip;
        public override bool CanCurrentlyBeOverriden => true;
        public override bool RequirementsFulfilled => CheatController.CheatsEnabled();

        protected override void Init() {
            Controller.gameObject.layer = RenderLayers.PlayerInteractions;
            Log.Debug?.Info("Changed Player layer to PlayerInteractions");
            //Document layer changing in case the layers are ever needed for something else to easily find this change
            
            World.Only<GameUI>().AddElement(new MapHandlerSource(UIContext.Keyboard, this, this, int.MinValue));
            
            _swimSpeedTweak = new StatTweak(Controller.Target.HeroStats.SwimSpeed, DefaultSpeedModifier, TweakPriority.Override, OperationType.Override, this);
            
            CController = Controller.GetComponent<CharacterController>();
        }

        public override void Update(float deltaTime) {
            if (_accelerating || _boosting) {
                Accelerate();
            } else {
                Decelerate();
            }

            PerformSwimMovement();
            

            void Accelerate() {
                if (_swimSpeedTweak.Modifier <= (_boosting ? MaxBoostSpeedModifier : MaxSpeedModifier)) {
                    _swimSpeedTweak.SetModifier(_swimSpeedTweak.Modifier * 1.1f);
                }
                _accelerating = false;
                _boosting = false;
            }
            void Decelerate() {
                if (_swimSpeedTweak.Modifier == DefaultSpeedModifier) return; //Don't change tweak if value is default
                if (_swimSpeedTweak.Modifier * 0.9f <= DefaultSpeedModifier) { //Check if deceleration will go below default
                    _swimSpeedTweak.SetModifier(DefaultSpeedModifier);
                    return;
                }

                //slow down
                _swimSpeedTweak.SetModifier(_swimSpeedTweak.Modifier * 0.9f);
            }

            //Copied from swimming of vHeroController
            void PerformSwimMovement() {
                Vector2 moveVector = World.Any<PlayerInput>().MoveInput;
                float targetSpeed = Controller.Target.HeroStats.MoveSpeed * Controller.Target.HeroStats.SwimSpeed;
                Transform t = Controller.CinemachineHeadTarget.transform;
                
                Vector3 forward = t.forward * (moveVector.y * targetSpeed);
                Vector3 right = t.right * (moveVector.x * targetSpeed);
                
                Vector3 moveTowards = right + forward;
                Vector3 verticalVel = _noClipMoveIntent * targetSpeed;
                _noClipMoveIntent = Vector3.zero;

                ApplyMovement(moveTowards, verticalVel);
            }
            
            void ApplyMovement(Vector3 moveTowards, Vector3 verticalVel) {
                CController.Move(moveTowards * deltaTime + verticalVel * deltaTime);
                Controller.Target.MoveTo(Controller.transform.position);
            }
        }
        public override void FixedUpdate(float deltaTime) { }

        protected override void SetupForceExitConditions() {
            // None
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (Controller == null) {
                // Already discarded or not initialized. this means that the layer is back to default
                return;
            }
            Controller.gameObject.layer = RenderLayers.Player;
            Log.Debug?.Info("Changed Player layer back to Player");
        }

        void PlayerGoUp() => _noClipMoveIntent = Vector3.up;
        void PlayerGoDown() => _noClipMoveIntent = Vector3.down;
        void PlayerAccelerate() => _accelerating = true;
        void PlayerBoost() => _boosting = true;

        public UIResult Handle(UIEvent evt) {
            if (evt is not UIKeyHeldAction keyHeld) return UIResult.Ignore;
            
            if (keyHeld.Name == KeyBindings.Debug.DebugNoClipAccelerate) {
                PlayerAccelerate();
                return UIResult.Accept;
            }
            if (keyHeld.Name == KeyBindings.Debug.DebugNoClipBoost) {
                PlayerBoost();
                return UIResult.Accept;
            }
            
            if (keyHeld.Name == KeyBindings.Debug.DebugNoClipUp) {
                PlayerGoUp();
                return UIResult.Accept;
            }
            if (keyHeld.Name == KeyBindings.Debug.DebugNoClipDown) {
                PlayerGoDown();
                return UIResult.Accept;
            }
            
            return UIResult.Ignore;
        }
    }
}