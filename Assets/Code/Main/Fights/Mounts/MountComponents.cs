using System;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Mounts {
    public class MountComponents : MonoBehaviour {
        [SerializeField] Transform[] dismountLocations = Array.Empty<Transform>();
        [SerializeField] float dismountLocationRadius = 0.25f;
        
        [field: SerializeField] public Collider InteractionCollider { get; private set; }
        [field: SerializeField] public Transform AheadWallDetectionPoint { get; private set; }
        [field: SerializeField, OnValueChanged(nameof(CalculateInitialSaddleToSpineOffset))] public Transform MountingParent { get; private set; } 
        [field: SerializeField, OnValueChanged(nameof(CalculateInitialSaddleToSpineOffset))] public Transform DynamicSaddlePosition { get; private set; }
        [field: SerializeField] public Collider WalkThroughCollider { get; private set; }
        [field: SerializeField] public Vector3 InitialSaddleToSpineOffset { get; private set; }
        
        public Transform GetAvailableDismountLocation() {
            using var hitsRentedArray = RentedArray<RaycastHit>.Borrow(1);
            float heroHeight = Hero.Current.Data.standingHeightData.height;
            Vector3 heroPos = Hero.Current.Coords;
            foreach (var location in dismountLocations) {
                if (IsValidDismountLocation(heroPos, heroHeight, location, hitsRentedArray)) {
                    return location;
                }
            }
            
            return dismountLocations[^1];
        }

        bool IsValidDismountLocation(Vector3 heroPos, float heroHeight, Transform dismountLocation, RentedArray<RaycastHit> hitsRentedArray) {
            Vector3 checkPosition = dismountLocation.position;
            Vector3 groundCheckPosition = Ground.SnapToGround(checkPosition);
            if (math.abs(checkPosition.y - groundCheckPosition.y) > heroHeight * 2f) {
                return false;
            }
            // Add bonus height to avoid terrain irregularities
            groundCheckPosition += Vector3.up * 0.1f;
            if (!CheckIfHeroCanFit(groundCheckPosition, hitsRentedArray, heroHeight)) {
                return false;
            }
            if (!CheckIfPositionVisible(heroPos, groundCheckPosition, hitsRentedArray, heroHeight)) {
                return false;
            }
            return true;
        }

        bool CheckIfHeroCanFit(Vector3 groundPosition, RentedArray<RaycastHit> hitsRentedArray, float checkHeight) {
            groundPosition += Vector3.up * dismountLocationRadius;
            checkHeight -= 2 * dismountLocationRadius;
            return Physics.SphereCastNonAlloc(groundPosition, dismountLocationRadius, Vector3.up, hitsRentedArray.GetBackingArray(), checkHeight, RenderLayers.Mask.CharacterGround) == 0;
        }
        
        bool CheckIfPositionVisible(Vector3 initialPosition, Vector3 groundPosition, RentedArray<RaycastHit> hitsRentedArray, float heroHeight) {
            (Vector3, Vector3)[] checkPositions = new (Vector3, Vector3)[]{
                (initialPosition + Vector3.up * heroHeight, groundPosition),
                (initialPosition + Vector3.up * heroHeight, groundPosition + Vector3.up * (heroHeight * 0.5f)),
                (initialPosition + Vector3.up * heroHeight, groundPosition + Vector3.up * heroHeight),
                (initialPosition, groundPosition),
                (initialPosition, groundPosition + Vector3.up * (heroHeight * 0.5f)),
                (initialPosition, groundPosition + Vector3.up * heroHeight),
            };

            for (int i = 0; i < checkPositions.Length; i++) {
                if (Check(checkPositions[i].Item1, checkPositions[i].Item2)) {
                    return true;
                }
            }
            return false;

            bool Check(Vector3 startPos, Vector3 endPosition) {
                Vector3 diff = endPosition - startPos;
                return Physics.RaycastNonAlloc(startPos, diff, hitsRentedArray.GetBackingArray(), diff.magnitude, RenderLayers.Mask.CharacterGround) == 0;
            }
        }
        
        [Button]
        void CalculateInitialSaddleToSpineOffset() {
            InitialSaddleToSpineOffset = MountingParent.InverseTransformPoint(DynamicSaddlePosition.position) * -1.0f;
        }
    }
}