using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.ExtraCustom, "For items that can be used as gliders.")]
    public class GliderAttachment : MonoBehaviour, IAttachmentSpec {

        [SerializeField, FoldoutGroup("Gliding stats")] public float minHeightToStartGliding = 2.0f;
        [SerializeField, FoldoutGroup("Gliding stats")] public float minDownVelocityToStartGliding = 0f;
        [SerializeField, FoldoutGroup("Gliding stats")] public float minWallHitForceToEndGliding = 4f;
        
        [SerializeField, FoldoutGroup("Preflight stats")] public float preFlightDuration = 0.8f;
        [SerializeField, FoldoutGroup("Preflight stats")] public float preFlightHorizontalSpeedFactor = 0.7f;
        [SerializeField, FoldoutGroup("Preflight stats")] public float preFlightVerticalSpeedFactor = 0.2f;
        
        [SerializeField, FoldoutGroup("Postflight stats")] public float postFlightDuration = 0.5f;
        
        [SerializeField, FoldoutGroup("Movement stats")] public float constantDownwardsVelocity = 2.0f;
        [SerializeField, FoldoutGroup("Movement stats")] public AnimationCurve flightSpeedByAngleCurve;
        [SerializeField, FoldoutGroup("Movement stats")] public float glidingAcceleration = 30.0f;
        [SerializeField, FoldoutGroup("Movement stats")] public float glidingDeceleration = 3.0f;

        [SerializeField, FoldoutGroup("Steering stats")] public float lowestFlightAngle = 70.0f;
        [SerializeField, FoldoutGroup("Steering stats")] public float highestFlightAngle = -10.0f;
        [SerializeField, FoldoutGroup("Steering stats")] public float pitchClampingDistance = 5.0f;
        [SerializeField, FoldoutGroup("Steering stats")] public float pitchUpSpeed = 100.0f;
        [SerializeField, FoldoutGroup("Steering stats")] public float pitchDownSpeed = 140.0f;
        [SerializeField, FoldoutGroup("Steering stats")] public float pitchUpAcceleration = 120.0f;
        [SerializeField, FoldoutGroup("Steering stats")] public float pitchDownAcceleration = 120.0f;
        [SerializeField, FoldoutGroup("Steering stats")] public float pitchDeceleration = 160.0f;
        [SerializeField, FoldoutGroup("Steering stats")] public AnimationCurve turnSpeedByFlightSpeedCurve;
        [SerializeField, FoldoutGroup("Steering stats")] public float turnAcceleration = 200.0f;
        [SerializeField, FoldoutGroup("Steering stats")] public float turnDeceleration = 180.0f;
        
        [SerializeField, FoldoutGroup("Camera settings")] public float cameraLockInMaxForce = 10.0f;
        [SerializeField, FoldoutGroup("Camera settings")] public float cameraLockInAutomaticDelay = 3f;
        [SerializeField, FoldoutGroup("Camera settings")] public float cameraLockInManualDelay = 1f;
        [SerializeField, FoldoutGroup("Camera settings")] public float cameraLockInAutomaticSpeed = 2.0f;
        [SerializeField, FoldoutGroup("Camera settings")] public float cameraLockInManualSpeed = 1.0f;
        [SerializeField, FoldoutGroup("Camera settings")] public float cameraClampPitch = 30f;
        [SerializeField, FoldoutGroup("Camera settings")] public float cameraClampYaw = 90f;
        [SerializeField, FoldoutGroup("Camera settings")] public float cameraClampSmoothingRange = 10f;
        [SerializeField, FoldoutGroup("Camera settings")] public float cameraRollMagnitude = 1f;
        [SerializeField, FoldoutGroup("Camera settings")] public float cameraRollForce = 1f;
        
        [SerializeField, FoldoutGroup("Visuals")] public float maxCameraFovMultiplier = 1.4f;
        [SerializeField, FoldoutGroup("Visuals")] public float cameraFovMultiplierChangeSpeed = 0.1f;
        [SerializeField, FoldoutGroup("Visuals")] public float cameraDirectionalBlurMultiplier = 0.03f;

        public Element SpawnElement() => new GliderItem();

        public bool IsMine(Element element) => element is GliderItem;
    }
}