using Awaken.TG.Assets;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public class ProjectileVisualData : MonoBehaviour {
        [ARAssetReferenceSettings(new[] {typeof(GameObject)}, true), FoldoutGroup("On Contact")]
        public ShareableARAssetReference hitVFX; 
        [FoldoutGroup("On Contact"), ShowIf(nameof(IsHitVFXSetUp))] public float hitVFXDuration = PrefabPool.DefaultVFXLifeTime;
        [FoldoutGroup("On Contact"), ShowIf(nameof(IsHitVFXSetUp))] public bool setVFXProjectileForwardPropertyOnHit;
        [FoldoutGroup("On Contact")]
        public float sphereDamageDuration = 0.1f;
        [FoldoutGroup("On Contact")]
        public float sphereEndRadius = 2f;
        
        [ARAssetReferenceSettings(new[] {typeof(GameObject)}, true), FoldoutGroup("On Lifetime")]
        public ShareableARAssetReference lifetimeStartVFX;
        [FoldoutGroup("On Lifetime"), ShowIf(nameof(IsLifeTimeStartVFXSetUp))] public float lifetimeStartVFXDuration = PrefabPool.DefaultVFXLifeTime;
        
        [ARAssetReferenceSettings(new[] {typeof(GameObject)}, true), FoldoutGroup("On Lifetime")] 
        public ShareableARAssetReference lifetimeEndVFX;
        [FoldoutGroup("On Lifetime"), ShowIf(nameof(IsLifeTimeEndVFXSetUp))] public float lifetimeEndVFXDuration = PrefabPool.DefaultVFXLifeTime;
        [FoldoutGroup("On Lifetime"), ShowIf(nameof(IsLifeTimeEndVFXSetUp))] public bool setVFXProjectileForwardPropertyOnLifetimeEnd;
        
        [ARAssetReferenceSettings(new[] {typeof(GameObject)}, true), FoldoutGroup("On Enviro")] 
        public ShareableARAssetReference enviroVFX;
        [FoldoutGroup("On Enviro"), ShowIf(nameof(IsEnviroVFXSetUp))] public float enviroVFXDuration = PrefabPool.DefaultVFXLifeTime;
        [FoldoutGroup("On Enviro"), ShowIf(nameof(IsEnviroVFXSetUp))] public bool setVFXProjectileForwardPropertyOnEnviro;
        [FoldoutGroup("On Enviro")] public float destructionSphereDamageDuration = 0.1f;
        [FoldoutGroup("On Enviro")] public float destructionSphereEndRadius = 2f;
        
        [HideIf(nameof(useBoxCast))] public float raycastSphereSize = 0.5f;
        public bool useBoxCast;
        [ShowIf(nameof(useBoxCast))] public Vector2 boxCastSize;
        
        public float timeForTrailsToDie = 0.5f;
        public GameObject trailHolder;
        
        bool IsHitVFXSetUp => hitVFX.IsSet;
        bool IsLifeTimeStartVFXSetUp => lifetimeStartVFX.IsSet;
        bool IsLifeTimeEndVFXSetUp => lifetimeEndVFX.IsSet;
        bool IsEnviroVFXSetUp => enviroVFX.IsSet;
        
        void OnDrawGizmosSelected() {
            if (useBoxCast) {
                var rotation = transform.rotation;
                var startPoint = transform.position;
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(startPoint, rotation, Vector3.one);
                Gizmos.matrix = rotationMatrix;
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(boxCastSize.x, boxCastSize.y, 1));
            }
        }

    }
}