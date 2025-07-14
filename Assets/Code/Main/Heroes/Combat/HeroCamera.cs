using System;
using System.Collections.Generic;
using Awaken.Kandra;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Locations.Shops.UI;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Settings.Controls;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;

namespace Awaken.TG.Main.Heroes.Combat {
    public class HeroCamera {
        const float HeroKnockdownCameraRotationSpeed = 8;
        const float MouseSensitivity = 0.1f;
        const float GamepadSensitivity = 0.09f;
        const float DelayDialogueCameraDeadzone = 3f;

        readonly VHeroController _heroController;
        readonly Hero _hero;
        readonly int _dialogueCameraMaxAngle;
        readonly float _dialogueLookAreaSizePercent;
        readonly float _dialogueCameraResetDelay;
        readonly float _dialogueCameraResetSpeed;
        readonly float _dialogueCameraLookSpeedMultiplier;
        readonly float _gamepadWorldCameraLookSpeedMultiplier;
        readonly AnimationCurve _gamepadAimTiltMultiplier;
        readonly Transform _tppTargetFollower;
        readonly Transform _assistTarget;
        readonly List<KandraRenderer> _hiddenKandraRenderers = new();
        
        CinemachineComposer _cinemachineComposer;
        Cinemachine3rdPersonFollow _thirdPersonFollow;
        Transform _cameraLookAt;
        Transform _dialogueCameraLookTarget;
        float _finisherActivityTime;
        float _dialogueCameraLookTargetMaxDistance;
        float _dialogueCameraDeadzoneX;
        float _dialogueCameraDeadzoneY;
        bool _dialogueCameraMoved;
        bool? _targetUnlockedCameraActivityCache;
        float _dialogueCameraResetTimer;
        float _cinemachineTargetRoll;
        float _rotationVelocity;
        float _sensitivity;
        float _lookDeadZone;
        float _dialogueCameraActiveDuration;
        bool _dialogueFppPivotRotationActive;
        SmoothClampingData _smoothClampingData;
        Transform _lockedTarget;
        IEventListener _targetDeathListener;
        IEventListener _targetLeftCombatListener;
        IEventListener _targetDiscardedListener;
        AimAssistSetting _aimAssistSetting;
        AimAssistData _lowAssistData;
        AimAssistData _highAssistData;

        public float CinemachineTargetPitch { get; private set; }
        [UnityEngine.Scripting.Preserve] float DialogueLookAreaSize => Screen.height * _dialogueLookAreaSizePercent;
        HeroControllerData Data => _heroController.Data;
        Transform HeroTransform => _heroController.transform;
        Vector3 HeroForward => _hero.Rotation * Vector3.forward;
        Vector2 ForcedInputFromCode => _heroController.ForcedInputFromCode;
        CinemachineVirtualCamera BaseVirtualCamera => Hero.TppActive ? _heroController.tppVirtualCamera : _heroController.baseVirtualCamera;
        CinemachineVirtualCamera DialogueVirtualCamera => _heroController.dialogueVirtualCamera;
        CinemachineVirtualCamera FinisherVirtualCamera => _heroController.finisherVirtualCamera;
        Transform FppArmsPivot => _heroController.fppParent.transform;
        Transform TppPivot => _heroController.tppPivot.transform;
        Camera MainCamera => _heroController.MainCamera;
        CinemachineComposer CinemachineComposer => _cinemachineComposer ??= DialogueVirtualCamera.GetCinemachineComponent<CinemachineComposer>();
        Cinemachine3rdPersonFollow ThirdPersonFollow => _thirdPersonFollow ??= _heroController.tppVirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        float GetAssistRange => _hero.MainHandWeapon is CharacterBow or CharacterMagic || _hero.OffHandWeapon is CharacterMagic ? AimAssistData.assistLongRange : AimAssistData.assistShortRange;
        AimAssistData AimAssistData => HighAssistEnabled ? _highAssistData : _lowAssistData;
        bool AimAssistEnabled => _aimAssistSetting?.Enabled ?? false;
        bool HighAssistEnabled => _aimAssistSetting?.HighAssistEnabled ?? false;
        Transform TargetFollower => Hero.TppActive ? _tppTargetFollower : _heroController.AimAssistTargetFollower;
        bool CanRotateHero => !Hero.TppActive || !Hero.Current.Mounted;
        
        public HeroCamera(VHeroController heroController) {
            _heroController = heroController;
            _tppTargetFollower = heroController.tppAimAssistTargetFollower;
            _assistTarget = heroController.aimAssistTarget;
            _hero = _heroController.Target;

            _aimAssistSetting = World.Any<AimAssistSetting>();
            GameConstants gameConstants = World.Services.Get<GameConstants>();
            
            _lowAssistData = gameConstants.lowAssistData;
            _highAssistData = gameConstants.highAssistData;
            _dialogueCameraMaxAngle = gameConstants.dialogueCameraMaxAngle;
            _dialogueLookAreaSizePercent = gameConstants.dialogueLookAreaSize / 100f;
            _dialogueCameraResetDelay = gameConstants.dialogueCameraResetDelay;
            _dialogueCameraResetSpeed = gameConstants.dialogueCameraResetSpeed;
            _dialogueCameraLookSpeedMultiplier = gameConstants.dialogueCameraLookSpeedMultiplier;
            _gamepadAimTiltMultiplier = gameConstants.gamepadAimTiltMultiplier;
            _dialogueCameraDeadzoneX = CinemachineComposer.m_DeadZoneWidth;
            _dialogueCameraDeadzoneY = CinemachineComposer.m_DeadZoneHeight;
            CinemachineComposer.m_SoftZoneWidth = 999;
            CinemachineComposer.m_SoftZoneHeight = 999;
            _gamepadWorldCameraLookSpeedMultiplier = gameConstants.gamepadWorldCameraLookSpeedMultiplier;

            var cameraSensitivity = World.Only<CameraSensitivity>();
            var aimDeadzone = World.Only<AimDeadzone>();
            UpdateSensitivity(cameraSensitivity);
            UpdateDeadZone(aimDeadzone);
            
            var tppCameraDistance = World.Any<TppCameraDistanceSetting>();
            if (tppCameraDistance != null) {
                UpdateTppCameraZoom(tppCameraDistance);
                tppCameraDistance.ListenTo(Setting.Events.SettingRefresh, UpdateTppCameraZoom, _heroController);
            }

            heroController.Target.ListenTo(GroundedEvents.AfterTeleported, AfterTeleported, _heroController);
            cameraSensitivity.ListenTo(Setting.Events.SettingRefresh, UpdateSensitivity, _heroController);
            aimDeadzone.ListenTo(Setting.Events.SettingRefresh, UpdateDeadZone, _heroController);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<IHeroInvolvement>(), _heroController, m => LookAt((IHeroInvolvement)m, _heroController));
            World.EventSystem.ListenTo(EventSelector.AnySource, HeroCameraDampingOverride.Events.DampingChanged, _heroController, UpdateDamping);
            World.Any<AimAssistSetting>()?.ListenTo(Setting.Events.SettingRefresh, DisableLockToTarget, _heroController);
            SetDefaultLookAt();
        }

        void AfterTeleported() {
            MainCamera.transform.forward = HeroTransform.forward;
        }

        void UpdateSensitivity(Setting setting) {
            if (setting is CameraSensitivity cameraSensitivity) {
                _sensitivity = cameraSensitivity.Sensitivity;
            }
        }

        void UpdateDeadZone(Setting setting) {
            if (setting is AimDeadzone aimDeadzone) {
                _lookDeadZone = aimDeadzone.Value;
            }
        }

        void UpdateTppCameraZoom(Setting setting) {
            if (setting is TppCameraDistanceSetting tppCameraDistance) {
                ThirdPersonFollow.CameraDistance = tppCameraDistance.TppCameraDistance;
            }
        }

        void LookAt(IHeroInvolvement heroInvolvement, IListenerOwner eventListenerOwner) {
            if (World.HasAny<CharacterSheetUI>()) {
                return;
            }
            
            if (DialogueVirtualCamera != null) {
                RefreshLookAt(heroInvolvement);
                SetActiveDialogueCamera(true);
                heroInvolvement.ListenTo(Model.Events.AfterChanged, () => RefreshLookAt(heroInvolvement), eventListenerOwner); 
                heroInvolvement.ListenTo(Model.Events.AfterDiscarded, StopLookingInDirection, eventListenerOwner);

                if (World.HasAny<StoryInteractionFocusOverride>()) {
                    var focusOverride = World.Any<StoryInteractionFocusOverride>();
                    focusOverride.ListenTo(Model.Events.AfterChanged, () => RefreshLookAt(heroInvolvement), heroInvolvement);
                    focusOverride.ListenTo(Model.Events.AfterDiscarded, () => RefreshLookAt(heroInvolvement), heroInvolvement);
                }
                
                World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<StoryInteractionFocusOverride>(), heroInvolvement, m => {
                    RefreshLookAt(heroInvolvement);
                    m.ListenTo(Model.Events.AfterChanged, () => RefreshLookAt(heroInvolvement), heroInvolvement);
                    m.ListenTo(Model.Events.AfterDiscarded, () => RefreshLookAt(heroInvolvement), heroInvolvement);
                });
            }
        }

        void RefreshLookAt(IHeroInvolvement heroInvolvement) {
            if (DialogueVirtualCamera != null) {
                if (!heroInvolvement.TryGetFocus(out var newLookAt)) {
                    SetDefaultLookAt();
                    return;
                }
                if (NpcPresence.InAbyss(newLookAt.position)) {
                    Log.Important?.Error("Camera trying to look at NPC in abyss!", newLookAt);
                    SetDefaultLookAt();
                    return;
                }
                _cameraLookAt = newLookAt;
                
                PositionConstraint positionConstraint = GetOrCreatePositionConstraint();
                positionConstraint.constraintActive = true;
                positionConstraint.SetSource(0, new ConstraintSource { sourceTransform = _cameraLookAt, weight = 1});
                
                _dialogueCameraLookTarget.position = _cameraLookAt.position;
                _dialogueCameraLookTarget.SetParent(heroInvolvement.FocusParent);
                _dialogueCameraLookTarget.gameObject.SetActive(true);
                DialogueVirtualCamera.LookAt = _dialogueCameraLookTarget;
                
                _dialogueCameraLookTargetMaxDistance = Mathf.Tan(_dialogueCameraMaxAngle * Mathf.Deg2Rad) * _hero.Coords.DistanceTo(_cameraLookAt.position);
            }
        }

        void SetDefaultLookAt() {
            _cameraLookAt = null;
            
            var transform = BaseVirtualCamera.transform;
            var targetPos = transform.position + transform.forward * 5f;
            
            PositionConstraint positionConstraint = GetOrCreatePositionConstraint();
            positionConstraint.constraintActive = false;
            
            _dialogueCameraLookTarget.position = targetPos;
            _dialogueCameraLookTarget.SetParent(transform.parent);
            _dialogueCameraLookTarget.gameObject.SetActive(true);
            DialogueVirtualCamera.LookAt = _dialogueCameraLookTarget;
        }
        
        PositionConstraint GetOrCreatePositionConstraint() {
            PositionConstraint positionConstraint;
            if (_dialogueCameraLookTarget == null) {
                _dialogueCameraLookTarget = new GameObject(nameof(_cameraLookAt)).transform;
                positionConstraint = _dialogueCameraLookTarget.gameObject.AddComponent<PositionConstraint>();
                positionConstraint.constraintActive = true;
                positionConstraint.AddSource(new ConstraintSource());
            } else {
                positionConstraint = _dialogueCameraLookTarget.GetComponent<PositionConstraint>();
            }
            return positionConstraint;
        }

        void StopLookingInDirection() {
            //Multiple HeroInvolvements can be present at the same time (e.g. one story starts another story)
            if (DialogueVirtualCamera != null && World.Any<IHeroInvolvement>() == null) {
                SetActiveDialogueCamera(false);
                SetDefaultLookAt();
                ResetCameraPitch();
                if (CanRotateHero) {
                    HeroTransform.forward = MainCamera.transform.forward.ToHorizontal3();
                }
                CameraRotation();
            }
        }

        public void CameraRotation(float deltaTime = 0) {
            if (!Hero.Current.IsAlive) return;
            if (FinisherVirtualCamera.enabled) {
                FinisherCameraRotation();
            } else if (DialogueVirtualCamera.enabled && !World.HasAny<ShopUI>()) {
                DialogueCameraRotation(deltaTime);
            } else if (Hero.Current.TryGetElement(out HeroKnockdown heroKnockdown)) {
                HeroKnockdownCameraRotation(heroKnockdown);
            } else {
                WorldCameraRotation(deltaTime);
            }
        }
        
        public void ResetCameraPitch() {
            SetPitch(MainCamera.transform.localEulerAngles.x);
        }
        
        public void SetPitch(float pitch) {
            CinemachineTargetPitch = pitch switch {
                > 180 => pitch - 360,
                < -180 => pitch + 360,
                _ => pitch,
            };
            
            if (Hero.TppActive) {
                TppPivot.localRotation = Quaternion.Euler(CinemachineTargetPitch, 0.0f, 0.0f);
            }
        }

        public void SetActiveFppArmsRotation(bool enable) {
            _dialogueFppPivotRotationActive = enable;
        }

        public void SetRoll(float roll) {
            _cinemachineTargetRoll = roll;
        }
        
        public void ResetSmoothClampingData() {
            SetSmoothClampingData(new SmoothClampingData {
                targetDirection = Vector3.zero
            });
        }
        
        public void SetSmoothClampingData(SmoothClampingData data) {
            _smoothClampingData = data;
        }

        void FinisherCameraRotation() {
            const float LerpTime = 0.25f;
            if (MainCamera == null || FppArmsPivot == null) {
                return;
            }
            _finisherActivityTime += Time.unscaledDeltaTime;
            if (_finisherActivityTime < LerpTime) {
                FppArmsPivot.localRotation = Quaternion.Lerp(FppArmsPivot.localRotation, Quaternion.identity, _finisherActivityTime / LerpTime);
            } else {
                FppArmsPivot.localRotation = Quaternion.identity;
            }
        }

        void DialogueCameraRotation(float deltaTime) {
            if (_cinemachineComposer == null || _cameraLookAt == null || MainCamera == null || HeroTransform == null) {
                return;
            }

            _dialogueCameraActiveDuration += deltaTime;
            Vector3 mainCameraForward = MainCamera.transform.forward;
            if (CanRotateHero) {
                HeroTransform.forward = mainCameraForward.ToHorizontal3();
            }
            if (_dialogueFppPivotRotationActive) {
                FppArmsPivot.forward = mainCameraForward;
            }
            float rotationDirectionX;
            float rotationDirectionY;
            bool goBackX = false;
            bool goBackY = false;
            if (RewiredHelper.IsGamepad) {
                HandleGamepadInput(out rotationDirectionX, ref goBackX, out rotationDirectionY, ref goBackY);
            } else {
                // Disabled mouse rotation in dialogues because if didn't work properly
                rotationDirectionX = rotationDirectionY = 0;
                goBackX = goBackY = true;
                //HandleScreenBordersInputAlternative(out rotationDirectionX, ref goBackX, out rotationDirectionY, ref goBackY);
            }

            if (_dialogueCameraMoved && goBackX && goBackY) {
                DialogueCameraReset();
                return;
            }

            _dialogueCameraResetTimer = 0;
            // Deadzone
            float deadzoneInterpolation = math.max(0, _dialogueCameraActiveDuration - DelayDialogueCameraDeadzone);
            if (deadzoneInterpolation < 1) {
                CinemachineComposer.m_DeadZoneHeight = math.lerp(0, _dialogueCameraDeadzoneY, deadzoneInterpolation);
                CinemachineComposer.m_DeadZoneWidth = math.lerp(0, _dialogueCameraDeadzoneX, deadzoneInterpolation);
            } else {
                CinemachineComposer.m_DeadZoneHeight = _dialogueCameraDeadzoneY;
                CinemachineComposer.m_DeadZoneWidth = _dialogueCameraDeadzoneX;
            }

            if (!_dialogueCameraMoved) {
                return;
            }

            float rotSpeed = RewiredHelper.IsGamepad ? Data.padRotationSpeed : Data.rotationSpeed;
            Vector3 cache = _cameraLookAt.forward;
            _cameraLookAt.forward = _hero.Coords - _cameraLookAt.position;
            Matrix4x4 temp = _cameraLookAt.worldToLocalMatrix;
            Matrix4x4 temp2 = _cameraLookAt.localToWorldMatrix;
            _cameraLookAt.forward = cache;
            var pos = temp.MultiplyPoint(_dialogueCameraLookTarget.position);
            pos.x = Mathf.Clamp(pos.x + -rotationDirectionX * rotSpeed * _dialogueCameraLookSpeedMultiplier, -_dialogueCameraLookTargetMaxDistance, _dialogueCameraLookTargetMaxDistance);
            pos = temp2.MultiplyPoint(pos);
            pos.y = Mathf.Clamp(pos.y + rotationDirectionY * rotSpeed * _dialogueCameraLookSpeedMultiplier, _cameraLookAt.position.y - _dialogueCameraLookTargetMaxDistance, _cameraLookAt.position.y + _dialogueCameraLookTargetMaxDistance);
            _dialogueCameraLookTarget.position = pos;
        }

        void HandleGamepadInput(out float rotationDirectionX, ref bool goBackX, out float rotationDirectionY, ref bool goBackY) {
            float axisX = 0;//RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.CameraHorizontal);
            float axisY = 0;//RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.CameraVertical);
            if (Mathf.Abs(axisX) > _lookDeadZone) {
                rotationDirectionX = axisX;
                _dialogueCameraMoved = true;
            } else {
                rotationDirectionX = 0;
                goBackX = true;
            }

            if (Mathf.Abs(axisY) > _lookDeadZone) {
                rotationDirectionY = -axisY;
                _dialogueCameraMoved = true;
            } else {
                rotationDirectionY = 0;
                goBackY = true;
            }
        }

        /*void HandleScreenBordersInputAlternative(out float rotationDirectionX, ref bool goBackX, out float rotationDirectionY, ref bool goBackY) {
            rotationDirectionX = rotationDirectionY = 0;
            Vector2 mousePosition = UnityEngine.Input.mousePosition;
            if (mousePosition.x.OutsideOfRange(0, Screen.width) || mousePosition.y.OutsideOfRange(0, Screen.height)) {
                return;
            }

            if (mousePosition.x > Screen.width - DialogueLookAreaSize) {
                rotationDirectionX = _dialogueCameraSpeedCurve.Evaluate(1 - (Screen.width - mousePosition.x) / DialogueLookAreaSize);
                _dialogueCameraMoved = true;
            } else if (mousePosition.x < DialogueLookAreaSize) {
                rotationDirectionX = -_dialogueCameraSpeedCurve.Evaluate(1 - mousePosition.x / DialogueLookAreaSize);
                _dialogueCameraMoved = true;
            } else {
                goBackX = true;
            }

            if (mousePosition.y > Screen.height - DialogueLookAreaSize) {
                rotationDirectionY = _dialogueCameraSpeedCurve.Evaluate(1 - (Screen.height - mousePosition.y) / DialogueLookAreaSize);
                _dialogueCameraMoved = true;
            } else if (mousePosition.y < DialogueLookAreaSize) {
                rotationDirectionY = -_dialogueCameraSpeedCurve.Evaluate(1 - mousePosition.y / DialogueLookAreaSize);
                _dialogueCameraMoved = true;
            } else {
                goBackY = true;
            }
        }*/

        void DialogueCameraReset() {
            _dialogueCameraResetTimer += Time.deltaTime;
            if (_dialogueCameraResetTimer > _dialogueCameraResetDelay) {
                _dialogueCameraLookTarget.position = Vector3.MoveTowards(_dialogueCameraLookTarget.position, _cameraLookAt.position,
                    _dialogueCameraResetSpeed * Time.deltaTime);
                CinemachineComposer.m_DeadZoneHeight = CinemachineComposer.m_DeadZoneWidth = 0;
                _dialogueCameraMoved = !_dialogueCameraLookTarget.position.EqualsApproximately(_cameraLookAt.position, 0.0001f);
            }
        }

        public void FollowRotation(Vector3 eulerAngles, float deltaTime, float force = 1f) {
            float d = deltaTime * force;
            Quaternion targetHeroRotation = Quaternion.Euler(0.0f, eulerAngles.y, 0.0f);
            HeroTransform.rotation = Quaternion.Slerp(HeroTransform.rotation, targetHeroRotation, d);
            SetPitch(Mathf.LerpAngle(CinemachineTargetPitch, eulerAngles.x, d));
        }

        public void SetAngles(Vector3 eulerAngles) {
            HeroTransform.rotation = Quaternion.Euler(0.0f, eulerAngles.y, 0.0f);
            SetPitch(eulerAngles.x);
        }

        public Vector3 GetAngles() {
            return new Vector3(CinemachineTargetPitch, HeroTransform.eulerAngles.y, 0.0f);
        }
        
        void HeroKnockdownCameraRotation(HeroKnockdown heroKnockdown) {
            if (heroKnockdown.LookAt == null) {
                return;
            }

            var lookRotation =  Quaternion.LookRotation(heroKnockdown.LookAt.Coords - _hero.Coords);
            lookRotation.x = 0;
            lookRotation.z = 0;
            HeroTransform.rotation = Quaternion.Slerp(HeroTransform.rotation, lookRotation,
                Hero.Current.GetDeltaTime() * HeroKnockdownCameraRotationSpeed);
        }
        
        void LockToTarget(NpcElement npc, Transform target) {
            _lockedTarget = _aimAssistSetting.HighAssistEnabled ? npc.Torso : target;
            _targetDeathListener = npc.ListenTo(IAlive.Events.BeforeDeath, DisableLockToTarget, npc);
            _targetLeftCombatListener = npc.ListenTo(ICharacter.Events.CombatExited, DisableLockToTarget, npc);
            _targetDiscardedListener = npc.ListenTo(Model.Events.BeforeDiscarded, DisableLockToTarget, npc);
        }

        void DisableLockToTarget() {
            _lockedTarget = null;
            World.EventSystem.TryDisposeListener(ref _targetDeathListener);
            World.EventSystem.TryDisposeListener(ref _targetLeftCombatListener);
            World.EventSystem.TryDisposeListener(ref _targetDiscardedListener);
            
            _assistTarget.SetParent(HeroTransform);
            TargetFollower.localRotation = Quaternion.identity;
        }

        void TrackTarget() {
            Vector3 targetFollowerPosition = TargetFollower.position;
            Vector3 targetDirection = _lockedTarget.position - targetFollowerPosition;
            var angle = Vector3.Angle(TargetFollower.forward, targetDirection);
            if (angle * Mathf.Deg2Rad > AimAssistData.angleToStopTrackingRadians) {
                DisableLockToTarget();
                return;
            }
            
            float assistSpeed = Mathf.Lerp(AimAssistData.maxAssistSpeed, AimAssistData.minAssistSpeed, 
                Vector3.Distance(_lockedTarget.position, targetFollowerPosition) / AimAssistData.distanceUntilSpeedDrops);
           
            
            if (angle < AimAssistData.maxAngleThatSlowsAssist) {
                assistSpeed *= Mathf.Lerp(AimAssistData.narrowAngleMinMultiplier, 1f, angle / AimAssistData.maxAngleThatSlowsAssist);
            }

            float heroTimeScale = Hero.Current.GetDeltaTime();
            Vector3 newDirection = Vector3.RotateTowards(TargetFollower.forward, targetDirection, assistSpeed * heroTimeScale, 0.0f);
            TargetFollower.rotation = Quaternion.LookRotation(newDirection);
            Vector3 targetFollowerRotationEuler = TargetFollower.rotation.eulerAngles;
            Quaternion targetHeroRotation = Quaternion.Euler(targetFollowerRotationEuler.x, 0.0f, 0.0f);
            FppArmsPivot.localRotation = Quaternion.Slerp(FppArmsPivot.localRotation, targetHeroRotation, heroTimeScale);
            FollowRotation(targetFollowerRotationEuler, heroTimeScale, assistSpeed);
        }

        void WorldCameraRotation(float deltaTime) {
            var isGamepad = RewiredHelper.IsGamepad;
            var input = World.Any<PlayerInput>();
            Vector2 lookInput = isGamepad ? input.LookInput * (deltaTime * _gamepadWorldCameraLookSpeedMultiplier) : input.LookInput;
            bool hasPlayerInput = lookInput != Vector2.zero;
            
            if (isGamepad && hasPlayerInput) {
                lookInput.x *= _gamepadAimTiltMultiplier.Evaluate(Math.Abs(lookInput.x));
                lookInput.y *= _gamepadAimTiltMultiplier.Evaluate(Math.Abs(lookInput.y));
            }

            if (hasPlayerInput) {
                lookInput *= _sensitivity;
                lookInput *= Hero.Current.HeroStats.AimSensitivityMultiplier;

                if (isGamepad) {
                    lookInput += ForcedInputFromCode * GamepadSensitivity;
                    lookInput *= Data.padRotationSpeed;
                } else {
                    lookInput += ForcedInputFromCode;
                    lookInput *= Data.rotationSpeed * MouseSensitivity;
                }
                
                lookInput = ApplySmoothClampingToInput(lookInput);
            }
            
            if (isGamepad && AimAssistEnabled && _hero.IsWeaponEquipped && !hasPlayerInput && _lockedTarget == null) {
                if (Physics.Raycast(TargetFollower.position, TargetFollower.forward, out RaycastHit hit, GetAssistRange, RenderLayers.Mask.AIs + RenderLayers.Mask.CharacterGround) && 
                    VGUtils.GetModel<IAlive>(hit.collider.gameObject) is NpcElement npc && npc.AntagonismTo(_hero) == Antagonism.Hostile) {
                    if (npc.ParentTransform != null) {
                        lookInput *= AimAssistData.softAssistOnEnemyMultiplier;
                        _assistTarget.SetParent(npc.ParentTransform);
                        _assistTarget.position = hit.point;
                        LockToTarget(npc, _assistTarget);
                    }
                }
            }

            CinemachineTargetPitch += lookInput.y;
            _rotationVelocity = lookInput.x;

            // clamp our pitch rotation
            CinemachineTargetPitch = GeneralUtils.ClampAngle(CinemachineTargetPitch, Data.bottomClamp, Hero.Current.Mounted ? Data.topClampOnHorse : Data.topClamp);
            
            // Update Cinemachine camera target pitch and roll
            FppArmsPivot.localRotation = Hero.TppActive ? Quaternion.identity : Quaternion.Euler(CinemachineTargetPitch, 0.0f, _cinemachineTargetRoll);
            if (Hero.TppActive) {
                float y = 0.0f;
                if (Hero.Current.Mounted) {
                    y = TppPivot.localEulerAngles.y + _rotationVelocity;
                } 
                TppPivot.localRotation = Quaternion.Euler(CinemachineTargetPitch, y, 0.0f);
            } else {
                TppPivot.localRotation = Quaternion.identity;
            }

            // rotate the player left and right
            if (CanRotateHero) {
                HeroTransform.Rotate(Vector3.up * _rotationVelocity);
            }

            if (isGamepad && _lockedTarget != null) {
                if (!hasPlayerInput) {
                    TrackTarget();
                } else {
                    DisableLockToTarget();
                }
            }
        }

        Vector2 ApplySmoothClampingToInput(Vector2 lookInput) {
            Vector3 targetDirection = _smoothClampingData.targetDirection;
            
            if (targetDirection == Vector3.zero) {
                return lookInput;
            }

            Vector3 targetEulerAngles = Quaternion.LookRotation(targetDirection).eulerAngles;

            float currentPitch = CinemachineTargetPitch;
            float currentYaw = HeroTransform.eulerAngles.y;

            float pitchRange = _smoothClampingData.pitchRange;
            float yawRange = _smoothClampingData.yawRange;
            float smoothingRange = _smoothClampingData.smoothingRange;
            
            return new Vector2 {
                x = SmoothClampInput(lookInput.x, currentYaw, targetEulerAngles.y, yawRange, smoothingRange),
                y = SmoothClampInput(lookInput.y, currentPitch, targetEulerAngles.x, pitchRange, smoothingRange),
            };
        }

        float SmoothClampInput(float input, float current, float target, float range, float smoothingRange) {
            float delta = Mathf.DeltaAngle(target, current);
            float edgeDelta = range - Mathf.Abs(delta);
            bool movingTowardsEdge = delta * input > 0.0f;
            if (movingTowardsEdge && smoothingRange > 0.0f) {
                input *= Mathf.Clamp01(edgeDelta / smoothingRange);
            } 
            
            float edgeDeltaAfterInput = range - Mathf.Abs(delta + input);
            bool hasPassedTheEdge = edgeDeltaAfterInput <= 0.0f;
            if (hasPassedTheEdge){
                return edgeDelta * Mathf.Sign(delta);
            }
            
            return input;
        }

        public void ChangeHeroPerspective(bool tppActive) {
            if (DialogueVirtualCamera.enabled) {
                return;
            }

            _heroController.tppVirtualCamera.enabled = tppActive;
            _heroController.baseVirtualCamera.enabled = !tppActive;
            World.Only<GameCamera>().SetCinemachineCamera(BaseVirtualCamera);
        }
        
        public void LockActiveDialogueCamera(bool enabled) {
            _targetUnlockedCameraActivityCache ??= DialogueVirtualCamera.enabled;
            DialogueVirtualCamera.enabled = enabled;
            World.Only<GameCamera>().SetCinemachineCamera(enabled ? DialogueVirtualCamera : BaseVirtualCamera);
        }
        
        public void UnlockActiveDialogueCamera() {
            if (_targetUnlockedCameraActivityCache.HasValue) {
                bool cache = _targetUnlockedCameraActivityCache.Value;
                _targetUnlockedCameraActivityCache = null;
                SetActiveDialogueCamera(cache);
            }
        }

        public void InstantSnapDialogueCamera() {
            if (_cameraLookAt != null) {
                DialogueVirtualCamera.CancelDamping();
            }
        }

        public void ActivateFinisherCamera() {
            _finisherActivityTime = 0f;
            FinisherVirtualCamera.m_Lens = BaseVirtualCamera.m_Lens;
            FinisherVirtualCamera.enabled = true;
            World.Only<GameCamera>().SetCinemachineCamera(FinisherVirtualCamera);
        }
        
        public void DeactivateFinisherCamera() {
            FinisherVirtualCamera.enabled = false;
            World.Only<GameCamera>().SetCinemachineCamera(DialogueVirtualCamera.enabled ? DialogueVirtualCamera : BaseVirtualCamera);
            ResetCameraPitch();
            HeroTransform.forward = MainCamera.transform.forward.ToHorizontal3();
            CameraRotation();
        }

        void SetActiveDialogueCamera(bool enabled) {
            if (_targetUnlockedCameraActivityCache.HasValue) {
                _targetUnlockedCameraActivityCache = enabled;
                return;
            }
            DialogueVirtualCamera.enabled = enabled;
            _dialogueCameraActiveDuration = 0;
            // --- We should never hide hero body in dialogues
            if (!Hero.TppActive) {
                UpdateHandsVisibility(enabled);
            }

            World.Only<GameCamera>().SetCinemachineCamera(enabled ? DialogueVirtualCamera : BaseVirtualCamera);
        }

        void UpdateHandsVisibility(bool enabled) {
            //If hands should be hidden (enabled == true) we need to check if there is at least one HeroInvolvement that hides hands
            //If hands should be shown (enabled == false) we don't need to check anything because it means there are no HeroInvolvements
            bool updateHands = !enabled || World.Any<IHeroInvolvement>(h => h.HideHands) != null;
            if (updateHands) {
                if (!enabled) {
                    foreach (var renderer in _hiddenKandraRenderers) {
                        if (renderer != null && renderer.gameObject != null) {
                            renderer.gameObject.SetActive(true);
                        }
                    }
                    _hiddenKandraRenderers.Clear();
                } else {
                    var controller = Hero.Current.VHeroController;
                    if (controller == null) {
                        Log.Critical?.Error($"VHeroController is null when trying to hide hands");
                        return;
                    }
                    if (controller.HeroAnimator == null) {
                        Log.Critical?.Error($"HeroAnimator is null when trying to hide hands");
                        return;
                    }
                    var kandraRenderers = controller.HeroAnimator.GetComponentsInChildren<KandraRenderer>();
                    foreach (var kandraRenderer in kandraRenderers) {
                        if (_hiddenKandraRenderers.Contains(kandraRenderer)) {
                            continue;
                        }
                        kandraRenderer.gameObject.SetActive(false);
                        _hiddenKandraRenderers.Add(kandraRenderer);
                    }
                }
            }
        }
        
        void UpdateDamping(float damping) {
            CinemachineComposer.m_HorizontalDamping = damping;
            CinemachineComposer.m_VerticalDamping = damping;
        }

        [Serializable]
        public struct SmoothClampingData {
            public Vector3 targetDirection;
            public float pitchRange;
            public float yawRange;
            public float smoothingRange;
        }
    }
}