using System.Collections;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Graphics
{
    public class VCCameraDitherFadeMask : ViewComponent<GameCamera>
    {
        // === Shader property
        //static readonly int CameraDitherFadeSphereMaskPos = Shader.PropertyToID("cameraDitherFadeSphereMaskPos");
        static readonly int HeroPositionId = Shader.PropertyToID("SSM_WorldToScreenPoint");

        // Screen Space Mask
        static readonly int SSM_Step = Shader.PropertyToID("SSM_Step");
        static readonly int SSM_Hardness = Shader.PropertyToID("SSM_Hardness");
        static readonly int SSM_NoiseScale = Shader.PropertyToID("SSM_NoiseScale");

        // Sphere Mask
        static readonly int SM_Radius = Shader.PropertyToID("SM_Radius");
        static readonly int SM_Hardness = Shader.PropertyToID("SM_Hardness");

        // === Editor fields
        [Header("Sphere Mask")]
        [Range(1.0f, 100.0f)] public float _SM_Radius;
        [Range(1.0f, 10.0f)] public float _SM_Hardness;

        [Header("Screen Space Mask")]
        public FloatRange _SSM_Hardness;
        [Range(0.01f, 1.0f)] public float _SSM_NoiseScale;
        public FloatRange _SSM_StepRange;
        public Vector3 worldOffset;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _SM_Radius);
        }

        void Update()
        {
            Hero hero = null;
            if ((hero = Hero.Current) == null)
            {
                return;
            }

            float zoomValue = Shader.GetGlobalFloat(GameCamera.ZoomShaderID);

            Shader.SetGlobalFloat(SSM_Step, _SSM_StepRange.max);
            Shader.SetGlobalFloat(SSM_Hardness, Mathf.Lerp(_SSM_Hardness.min, _SSM_Hardness.max, zoomValue));
            Shader.SetGlobalFloat(SSM_NoiseScale, _SSM_NoiseScale);

            // Sphere Mask
            // var position = transform.position;
            // Vector4 pos = new Vector4(position.x, position.y, position.z, 0);
            // Shader.SetGlobalVector(CameraDitherFadeSphereMaskPos, pos);
            Shader.SetGlobalFloat(SM_Radius, _SM_Radius);
            Shader.SetGlobalFloat(SM_Hardness, _SM_Hardness);

            // Hero position
            var heroWorldPos = Ground.CoordsToWorld(hero.Coords) + worldOffset;
            var heroScreenPos = Target.MainCamera.WorldToScreenPoint(heroWorldPos);
            heroScreenPos.x /= Screen.width;
            heroScreenPos.y /= Screen.height;
            bool heroInView = (heroScreenPos.x >= 0 && heroScreenPos.x <= 1) && (heroScreenPos.y >= 0 && heroScreenPos.y <= 1);
            heroScreenPos.z = heroInView ? 1 : 0;
            Shader.SetGlobalVector(HeroPositionId, heroScreenPos);
        }
    }
}