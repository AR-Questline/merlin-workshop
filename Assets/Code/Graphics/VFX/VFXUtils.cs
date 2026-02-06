using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Tutorials.Utility;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

namespace Awaken.TG.Graphics.VFX {
    public static class VFXUtils {
        public static void PlayVfx(VisualEffect vfx) {
            if (vfx == null) {
                return;
            }
            PlayVfxInternal(vfx, vfx.gameObject.GetComponentsInChildren<IVFXOnPlayEffects>());
        }
        
        public static void PlayVfx(GameObject vfx) {
            if (vfx == null) {
                return;
            }
            PlayVfxInternal(vfx.GetComponentInChildren<VisualEffect>(), vfx.GetComponentsInChildren<IVFXOnPlayEffects>());
        }

        static void PlayVfxInternal(VisualEffect vfx, IEnumerable<IVFXOnPlayEffects> onPlayEffects) {
            try {
                if (vfx != null) {
                    vfx.Play();
                }

                foreach (var onPlayEffect in onPlayEffects) {
                    onPlayEffect.VFXPlayed();
                }
            } catch (Exception e) {
                // We need to catch all exceptions here because broken visuals can't break logic (addresables, pooling etc.)
                Debug.LogException(e);
            }
        }
        
        public static void StopVfx(VisualEffect vfx) {
            if (vfx == null) {
                return;
            }
            StopVfxInternal(vfx, vfx.gameObject.GetComponentsInChildren<IVFXOnStopEffects>());
        }
        
        public static void StopVfx(GameObject vfx) {
            if (vfx == null) {
                return;
            }
            StopVfxInternal(vfx.GetComponentInChildren<VisualEffect>(), vfx.GetComponentsInChildren<IVFXOnStopEffects>());
        }

        static void StopVfxInternal(VisualEffect vfx, IEnumerable<IVFXOnStopEffects> onStopEffects) {
            try {
                if (vfx != null) {
                    vfx.Stop();
                }

                foreach (var onStopEffect in onStopEffects) {
                    onStopEffect.VFXStopped();
                }
            } catch (Exception e) {
                // We need to catch all exceptions here because broken visuals can't break logic (addresables, pooling etc.)
                Debug.LogException(e);
            }
        }
        
        public static void StopVfxAndDestroy(VisualEffect vfx, float destroyDelay) {
            StopVfx(vfx);
            Object.Destroy(vfx.gameObject, destroyDelay);
        }
        
        public static void StopVfxAndDestroy(GameObject vfx, float destroyDelay) {
            StopVfx(vfx);
            Object.Destroy(vfx, destroyDelay);
        }
        
        public static void StopVfxAndDisableGameObject(VisualEffect vfx, GameObject gameObject, float delay = 5f) {
            StopVfx(vfx);
            AsyncUtil.GameObjectDisableDelay(gameObject, delay).Forget();
        }
        
        public static void StopVfxAndDiscard(Model model, float destroyDelay) {
            if (model is { HasBeenDiscarded: false }) {
                StopVfxAndDiscard(model.MainView.gameObject, model, destroyDelay);
            }
        }
        
        public static void StopVfxAndDiscard(VisualEffect vfx, Model model, float destroyDelay) {
            StopVfx(vfx);
            if (model is { HasBeenDiscarded: false }) {
                TutorialEventUtils.DiscardAfterTime(model, destroyDelay);
            }
        }

        public static void StopVfxAndDiscard(GameObject vfx, Model model, float destroyDelay) {
            StopVfx(vfx);
            if (model is { HasBeenDiscarded: false }) {
                TutorialEventUtils.DiscardAfterTime(model, destroyDelay);
            }
        }

        public static void StopVfxAndReturn(IPooledInstance pooledVfx, float destroyDelay) {
            if (pooledVfx == null) {
                return;
            }
            StopVfx(pooledVfx.Instance);
            pooledVfx.Return(destroyDelay).Forget();
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void StopVfxOnDiscard(VisualEffect vfx, Model model) {
            model.ListenTo(Model.Events.BeforeDiscarded, _ => StopVfx(vfx), model);
        }

        [UnityEngine.Scripting.Preserve]
        public static void StopVfxOnDiscard(GameObject vfx, Model model) {
            model.ListenTo(Model.Events.BeforeDiscarded, _ => StopVfx(vfx), model);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void StopVfxAndDestroyOnDiscard(VisualEffect vfx, float discardDelay, Model model) {
            model.ListenTo(Model.Events.BeforeDiscarded, _ => StopVfxAndDestroy(vfx, discardDelay), model);
        }

        [UnityEngine.Scripting.Preserve]
        public static void StopVfxAndDestroyOnDiscard(GameObject vfx, float discardDelay, Model model) {
            model.ListenTo(Model.Events.BeforeDiscarded, _ => StopVfxAndDestroy(vfx, discardDelay), model);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void StopVfxAndDiscardModelOnDiscard(Model modelToDiscard, float discardDelay, Model modelToListen) {
            modelToListen.ListenTo(Model.Events.BeforeDiscarded, _ => StopVfxAndDiscard(modelToDiscard, discardDelay), modelToListen);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void StopVfxAndDiscardModelOnDiscard(VisualEffect vfx, Model modelToDiscard, float discardDelay, Model modelToListen) {
            modelToListen.ListenTo(Model.Events.BeforeDiscarded, _ => StopVfxAndDiscard(vfx, modelToDiscard, discardDelay), modelToListen);
        }

        [UnityEngine.Scripting.Preserve]
        public static void StopVfxAndDiscardModelOnDiscard(GameObject vfx, Model modelToDiscard, float discardDelay, Model modelToListen) {
            modelToListen.ListenTo(Model.Events.BeforeDiscarded, _ => StopVfxAndDiscard(vfx, modelToDiscard, discardDelay), modelToListen);
        }
    }
}