using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Gliding;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Controls;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.UI.Menu.DeathUI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    public partial class GliderMovement : HeroMovementSystem {
        public sealed override bool IsNotSaved => true;

        Transform _transform;
        HeroCamera _heroCamera;
        HeroGlideAction _gliderAction;

        Vector3 _glidingDirection;
        float _glidingSpeed;
        float _turnSpeed;
        float _pitchSpeed;
        float _remainingPreflightTime;
        float _remainingPostFlightTime;
        Vector3 _initialPreflightVelocity;
        Vector3 _targetPreflightVelocity;
        float _currentRoll;
        float _currentFovMultiplier = 1.0f;
        float _timeSinceCameraInput;
        bool _endingGlide;
        bool _endedGliding;
        bool _endedGlideByHit;
        bool _invertPitch;

        float _cachedFastestFlightSpeed;
        
        public override MovementType Type => MovementType.Glider;
        public override bool CanCurrentlyBeOverriden => true;
        public override bool RequirementsFulfilled => true;
        public bool InPreFlight => _remainingPreflightTime > 0.0f;
        public bool InPostFlight => _remainingPostFlightTime > 0.0f;
        public GliderAttachment Data => _gliderAction.Glider.Attachment;
        public Vector3 GlidingAngles => Quaternion.LookRotation(_glidingDirection).eulerAngles;
        public bool ShouldEnd => _gliderAction is not { HasBeenDiscarded: false };

        protected override void Init() {
            _transform = Controller.transform;
            _heroCamera = ParentModel.VHeroController.HeroCamera;
            _gliderAction = ParentModel.Element<HeroGlideAction>();
            _cachedFastestFlightSpeed = Data.flightSpeedByAngleCurve.Evaluate(Data.lowestFlightAngle);
            
            var invertSetting = World.Any<InvertGliderPitch>();
            invertSetting?.ListenTo(Setting.Events.SettingChanged, OnInvertGliderSettingChanged, this);
            OnInvertGliderSettingChanged(invertSetting);
            
            Hero.Trigger(Hero.Events.HideWeapons, true);
            SetupPreflight();

            Controller.GlidingCrouch(true, Data.preFlightDuration);
            _currentFovMultiplier = GetFovMultiplierFromSpeed(_targetPreflightVelocity.magnitude);
            ParentModel.FoV.UpdateCustomLocomotionFoVMultiplier(_currentFovMultiplier, Data.preFlightDuration);
            Vector3 blurVelocity = _targetPreflightVelocity / _cachedFastestFlightSpeed;
            blurVelocity *= Data.cameraDirectionalBlurMultiplier;
            ParentModel.DirectionalBlur.SetBlurVelocity(blurVelocity, Data.preFlightDuration);
        }

        void OnInvertGliderSettingChanged(Setting setting) {
            if (setting == null) {
                return;
            }
            _invertPitch = ((InvertGliderPitch)setting).Invert;
        }
        
        void SetupPreflight() {
            _remainingPreflightTime = Data.preFlightDuration;
            
            _initialPreflightVelocity = Controller.HorizontalVelocity + Controller.verticalVelocity * Vector3.up;

            var currentVelocity = Controller.HorizontalVelocity * Data.preFlightHorizontalSpeedFactor;
            currentVelocity += Controller.verticalVelocity * Data.preFlightVerticalSpeedFactor * Vector3.up;
            
            var targetDirection = Controller.LookDirection;

            var targetSpeed = Vector3.Dot(targetDirection, currentVelocity);
            if (targetSpeed < 0.001f) targetSpeed = 0.001f;
            
            var targetAngles = Quaternion.LookRotation(targetDirection).eulerAngles;

            if (Mathf.Abs(targetDirection.y) >= 0.999f) {
                targetAngles.y = _transform.eulerAngles.y;
            }
            var angleDelta = Mathf.DeltaAngle(0.0f, targetAngles.x);
            targetAngles.x = Mathf.Clamp(angleDelta, Data.highestFlightAngle, Data.lowestFlightAngle);
            
            _targetPreflightVelocity = Quaternion.Euler(targetAngles) * Vector3.forward * targetSpeed;
        }

        public override void Update(float deltaTime) {
            if (ShouldEnd) {
                EndGlidingImmediate();
                return;
            }
            
            if (InPreFlight) {
                PreFlightUpdate(deltaTime);
            } else if (InPostFlight) {
                PostFlightUpdate(deltaTime);
            } else {
                UpdateHeroFov(deltaTime);
                UpdateHeroBlur();
                UpdateClampCamera();
                RollCamera(deltaTime);
                GliderMovementUpdate(deltaTime);
                CameraLockInUpdate(deltaTime);
            }
        }

        void PreFlightUpdate(float deltaTime) {
            _remainingPreflightTime -= deltaTime;
            if (_remainingPreflightTime < 0.0f) _remainingPreflightTime = 0.0f;
            
            float preflightFactor = 1.0f - _remainingPreflightTime / Data.preFlightDuration;
            var glidingVelocity = Vector3.Lerp(_initialPreflightVelocity, _targetPreflightVelocity, preflightFactor);
            _glidingDirection = glidingVelocity.normalized;
            _glidingSpeed = glidingVelocity.magnitude;
            
            Controller.Controller.Move(glidingVelocity * deltaTime);
            
            PerformMovementChecks(deltaTime);

            PreFlightCameraLockInUpdate();
        }
        
        void PostFlightUpdate(float deltaTime) {
            _remainingPostFlightTime -= deltaTime;
            if (_remainingPostFlightTime < 0.0f) {
                EndGlidingImmediate();
            }

            float postFlightScalar = _remainingPostFlightTime / Data.postFlightDuration;
            
            _currentRoll *= postFlightScalar;
            _heroCamera.SetRoll(_currentRoll);
            
            if (_endedGlideByHit) {
                _glidingSpeed *= postFlightScalar;
            }
            ApplyLinearMovement(deltaTime);
            
            PerformMovementChecks(deltaTime);
        }

        void GliderMovementUpdate(float deltaTime) {
            HandleGlidingSpeed(deltaTime);
            HandleTurn(deltaTime);
            HandlePitch(deltaTime);
            
            ApplyTurningMovement(deltaTime);
            ApplyLinearMovement(deltaTime);
            
            PerformMovementChecks(deltaTime);
        }
        
        void HandleGlidingSpeed(float deltaTime) {
            float pitchAngle = Mathf.DeltaAngle(0.0f, GlidingAngles.x);
            float targetVelocity = Data.flightSpeedByAngleCurve.Evaluate(pitchAngle);
            
            bool shouldDecel = targetVelocity < _glidingSpeed;
            float changeSpeed = shouldDecel ? Data.glidingDeceleration : Data.glidingAcceleration;

            _glidingSpeed = Mathf.MoveTowards(_glidingSpeed, targetVelocity, changeSpeed * deltaTime);
        }

        void HandleTurn(float deltaTime) {
            float input = Controller.Input.MoveInput.x;
            bool changingDirection = input * _turnSpeed <= 0.0f;

            float turnSpeed = Data.turnSpeedByFlightSpeedCurve.Evaluate(_glidingSpeed);
            float desiredSpeed = input * turnSpeed;
            float changeSpeed = Data.turnAcceleration * Mathf.Abs(input);
            if (changingDirection) changeSpeed += Data.turnDeceleration;
            
            _turnSpeed = Mathf.MoveTowards(_turnSpeed, desiredSpeed, changeSpeed * deltaTime);
        }
        
        void HandlePitch(float deltaTime) {
            float input = Controller.Input.MoveInput.y * (_invertPitch ? 1 : -1);
            bool shouldDecelerate = input * _pitchSpeed <= 0.0f;

            float pitchSpeed = input > 0.0f ? Data.pitchUpSpeed : Data.pitchDownSpeed;
            float desiredSpeed = input * pitchSpeed;
            float pitchAcceleration = input < 0.0f ? Data.pitchUpAcceleration : Data.pitchDownAcceleration;
            float changeSpeed = pitchAcceleration * Mathf.Abs(input);
            if (shouldDecelerate) changeSpeed += Data.pitchDeceleration;
            
            _pitchSpeed = Mathf.MoveTowards(_pitchSpeed, desiredSpeed, changeSpeed * deltaTime);
            DampenPitchSpeedToLimits();
        }

        void DampenPitchSpeedToLimits() {
            float lowestPitchDistance = Mathf.DeltaAngle(Data.highestFlightAngle, GlidingAngles.x);
            if (lowestPitchDistance < Data.pitchClampingDistance && _pitchSpeed < 0.0f) {
                _pitchSpeed *= lowestPitchDistance / Data.pitchClampingDistance;
            }
            
            float highestPitchDistance = Mathf.DeltaAngle(GlidingAngles.x, Data.lowestFlightAngle);
            if (highestPitchDistance < Data.pitchClampingDistance && _pitchSpeed > 0.0f) {
                _pitchSpeed *= highestPitchDistance / Data.pitchClampingDistance;
            }
        }

        void ApplyTurningMovement(float deltaTime) {
            float turnAngleDelta = _turnSpeed * deltaTime;
            float pitchAngleDelta = _pitchSpeed * deltaTime;
            
            Quaternion turnRotation = Quaternion.AngleAxis(turnAngleDelta, Vector3.up);
            Vector3 sideVector = Vector3.Cross(Vector3.up, _glidingDirection);
            Quaternion pitchRotation = Quaternion.AngleAxis(pitchAngleDelta, sideVector);
            _glidingDirection = turnRotation * pitchRotation * _glidingDirection;

            var currentLookRotation = _heroCamera.GetAngles();
            var newLookRotation = currentLookRotation + new Vector3(pitchAngleDelta, turnAngleDelta, 0.0f);
            
            _heroCamera.SetAngles(newLookRotation);
        }
        
        void ApplyLinearMovement(float deltaTime) {
            Vector3 downwardVelocity = Vector3.down * Data.constantDownwardsVelocity;

            Vector3 velocity = _glidingDirection * _glidingSpeed + downwardVelocity;
            
            Controller.PerformMoveStep(velocity * deltaTime);
            Controller.SetVerticalVelocity(velocity.y);
        }
        
        void PerformMovementChecks(float deltaTime) {
            Controller.PerformGroundChecks(deltaTime);
            Controller.PerformWaterCheck(deltaTime);
            
            if (Controller.Grounded || Controller.IsSwimming) {
                EndGliding();
                _endedGlideByHit = true;
            }
        }

        void PreFlightCameraLockInUpdate() {
            Vector3 flyDirectionEuler = Quaternion.LookRotation(_targetPreflightVelocity.normalized).eulerAngles;
            float cameraLockInFactor = Data.preFlightDuration - _remainingPreflightTime;
            _heroCamera.FollowRotation(flyDirectionEuler, cameraLockInFactor);
        }
        
        void CameraLockInUpdate(float deltaTime) {
            _timeSinceCameraInput += deltaTime;
            float movement = Controller.Input.MoveInput.magnitude;
            if (movement > 0.0f && _timeSinceCameraInput > Data.cameraLockInManualDelay) {
                _timeSinceCameraInput += movement * Data.cameraLockInManualSpeed / Data.cameraLockInAutomaticSpeed;
            }

            if (Controller.Input.LookInput != Vector2.zero) {
                _timeSinceCameraInput = 0.0f;
            }

            float timeSinceLockInBegun = _timeSinceCameraInput - Data.cameraLockInAutomaticDelay;
            float cameraLockInFactor = Mathf.Min(timeSinceLockInBegun * Data.cameraLockInAutomaticSpeed,
                Data.cameraLockInMaxForce);

            Vector3 flyDirectionEuler = Quaternion.LookRotation(_glidingDirection).eulerAngles;
            _heroCamera.FollowRotation(flyDirectionEuler, deltaTime, cameraLockInFactor);
        }

        void UpdateClampCamera() {
            _heroCamera.SetSmoothClampingData(new HeroCamera.SmoothClampingData {
                targetDirection = _glidingDirection,
                pitchRange = Data.cameraClampPitch,
                yawRange = Data.cameraClampYaw,
                smoothingRange = Data.cameraClampSmoothingRange
            });
        }

        void RollCamera(float deltaTime) {
            var targetRoll = _turnSpeed * -Data.cameraRollMagnitude;
            _currentRoll = Mathf.Lerp(_currentRoll, targetRoll, Data.cameraRollForce * deltaTime);
            _heroCamera.SetRoll(_currentRoll);
        }

        void UpdateHeroFov(float deltaTime) {
            float fovMultiplier = GetFovMultiplierFromSpeed(_glidingSpeed);

            float changeSpeed = Data.cameraFovMultiplierChangeSpeed * deltaTime;
            _currentFovMultiplier = Mathf.MoveTowards(_currentFovMultiplier, fovMultiplier, changeSpeed);
            
            ParentModel.FoV.UpdateCustomLocomotionFoVMultiplier(_currentFovMultiplier, 0.0f);
        }

        float GetFovMultiplierFromSpeed(float speed) {
            float forwardVelocity = Mathf.Abs(speed) / _cachedFastestFlightSpeed;
            float fovMultiplier = Mathf.LerpUnclamped(1.0f, Data.maxCameraFovMultiplier, forwardVelocity);

            return fovMultiplier;
        }

        void UpdateHeroBlur() {
            float forwardVelocity = Mathf.Abs(_glidingSpeed) / _cachedFastestFlightSpeed;
            float blurIntensity = Data.cameraDirectionalBlurMultiplier * forwardVelocity;
            ParentModel.DirectionalBlur.SetBlurVelocity(_glidingDirection * blurIntensity, 0.0f);
        }

        public override void FixedUpdate(float deltaTime) { }

        public override void OnControllerColliderHit(ControllerColliderHit hit) {
            var hitForce = Vector3.Dot(-hit.normal, _glidingDirection * _glidingSpeed);

            if (hitForce > Data.minWallHitForceToEndGliding) {
                EndGliding();
                _endedGlideByHit = true;
            }
        }

        public void EndGliding() {
            if (!_endingGlide && !_endedGliding) {
                _endingGlide = true;

                Controller.GlidingCrouch(false, Data.postFlightDuration);
                ParentModel.FoV.UpdateCustomLocomotionFoVMultiplier(1.0f, Data.postFlightDuration);
                ParentModel.DirectionalBlur.SetBlurVelocity(Vector3.zero, Data.postFlightDuration);
                _remainingPostFlightTime = Data.postFlightDuration;
            }
        }

        void EndGlidingImmediate(bool returnToDefaultMovement = true) {
            if (!_endedGliding) {
                Controller.GlidingCrouch(false, 0.0f);
                ParentModel.FoV.UpdateCustomLocomotionFoVMultiplier(1.0f);
                ParentModel.DirectionalBlur.SetBlurVelocity(Vector3.zero, 0.0f);
                _heroCamera.SetRoll(0.0f);
                _heroCamera.ResetSmoothClampingData();
                _endedGliding = true;
                if (returnToDefaultMovement) {
                    ParentModel.ReturnToDefaultMovement();
                }
            }
        }

        protected override void SetupForceExitConditions() {
            World.EventSystem.ListenTo(EventSelector.AnySource, SCloseHeroEyes.Events.EyesClosed, this,
                h => EndGlidingImmediate());
            World.EventSystem.ListenTo(EventSelector.AnySource, VJailUI.Events.GoingToJail, this,
                h => EndGlidingImmediate());
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<GravityMarker>(), this, h => {
                if (h is Element { GenericParentModel: Heroes.Hero }) EndGlidingImmediate();
            });
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (!Hero.HasBeenDiscarded) {
                EndGlidingImmediate(false);
            }
            base.OnDiscard(fromDomainDrop);
        }

    }
}