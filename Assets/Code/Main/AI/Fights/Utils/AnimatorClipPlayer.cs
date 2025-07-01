using System;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.FootSteps;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.Terrain;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.Utility.Threads;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using FMODUnity;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.AI.Fights.Utils {
    public class AnimatorClipPlayer : MonoBehaviour {
        const int FootstepsLogErrorThreshold = 10;
        ForFrameValue<int> _calledThisFrame;
        
        EventReference _footStepEventPath;
        FMODParameter[] _fmodParameters;
        
        LayerMask GroundMask => RenderLayers.Mask.CharacterGround;
        
        void Start() {
            _footStepEventPath = AliveAudioType.FootStep.RetrieveFrom(VGUtils.GetModel<NpcElement>(gameObject));
            if (_footStepEventPath.IsNull) {
                _footStepEventPath = CommonReferences.Get.AudioConfig.DefaultEnemyFootStep;
            }
        }

        public void PlayClipDirect(string pathToEvent) {
            //RuntimeManager.PlayOneShot(pathToEvent);
        }

        public void PlayClip(Object o) {
            MainThreadDispatcher.InvokeAsync(() => {
                if (o is FModEventRef template) {
                    FMODManager.PlayAttachedOneShotWithParameters(template, gameObject, this);
                }
            });
        }
        
        public void FootStep() {
            _calledThisFrame.Value += 1;

            // --- Compare to FootstepsLogErrorThreshold to log error only once, since when this error occurs it can trigger even 20k calls and such many LogErrors also slows down the game
            if (_calledThisFrame == FootstepsLogErrorThreshold) {
                Log.Debug?.Error("FootStep animation event called way too many times in one frame!", gameObject);
                return;
            }
            
            // --- Prevent calling FootStep audio more than once per frame, it can happen when blending 2 walk animations.
            if (_calledThisFrame > 1) {
                return;
            }
            

            if (_footStepEventPath.IsNull) {
                return;
            }
            
            bool groundHit = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out var raycastHit, 0.5f, GroundMask, QueryTriggerInteraction.Ignore);
            if (!groundHit) {
                return;
            }

            if (_fmodParameters != null || FootStepsUtils.TryInitialize(ref _fmodParameters, _footStepEventPath)) {
                string currentSurfaceTypeParamName = SurfaceType.TerrainGround.FModParameterName;
                if (raycastHit.collider.TryGetComponentInParent(out MeshSurfaceType meshSurfaceType)) {
                    currentSurfaceTypeParamName = meshSurfaceType.SurfaceType.FModParameterName;
                }
                FootStepsUtils.ClearAllAndSet(_fmodParameters, currentSurfaceTypeParamName, 1f);
                // --- Play footstep sound
                FMODManager.PlayOneShot(_footStepEventPath, transform.position, this, _fmodParameters);
            }
        }
    }
}
