using System.Collections.Generic;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.TG.Main.Grounds;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    public class OutOfGameBoundsValidator : IStaticRenderingValidatorWithInit {
        Bounds _gameBounds;
        Bounds _vegetationBounds;

        public void Init() {
            var groundBounds = Object.FindAnyObjectByType<GroundBounds>();
            if (!groundBounds) {
                _gameBounds = new Bounds(Vector3.zero, float.PositiveInfinity.UniformVector3());
                _vegetationBounds = new Bounds(Vector3.zero, float.PositiveInfinity.UniformVector3());
            } else {
                _gameBounds = groundBounds.CalculateGameBounds();
                _vegetationBounds = groundBounds.CalculateVegetationBounds();
            }
        }

        public void Check(in RenderingContextObject context, List<RenderingErrorMessage> errorMessages) {
            if (context.context is Component { gameObject: { activeInHierarchy: true } } component) {
                Bounds contextBounds = new Bounds(component.transform.position, 1f.UniformVector3());
                Bounds targetContainBounds = _vegetationBounds;
                if (component is Renderer renderer) {
                    contextBounds = renderer.bounds;
                } else if (component is Collider collider) {
                    contextBounds = collider.bounds;
                    targetContainBounds = _gameBounds;
                } else if (component is DrakeMeshRenderer drakeMeshRenderer) {
                    contextBounds = drakeMeshRenderer.WorldBounds.ToBounds();
                    targetContainBounds = _gameBounds;
                }
                if (!targetContainBounds.Intersects(contextBounds)) {
                    errorMessages.Add(new($"Object is out of bounds\n{contextBounds}", RenderingErrorLogType.Exception));
                }
            }
        }
    }
}
