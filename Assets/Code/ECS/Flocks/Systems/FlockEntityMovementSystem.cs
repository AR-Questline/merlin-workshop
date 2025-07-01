using Awaken.ECS.Components;
using Awaken.ECS.DrakeRenderer;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Awaken.ECS.Flocks {
    [UpdateInGroup(typeof(FlockSystemGroup))]
    [UpdateAfter(typeof(FlockEntityAvoidanceDataSetSystem))]
    [BurstCompile] [RequireMatchingQueriesForUpdate]
    public partial class FlockEntityMovementSystem : SystemBase {
        public const float DistanceToTargetEpsilon = 0.05f;
        const float OrbitingDistanceMultiplier = 2.1f;
        const float RotationAngleAlignEpsilon = 2 * math.TORADIANS;
        const float OrbitingDotValue = 0.3f;

        protected override void OnUpdate() {
            var deltaTime = SystemAPI.Time.DeltaTime;
            if (deltaTime == 0) {
                return;
            }

            var currentTime = SystemAPI.Time.ElapsedTime;
            var currentFrame = UnityEngine.Time.frameCount;

            Dependency = Entities.WithNone<CulledEntityTag>()
                .ForEach((ref CurrentMovementVector currentMovementVector, ref DrakeVisualEntitiesTransform flockEntityTransform, in TargetParams targetParams,
                    in MovementParams movementParams, in MovementStaticParams movementStaticParams,
                    in AvoidanceData avoidanceData, in AvoidanceColliderData avoidanceColliderData) => {
                    // Copy variables for shorter anc clearer names
                    var position = flockEntityTransform.position;
                    var rotation = flockEntityTransform.rotation;
                    bool isMovingTowardRestPosition = targetParams.targetPositionIsRestPosition;
                    // Calculate movement direction and current speed
                    var currentSpeed = math.length(currentMovementVector.value);
                    var movementDirection = math.select(currentMovementVector.value / currentSpeed, math.forward(), currentSpeed <= 0);
                    // Calculate distance and direction to target position
                    bool useOverridenTargetPosition = targetParams.useOverridenTargetPosition | (targetParams.useFlockTargetPosMinTime > currentTime);
                    var targetPosition = math.select(targetParams.flockTargetPosition, targetParams.overridenTargetPosition, useOverridenTargetPosition);
                    var vectorToTargetPos = targetPosition - position;
                    var distanceToTargetPos = math.length(vectorToTargetPos);
                    var dirToTargetPos = math.normalizesafe(vectorToTargetPos / distanceToTargetPos, math.forward());

                    bool isInRestDistance = distanceToTargetPos <= avoidanceColliderData.radius;
                    bool moveDirectlyToRestPos = isMovingTowardRestPosition & isInRestDistance;
                    bool reachedRestPos = moveDirectlyToRestPos & distanceToTargetPos < DistanceToTargetEpsilon;
                    // Calculate smooth direction towards target position from current rotation
                    var exactRotationToTarget = GetLookRotationWithForwardAxisPriority(dirToTargetPos, math.up());
                    var steeringSpeedMult = math.select(movementParams.steeringSpeedMult, movementStaticParams.toRestSteeringSpeedMult, isMovingTowardRestPosition);

                    var smoothRotationToTargetPosition = GetSmoothRotationTowards(rotation, exactRotationToTarget,
                        steeringSpeedMult, deltaTime);
                    var smoothDirectionToTarget = math.rotate(smoothRotationToTargetPosition, math.forward());

                    quaternion smoothRotationAwayFromObstacle = smoothRotationToTargetPosition;
                    float3 smoothDirectionAwayFromObstacle = smoothDirectionToTarget;
                    // Calculate direction to avoid obstacle
                    if ((isMovingTowardRestPosition == false) & (avoidanceData.valueSetFrame == currentFrame)) {
                        var directionAwayFromObstacle = GetDirectionAwayFromObstacle(position, avoidanceData.upDownHitPosition, avoidanceData.leftRightHitPosition);
                        var upDirFromSmoothDirToTarget = math.rotate(smoothRotationToTargetPosition, math.up());
                        var rotationAwayFromObstacle = GetLookRotationWithForwardAxisPriority(directionAwayFromObstacle, upDirFromSmoothDirToTarget);
#if UNITY_EDITOR && ENABLE_FLOCKS_DEBUG_RAYS
                        var directionAwayFromObstacleFromRotation = math.rotate(rotationAwayFromObstacle, math.forward());
                        UnityEngine.Debug.DrawRay(position, directionAwayFromObstacleFromRotation, UnityEngine.Color.red);
#endif
                        var dotToObstacle = math.dot(-directionAwayFromObstacle, smoothDirectionToTarget);
                        var angleToObstacle = math.acos(math.max(dotToObstacle, 0));
                        var angleToObstacle01 = math.unlerp(0, math.PI / 2, angleToObstacle);
                        var maxAvoidanceSpeedMult = 1 - math.pow(angleToObstacle01, movementStaticParams.avoidanceSpeedMultiplierCurvePow);
                        var distanceToObstacle = math.distance(position, avoidanceData.upDownHitPosition);
                        var colliderRadius = avoidanceColliderData.radius;
                        var exceedingDistanceToObstacle = math.max(colliderRadius - distanceToObstacle, 0);
                        var exceedingDistance01 = exceedingDistanceToObstacle / colliderRadius;
                        var exceedingDistanceMult = math.square(exceedingDistance01);
                        var avoidanceDistanceMultiplierAdd = exceedingDistanceMult * movementStaticParams.avoidanceRotationSpeedAdditionWhenExceeding;
                        var maxAvoidanceSpeed = movementStaticParams.avoidanceSteeringParams.maxRotationSpeed * maxAvoidanceSpeedMult + avoidanceDistanceMultiplierAdd;
                        smoothRotationAwayFromObstacle = GetSmoothRotationTowards(smoothRotationToTargetPosition, rotationAwayFromObstacle,
                            maxAvoidanceSpeed, movementStaticParams.avoidanceSteeringParams.dampingCurvePow, movementStaticParams.avoidanceSteeringParams.dampingMultMinValue,
                            deltaTime);
                        smoothDirectionAwayFromObstacle = math.rotate(smoothRotationAwayFromObstacle, math.forward());
#if UNITY_EDITOR && ENABLE_FLOCKS_DEBUG_RAYS
                        UnityEngine.Debug.DrawRay(position, smoothDirectionAwayFromObstacle, UnityEngine.Color.green);
#endif
                    }

                    var rotationAngle = math.acos(math.dot(movementDirection, smoothDirectionAwayFromObstacle));
                    var rotationSpeed = rotationAngle / deltaTime;

                    // Calculate final movement direction
                    var finalMovementDirection = math.select(smoothDirectionAwayFromObstacle, dirToTargetPos, moveDirectlyToRestPos);

                    // Calculate speed difference
                    var targetSpeed = math.select(movementParams.movementSpeed, 0, isMovingTowardRestPosition);
                    var speedDiff = targetSpeed - currentSpeed;

                    // Calculate if is orbiting
                    var orbitingDistance = OrbitingDistance(currentSpeed, rotationSpeed);
                    bool hasOrbitingAngle = math.abs(math.dot(movementDirection, dirToTargetPos)) < OrbitingDotValue;
                    bool isInOrbitingRange = distanceToTargetPos < (orbitingDistance * OrbitingDistanceMultiplier);
                    bool isOrbiting = hasOrbitingAngle & isInOrbitingRange;

                    // Calculate speed with acceleration
                    bool needsToAccelerate = speedDiff > 0;
                    var maxDeceleration = math.select(-movementStaticParams.maxDeceleration, -movementStaticParams.maxDecelerationForReachRestPosition,
                        !needsToAccelerate & isMovingTowardRestPosition);
                    var acceleration = math.select(maxDeceleration, movementStaticParams.maxAcceleration, needsToAccelerate);
                    //var acceleration = math.select(movementAcceleration, -movementParams.maxDecelerationForReachRestPosition, !needsToAccelerate & isInRestDistance);
                    bool isStoppingAndNearRestPosition = !needsToAccelerate & reachedRestPos & !isOrbiting;
                    var minSpeed = math.select(movementStaticParams.minSpeedForMovingToRestPosition, 0, isStoppingAndNearRestPosition);
                    var restSpeedAccelerationMult = math.select(1, float.MaxValue, isStoppingAndNearRestPosition);
                    var speedWithAcceleration = math.clamp(currentSpeed + (acceleration * restSpeedAccelerationMult * deltaTime), minSpeed, movementParams.movementSpeed);

                    // Calculate is it needed to delay deceleration when moving to rest position
                    float timeToReachZeroSpeed = TimeToStartDeceleration(distanceToTargetPos, movementStaticParams.maxDeceleration, 0, speedWithAcceleration);
                    bool delayDeceleration = isMovingTowardRestPosition & !isInRestDistance & ((timeToReachZeroSpeed > 0) & !isOrbiting);

                    // Calculate final speed
                    bool delayAcceleration = ((targetSpeed < math.EPSILON & !needsToAccelerate) | targetSpeed >= math.EPSILON & needsToAccelerate) &
                                             ((math.abs(speedDiff) < math.EPSILON) | (delayDeceleration & !needsToAccelerate));
                    var finalSpeed = math.select(speedWithAcceleration, currentSpeed, delayAcceleration);
                    // Calculate final position
                    var moveOffset = (finalMovementDirection * deltaTime * finalSpeed);
                    var finalPosition = position + moveOffset;

                    // Calculate rest rotation
                    var currentRotationForwardVector = math.rotate(rotation, math.forward());
                    var currentFacingRestRotation = GetLookRotationWithUpAxisPriority(currentRotationForwardVector, math.up());
                    bool useRestOverrideRotation = targetParams.restRotation.Equals(default) == false & reachedRestPos;
                    var exactRestRotationValue = math.select(currentFacingRestRotation.value, targetParams.restRotation.value, useRestOverrideRotation);
                    var smoothRestRotation = GetSmoothRotationTowards(rotation, new quaternion(exactRestRotationValue),
                        movementStaticParams.toRestSteeringSpeedMult, deltaTime);

                    // Calculate final rotation
                    var finalRotation = new quaternion(math.select(smoothRotationAwayFromObstacle.value, smoothRestRotation.value, moveDirectlyToRestPos));

                    // Assign entity values
                    currentMovementVector.value = finalMovementDirection * finalSpeed;
                    flockEntityTransform.position = finalPosition;
                    flockEntityTransform.rotation = finalRotation;
                }).Schedule(Dependency);

            Dependency = Entities.WithNone<CulledEntityTag>()
                .ForEach((ref LODWorldReferencePoint lodRefPoint, ref WorldRenderBounds worldRenderBounds, in CurrentMovementVector movementVector) => {
                    var movementSpeed = math.length(movementVector.value);
                    var movementSpeedForFrame = movementSpeed / deltaTime;
                    var movementDirection = (movementVector.value / movementSpeed);
                    var positionOffset = movementDirection * movementSpeedForFrame;
                    lodRefPoint.Value += positionOffset;
                    worldRenderBounds.Value.Center += positionOffset;
                }).Schedule(Dependency);
        }

        static float3 GetDirectionAwayFromObstacle(float3 currentPos, float3 upDownHitPos, float3 leftRightHitPos) {
            var vectorToUpDownHit = upDownHitPos - currentPos;
            var vectorToLeftRightHit = leftRightHitPos - currentPos;
            var distanceToUpDownHit = math.length(vectorToUpDownHit);
            var distanceToLeftRightHit = math.length(vectorToLeftRightHit);
            // Using simply the sum of these two vectors with negative sign would produce vector which faces
            // more away from the farthest hit point and there it is needed the opposite, so inverting the ratios of the vectors lengths
            var ratioUpDownToLeftRight = distanceToUpDownHit / distanceToLeftRightHit;
            var directionAwayFromObstacle = -math.normalize(
                (vectorToUpDownHit / distanceToLeftRightHit) +
                ((vectorToLeftRightHit / distanceToLeftRightHit) * ratioUpDownToLeftRight));
            return directionAwayFromObstacle;
        }

        static quaternion GetSmoothRotationTowards(quaternion currentRotation, quaternion targetRotation, float rotationSpeedMultiplier,
            float rotationDampingCurvePow, float rotationDampingMinMult, float deltaTime) {
            var angleBetweenCurrentAndToTargetRotation = math.angle(currentRotation, targetRotation);
            var angle01 = math.unlerp(0, math.PI, angleBetweenCurrentAndToTargetRotation);
            var rotationDampingMult = math.clamp(
                math.pow(angle01, rotationDampingCurvePow),
                rotationDampingMinMult, 1);
            var dampedRotationSpeed = rotationSpeedMultiplier * rotationDampingMult;
            var rotationSlerpValue = math.select(math.min((dampedRotationSpeed * deltaTime) / angleBetweenCurrentAndToTargetRotation, 1), 1,
                angleBetweenCurrentAndToTargetRotation < RotationAngleAlignEpsilon);

            var smoothRotationToTarget = math.slerp(currentRotation, targetRotation, rotationSlerpValue);
            return smoothRotationToTarget;
        }

        static quaternion GetSmoothRotationTowards(quaternion currentRotation, quaternion targetRotation, float rotationSpeedMultiplier,
            float deltaTime) {
            return math.slerp(currentRotation, targetRotation, rotationSpeedMultiplier * deltaTime);
        }

        static quaternion GetLookRotationWithUpAxisPriority(float3 forward, float3 up) {
            forward = math.select(forward, math.cross(up, math.mul(quaternion.RotateX(math.radians(90)), up)), math.abs(math.dot(forward, up)) > 0.99f);
            float3 t = math.normalize(math.cross(up, forward));
            return quaternion.LookRotation(math.cross(t, up), up);
        }

        static quaternion GetLookRotationWithForwardAxisPriority(float3 forward, float3 up) {
            up = math.select(up, math.cross(forward, math.mul(quaternion.RotateX(math.radians(90)), forward)), math.abs(math.dot(forward, up)) > 0.99f);
            return quaternion.LookRotation(forward, up);
        }

        static float TimeToStartDeceleration(float distanceToTargetPosition, float maxDeceleration, float targetSpeedAtTargetPosition, float currentSpeed) {
            return TimeToStartSpeedChange(distanceToTargetPosition, maxDeceleration, currentSpeed, targetSpeedAtTargetPosition, currentSpeed);
        }

        [UnityEngine.Scripting.Preserve]
        static float TimeToStartAcceleration(float distanceToTargetPosition, float maxAcceleration, float targetSpeedAtTargetPosition, float currentSpeed) {
            return TimeToStartSpeedChange(distanceToTargetPosition, maxAcceleration, targetSpeedAtTargetPosition, currentSpeed, currentSpeed);
        }

        static float TimeToStartSpeedChange(float distanceToTargetPosition, float maxAccelerationOrDeceleration, float biggerSpeed, float smallerSpeed, float currentSpeed) {
            // Compute the required distance to reach the target speed
            float distanceToReachTargetSpeed = (math.square(biggerSpeed) - math.square(smallerSpeed)) / (2 * maxAccelerationOrDeceleration);

            // Compute the distance to travel at constant speed before starting acceleration/deceleration
            float distanceUntilStartSpeedChange = math.max(distanceToTargetPosition - distanceToReachTargetSpeed, 0);

            // Time to travel at constant speed before starting the speed change
            float timeUntilStartSpeedChange = math.select(0, distanceUntilStartSpeedChange / currentSpeed, currentSpeed > 0);
            return timeUntilStartSpeedChange;
        }

        static float OrbitingDistance(float velocity, float maxRotationSpeed) {
            return velocity / maxRotationSpeed;
        }
    }
}