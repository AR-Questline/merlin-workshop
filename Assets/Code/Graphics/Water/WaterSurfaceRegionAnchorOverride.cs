using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Graphics.Water {
    public class WaterSurfaceRegionAnchorOverride : MonoBehaviour {
        const int WaterRenderMask = RenderLayers.Mask.Water;
        
        [SerializeField] float distanceToFindWaterSurface = 10.0f;

        WaterSurfaceRegionAnchorController _overridenWaterSurface;
        
        void OnEnable() {
            using var waterCollidersRentedArray = RentedArray<Collider>.Borrow(1);
            var anchorOverrideTransform = transform;
            
            int count = Physics.OverlapSphereNonAlloc(
                anchorOverrideTransform.position, 
                distanceToFindWaterSurface, 
                waterCollidersRentedArray.GetBackingArray(), 
                WaterRenderMask, 
                QueryTriggerInteraction.Collide);
            
            if (count > 0 && waterCollidersRentedArray[0].TryGetComponentInChildren(out WaterSurfaceRegionAnchorController waterSurface)) {
                _overridenWaterSurface = waterSurface;
                _overridenWaterSurface.OverrideRegionAnchor(anchorOverrideTransform);
            }
        }

        void OnDisable() {
            _overridenWaterSurface?.ResetRegionAnchor();
            _overridenWaterSurface = null;
        }
    }
}