using System.Collections.Generic;
using System.Threading;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components.MaterialOverrideComponents;
using Awaken.Utility.Extensions;
using Awaken.Utility.SerializableTypeReference;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Utility.VFX {
    public sealed class VCDissolveController : VCDissolveDeathRelatedControllerBase<IDissolveAble>, IDissolveAbleDissolveController {
        static readonly int DefaultTransitionPropertyId = Shader.PropertyToID("_Transition");
        static SerializableTypeReference DefaultSerializableTypeReference => new (typeof(TransitionOverrideComponent));
        
        [SerializeField] List<DissolveAbleRenderer> renderers;
        [SerializeField] bool overrideTransitionProperty;
        [SerializeField, ShowIf(nameof(overrideTransitionProperty))] string overridenPropertyName;
        [SerializeField, MaterialPropertyComponent, ShowIf(nameof(overrideTransitionProperty))] SerializableTypeReference overridenSerializedType;
        [SerializeField, BoxGroup(AdditionalDissolveEffectsGroupName)] VisualEffect targetVFXGraph;
        [Space]
        [SerializeField, BoxGroup(AdditionalDissolveEffectsGroupName)] Light effectLight;
        [SerializeField, BoxGroup(AdditionalDissolveEffectsGroupName)] AnimationCurve lightIntensity;

        HDAdditionalLightData _lightData;
        bool _useLight;
        bool _useVfxGraph;
        
        int TransitionPropertyId => overrideTransitionProperty ? Shader.PropertyToID(overridenPropertyName) : DefaultTransitionPropertyId;
        SerializableTypeReference SerializableTypeReference => overrideTransitionProperty ? overridenSerializedType : DefaultSerializableTypeReference;

        protected override void OnAttach() {
            if (effectLight != null) {
                _lightData = effectLight.GetComponent<HDAdditionalLightData>();
                _useLight = _lightData != null;
            }

            _useVfxGraph = targetVFXGraph != null;
            foreach (var dissolveAbleRenderer in renderers) {
                if (dissolveAbleRenderer) {
                    AddRenderer(dissolveAbleRenderer);
                }
            }
            base.OnAttach();
        }
        
        protected override void BeforeDissolveStarted(IDissolveAble renderer, float startingTransitionValue) {
            renderer.ChangeToDissolveAble();
            renderer.InitPropertyModification(SerializableTypeReference, startingTransitionValue);
        }
        
        protected override void AfterDissolveEnded(float endValue, CancellationToken ct) {
            if (ct.IsCancellationRequested) {
                return;
            }
            
            if (!_discardOnDisappeared && endValue == Invisible) {
                return;
            }
            
            foreach (var dissolveAbleRenderer in _actualRenderers) {
                dissolveAbleRenderer.RestoreToOriginal();
                dissolveAbleRenderer.FinishPropertyModification(SerializableTypeReference);
            }
        }

        /// <param name="transition">1 means dissolved and 0 means fully visible. It's made this way because of dissolve shader</param>
        protected override void UpdateEffects(float transition) {
            // ReSharper disable once LocalVariableHidesMember
            foreach (var renderer in _actualRenderers) {
                if (renderer != null) {
                    renderer.UpdateProperty(SerializableTypeReference, transition);
                }
            }

            if (_useVfxGraph && targetVFXGraph != null) {
                targetVFXGraph.SetFloat(TransitionPropertyId, transition);
                if (transition <= float.Epsilon) {
                    targetVFXGraph.Play();
                } else if (transition >= 1 - float.Epsilon) {
                    targetVFXGraph.Stop();
                }
            }

            if (_useLight && effectLight != null) {
                effectLight.intensity = lightIntensity.Evaluate(transition);
            }
        }

        protected override void OnRendererAdded(IDissolveAble dissolveable) {
            dissolveable.Init();
            dissolveable.AssignController(this);
        }
        
        protected override bool CanBeDissolved(IDissolveAble dissolvable) {
            if (!dissolveType.HasFlagFast(DissolveType.Weapon) && dissolvable.IsWeapon) {
                return false;
            }

            if (!dissolveType.HasFlagFast(DissolveType.Cloth) && dissolvable.IsCloth) {
                return false;
            }

            return true;
        }
    }
}