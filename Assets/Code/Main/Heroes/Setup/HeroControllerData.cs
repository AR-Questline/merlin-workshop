using System;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Utility.Animations.HitStops;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Setup {
	public class HeroControllerData : ScriptableObject {
        [FoldoutGroup("Movement")] public float moveSpeed = 6.5f;
        [FoldoutGroup("Movement")] public float backwardMultiplier = 0.6f;
	    [FoldoutGroup("Movement")] public float moveAcceleration = 50f;
	    [FoldoutGroup("Movement")] public float walkSpeed = 3f;
	    [FoldoutGroup("Movement")] public float walkAcceleration = 30f;
	    [FoldoutGroup("Movement")] public float sprintSpeed = 9.0f;
	    [FoldoutGroup("Movement")] public float sprintAcceleration = 80f;
	    [FoldoutGroup("Movement")] public float groundFrictionCoefficient = 0.5f;
	    [FoldoutGroup("Movement")] public float minGroundFriction = 30f;
	    
	    [FoldoutGroup("Movement")] public float rotationSpeed = 65f;
	    [FoldoutGroup("Movement")] public float padRotationSpeed = 165f;
	    [FoldoutGroup("Movement"), Range(0, 1)] public float bowDrawnSpeedMultiplier = 0.3f;
	    [FoldoutGroup("Movement"), Range(0, 4)] public float moveSoundMultiplier = 4f;
	    [FoldoutGroup("Movement"), Range(0.01f, 2)] public float aimSensitivityMultiplier = 0.25f;
	    
	    [FoldoutGroup("Movement")] public float sprintCostPerTick = 0.25f;
	    [FoldoutGroup("Movement")] public float staminaRegenPerTick = 2.5f;
	    [FoldoutGroup("Movement")] public float footstepNoisiness = 1f;
	    [FoldoutGroup("Movement")] public float pushDuration = 2.5f;
	    [FoldoutGroup("Movement")] public float pushForce = 1f;
	    [FoldoutGroup("Movement")] public float slidePushForce = 300f;
        [FoldoutGroup("Movement")] public float blockingMultiplier = 0.3f;
        [FoldoutGroup("Movement"), Range(0f, 1f)] public float minYInputForSprint = 0.75f;
        
	    [FoldoutGroup("Dashing")] public float dashSpeed = 8f;
        
        [FoldoutGroup("Dashing")] public AnimationCurve dashMovementCurve; 
	    [FoldoutGroup("Dashing")] public float dashCooldown = 1.0f; 
	    [Tooltip("Duration of dash in milliseconds")]
	    [FoldoutGroup("Dashing")] public float dashDuration = 0.4f;
	    [FoldoutGroup("Dashing/Armor Modifiers")] public float dashDurationLightArmor = 1f;
	    [FoldoutGroup("Dashing/Armor Modifiers")] public float dashDurationMediumArmor = 0.5f;
	    [FoldoutGroup("Dashing/Armor Modifiers")] public float dashDurationHeavyArmor = 0.2f;
	    [FoldoutGroup("Dashing/Armor Modifiers")] public float dashDurationOverload = 0.2f;
        [FoldoutGroup("Dashing")] public float dashGodModeDurationSeconds = 0.2f;
	    [FoldoutGroup("Dashing")] public float dashStaminaCost = 25f;
	    [FoldoutGroup("Dashing")] public float dashCostMultiplier = 1f;
	    [FoldoutGroup("Dashing")] public int dashMaxOptimalCounters = 3;

	    [FoldoutGroup("Jumping")] public float midairUpwardsSpeed = 16f;
	    [FoldoutGroup("Jumping")] public float midairFallingSpeed = 15f;
	    [FoldoutGroup("Jumping")] public float airDragCoefficient = 0.3f;
	    [FoldoutGroup("Jumping")] public float minAirDrag = 12f;
	    [FoldoutGroup("Jumping")] public float jumpHeight = 1.1f;
	    [FoldoutGroup("Jumping")] public float gravity = -15.0f;
	    [FoldoutGroup("Jumping")] public float jumpTimeout = 0.1f;
	    [FoldoutGroup("Jumping")] public float jumpBufferTime = 0.1f;
        [FoldoutGroup("Jumping")] public float terminalVelocity = -53.0f;
        [FoldoutGroup("Jumping")] public float fasterFallingThreshold = -10.0f;
        [FoldoutGroup("Jumping")] public float fasterFallingMultiplier = 2.0f;
        [FoldoutGroup("Jumping")] public float jumpStaminaCost = 25f;
        [FoldoutGroup("Jumping")] public float jumpSoundMultiplier = 0.5f;

        [FoldoutGroup("Sliding")] public float slideStaminaCost = 25f;
        
        [FoldoutGroup("Standing")] public HeightData standingHeightData;
        [FoldoutGroup("Standing")] [UnityEngine.Scripting.Preserve] public float characterControllerYCenter = 1.055f;
        
        [FoldoutGroup("Crouching")] public float crouchingSpeedMultiplier = 0.6f;
        [FoldoutGroup("Crouching")] public HeightData crouchingHeightData;
        [FoldoutGroup("Crouching"), Range(0.1f, 1f)] public float crouchingInteractionLengthMultiplier = 0.6f;

        [FoldoutGroup("Gliding")] public HeightData glidingHeightData;
        
        [FoldoutGroup("ForeDweller")] [UnityEngine.Scripting.Preserve] public HeightData foreDwellerHeightData;
        
        [FoldoutGroup("Swimming")] public float swimmingOffset = 1.55f;
        [FoldoutGroup("Swimming")] public HeightData swimmingHeightData;
        [FoldoutGroup("Swimming")] public float swimSpeed = 4.5f;
        [FoldoutGroup("Swimming")] public float waterDragCoefficient = 1.5f;
        [FoldoutGroup("Swimming")] public float minWaterDrag = 0f;
        [FoldoutGroup("Swimming")] public float swimAcceleration = 15f;
        [FoldoutGroup("Swimming")] public float oxygenLevel = 100;
        [FoldoutGroup("Swimming")] public float oxygenUsageBase = 3f;
        [FoldoutGroup("Swimming")] public float oxygenRegenPerTick = 20f;
        [FoldoutGroup("Swimming")] public float oxygenUsageDelay = 0.5f;
        [FoldoutGroup("Swimming")] public float suffocateTick = 2;
        [FoldoutGroup("Swimming")] public float suffocatePercentageDamage = 0.1f;
        [FoldoutGroup("Swimming")] public float waterLeaveTimeout = 0.8f;
        [FoldoutGroup("Swimming")] public float wavesImpactRange = 0.5f;
        [FoldoutGroup("Swimming")] public float wavesImpactFalloff = 1f;
        
        [FoldoutGroup("Cinemachine")] public float topClamp = 90.0f;
        [FoldoutGroup("Cinemachine")] public float topClampOnHorse = 30.0f;
        [FoldoutGroup("Cinemachine")] public float bottomClamp = -90.0f;
        [FoldoutGroup("Cinemachine")] public float tppTopClamp = 50.0f;
        [FoldoutGroup("Cinemachine")] public float tppBottomClamp = -50.0f;
        [FoldoutGroup("Cinemachine")] public float tppBowBottomClamp = -50.0f;

        [FoldoutGroup("HeadCheck")] public LayerMask headObstacleLayers;

        [FoldoutGroup("Bow Shake")] public FloatRange firstVectorSize = new(0.1f, 0.15f);
        [FoldoutGroup("Bow Shake")] public FloatRange secondVectorSize = new(0.03f, 0.06f);
        [FoldoutGroup("Bow Shake")] public FloatRange thirdVectorSize = new(0.01f, 0.03f);
        [FoldoutGroup("Bow Shake")] public float firstVectorRotationSpeed = 360f;
        [FoldoutGroup("Bow Shake")] public float secondVectorRotationSpeed = -240f;
        [FoldoutGroup("Bow Shake")] public float thirdVectorRotationSpeed = 60f;

        [FoldoutGroup("Pushing")] public Vector3 pushColliderSize = new(1, 1, 2.5f);
        
        [FoldoutGroup("Back Stabbing")] public float backStabRange = 2.5f;

        [FoldoutGroup("Fall Damage")] public float damageNullifier = 9;
        [FoldoutGroup("Fall Damage")] public float fallDamageMultiplier = 1;

        [FoldoutGroup("Combat")] public LayerMask enemiesHitMask;
        
        // === HitStop
        [FoldoutGroup("HitStop")] public HitStopsAsset hitStopsAsset;
        [FoldoutGroup("HitStop")] public HitStopData environmentHitStopsData;

        public float BackStabRangeSqr => backStabRange * backStabRange;

        [Serializable]
        public class HeightData {
	        public float height;
	        public float groundSubmerging;
	        public float defaultCameraHeight;
	        public float bowingCameraHeight;
	        public float bowingCheckAdditionalLength;
	        public float zoneCheckerHeight;

	        public HeightData Copy() => new() {
		        height = height,
		        groundSubmerging = groundSubmerging,
		        defaultCameraHeight = defaultCameraHeight,
		        bowingCameraHeight = bowingCameraHeight,
		        bowingCheckAdditionalLength = bowingCheckAdditionalLength,
		        zoneCheckerHeight = zoneCheckerHeight
	        };

	        public Sequence TweenTo(HeightData to, float duration) => DOTween.Sequence()
		        .Append(DOTween.To(() => height, v => height = v, to.height, duration))
		        .Join(DOTween.To(() => groundSubmerging, v => groundSubmerging = v, to.groundSubmerging, duration))
		        .Join(DOTween.To(() => defaultCameraHeight, v => defaultCameraHeight = v, to.defaultCameraHeight, duration))
		        .Join(DOTween.To(() => bowingCameraHeight, v => bowingCameraHeight = v, to.bowingCameraHeight, duration))
		        .Join(DOTween.To(() => bowingCheckAdditionalLength, v => bowingCheckAdditionalLength = v, to.bowingCheckAdditionalLength, duration))
		        .Join(DOTween.To(() => zoneCheckerHeight, v => zoneCheckerHeight = v, to.zoneCheckerHeight, duration));
	        
	        public void SetTo(HeightData to) {
		        height = to.height;
		        groundSubmerging = to.groundSubmerging;
		        defaultCameraHeight = to.defaultCameraHeight;
		        bowingCameraHeight = to.bowingCameraHeight;
		        bowingCheckAdditionalLength = to.bowingCheckAdditionalLength;
		        zoneCheckerHeight = to.zoneCheckerHeight;
	        }
        }
	}
}