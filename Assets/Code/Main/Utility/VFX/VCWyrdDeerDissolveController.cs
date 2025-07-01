using Awaken.TG.Graphics.VFX.ShaderControlling;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Utility;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    [RequireComponent(typeof(MaterialGatherer))]
    public class VCWyrdDeerDissolveController : MonoBehaviour {
        static readonly int Transition = Shader.PropertyToID("_Ghost_Transparency");
        const float DissolveMaxSpeed = 5f;
        const float DissolveCompletelyAtDistance = 250f * 250f;
        const float FullyVisibleAtDistance = 350f * 350f;

        float _dissolve;
        MaterialGatherer _materialGatherer;
        
        void Start() {
            _materialGatherer = GetComponent<MaterialGatherer>();
            _materialGatherer.Gather();
        }

        void Update() {
            if (Hero.Current == null) {
                return;
            }
            
            float deltaTime = Hero.Current.GetDeltaTime();
            float distanceToHeroSqr = (transform.position - Hero.Current.Coords).sqrMagnitude;
            float desiredDissolve = distanceToHeroSqr.Remap(DissolveCompletelyAtDistance, FullyVisibleAtDistance, 0, 1, true);
            _dissolve = mathExt.MoveTowards(_dissolve,desiredDissolve, DissolveMaxSpeed * deltaTime);
            foreach (var material in _materialGatherer.Materials) {
                material.SetFloat(Transition, _dissolve);
            }
        }

        void OnDestroy() {
            _materialGatherer.Release();
            _materialGatherer = null;
        }
    }
}
