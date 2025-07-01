using System;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Mounts {
    public class MountData : Template {

        [SerializeField] Data gameplayData;
        [SerializeField] InputData keyboardInputData;
        [SerializeField] InputData gamepadInputData;

        public Data GameplayData => gameplayData;

        [Serializable]
        public class Data {
            [FoldoutGroup("Running")] public float sprintingSpeed = 14.0f;
            [FoldoutGroup("Running")] public float backingOffSpeed = 3.0f;
            [FoldoutGroup("Running")] public float runningSpeed = 7.0f;
            [FoldoutGroup("Running")] public float walkingSpeed = 3.5f;

            [FoldoutGroup("Running")] public float runningAcceleration = 8.0f;
            [FoldoutGroup("Running")] public float runningDeceleration = 5.0f;

            [FoldoutGroup("Turning")] public float turningStationarySpeed = 2.0f;
            [FoldoutGroup("Turning")] public float turningStationaryAccel = 4.0f;
            [FoldoutGroup("Turning")] public float turningStationaryDecel = 6.0f;
            [FoldoutGroup("Turning")] public float turningGallopSpeed = 1.0f;
            [FoldoutGroup("Turning")] public float turningGallopAccel = 3.0f;
            [FoldoutGroup("Turning")] public float turningGallopDecel = 1.0f;

            [FoldoutGroup("Falling")] public float gravity = -25.0f;
            [FoldoutGroup("Falling")] public float terminalVelocity = -53.0f;
            [FoldoutGroup("Falling")] public float fasterFallingThreshold = -10.0f;
            [FoldoutGroup("Falling")] public float fasterFallingMultiplier = 2.0f;

            [FoldoutGroup("Grounding")] public float groundingSnapForce = -50.0f;
            [FoldoutGroup("Grounding")] public LayerMask groundLayers;

            [FoldoutGroup("Jumping")] public float minimumSpeedForJump = 7.5f;
            [FoldoutGroup("Jumping")] public float jumpHeight = 1.5f;

            [FoldoutGroup("Slopes")] public float slopeCriticalAngle = 45.0f;
            [FoldoutGroup("Slopes")] public float slopeStandTiltRotationSpeed = 50.0f;

            [FoldoutGroup("Movement Trivia")] public float aheadWallDetectionDistance = 2.0f;
            [FoldoutGroup("Movement Trivia")] public float aheadWallDesiredDistance = 1.0f;
            [FoldoutGroup("Movement Trivia")] public float aheadWallDampeningMultiplier = 0.1f;
            [FoldoutGroup("Movement Trivia")] public float wallHitVelocityDampenSpeed = 10.0f;
            [FoldoutGroup("Movement Trivia")] public float minimumSpeedForStep = 5.0f;
            [FoldoutGroup("Movement Trivia")] public int framesToAccumulateHitsFor = 4;

            [FoldoutGroup("Swimming")] public float swimmingSpeed = 5.0f;
            [FoldoutGroup("Swimming")] public float swimmingAcceleration = 2.0f;
            [FoldoutGroup("Swimming")] public float swimmingDeceleration = 1.0f;
            [FoldoutGroup("Swimming")] public float turningSwimmingSpeed = 2.0f;
            [FoldoutGroup("Swimming")] public float turningSwimmingAccel = 1.0f;
            [FoldoutGroup("Swimming")] public float turningSwimmingDecel = 1.0f;
            [FoldoutGroup("Swimming")] public float maxWaterDetectionDistance = 20.0f;
            [FoldoutGroup("Swimming")] public float minimumWaterDepthToEnterWater = 0.5f;
            [FoldoutGroup("Swimming")] public float waterHoverDepth = 0.7f;
            [FoldoutGroup("Swimming")] public float maxDivingWaterDepth = 1.4f;
            [FoldoutGroup("Swimming")] public float bouyancyForce = 5.0f;

            [FoldoutGroup("Game Logic")] public CrimeItemValue crimeValue = CrimeItemValue.High;
            [FoldoutGroup("Game Logic")] public float minDistanceForTeleportation = 150f;
            [FoldoutGroup("Game Logic")] public float requiredDistanceToSeekedPoint = 3.0f;
            [FoldoutGroup("Game Logic")] public float requiredDistanceToSeekedHero = 7.0f;

            [FoldoutGroup("NPC Hit")] public float minimumVelocityForPoiseBreak = 1.0f;
            [FoldoutGroup("NPC Hit")] public float minimumVelocityForRagdoll = 8.0f;
            [FoldoutGroup("NPC Hit")] public float maximumHitAngleForRagdoll = 20.0f;
            [FoldoutGroup("NPC Hit")] public float poiseBreakMaxPushForce = 5.0f;
            [FoldoutGroup("NPC Hit")] public float ragdollForceMultiplier = 20.0f;

            [FoldoutGroup("Visual")] public float runFovMultiplier = 1.15f;
            [FoldoutGroup("Visual")] public float sprintFovMultiplier = 1.25f;
            [FoldoutGroup("Visual")] public float fovIncreaseChangeDuration = 1.5f;
            [FoldoutGroup("Visual")] public float fovDecreaseChangeDuration = 0.7f;

            [FoldoutGroup("Sounds")] public float horseNoiseRangeMultiplier = 150f;
            [FoldoutGroup("Sounds")] public float horseNoiseStrengthMultiplier = 1.5f;
        }

        [Serializable]
        public class InputData {
            [FoldoutGroup("User Input")]
            public AnimationCurve turningInputMappingCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

            [FoldoutGroup("User Input")] public float runningForwardInputHelperScale = 1.0f;
            [FoldoutGroup("User Input")] public float maxHeroHorizontalRotation = 150f;
            [FoldoutGroup("User Input")] public bool clampInputMagnitude = true;
        }

        public InputData GetInputData(bool isGamepad) {
            return isGamepad ? gamepadInputData : keyboardInputData;
        }
    }
}
