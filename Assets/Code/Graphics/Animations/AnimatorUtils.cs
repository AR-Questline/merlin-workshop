using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif
using UnityEngine;

namespace Awaken.TG.Graphics.Animations {
    public static class AnimatorUtils {
        static readonly Dictionary<int, bool> Cache = new();

        public static void ResetCache() => Cache.Clear();

        public static bool HasParameter(this Animator animator, string parameterName) {
            if (animator.runtimeAnimatorController == null || !animator.gameObject.activeInHierarchy) {
                return false;
            }


            int hashCode = GetHashCode(animator, parameterName);
            if (!Cache.TryGetValue(hashCode, out bool val)) {
                val = animator.parameters.Any(p => p.name == parameterName);
                Cache[hashCode] = val;
            }

            return val;
        }

        public static bool HasParameter(this Animator animator, int parameterHash) {
            if (animator.runtimeAnimatorController == null || !animator.gameObject.activeInHierarchy) {
                return false;
            }
            
            int hashCode = GetHashCode(animator, parameterHash);
            if (!Cache.TryGetValue(hashCode, out bool val)) {
                val = animator.parameters.Any(p => p.nameHash == parameterHash);
                Cache[hashCode] = val;
            }

            return val;
        }
        
        static int GetHashCode(Animator animator, string parameter) => GetHashCode(animator, Animator.StringToHash(parameter));
        static int GetHashCode(Animator animator, int parameterHash) {
            unchecked {
                int hashCode = animator.GetHashCode();
                hashCode = (hashCode * 397) ^ parameterHash;
                return hashCode;
            }
        }

        public static void ResetAllTriggersAndBool(this Animator animator) {
            foreach (var parameter in animator.parameters) {
                if (parameter.type == AnimatorControllerParameterType.Trigger) {
                    animator.ResetTrigger(parameter.nameHash);
                } else if (parameter.type == AnimatorControllerParameterType.Bool) {
                    animator.SetBool(parameter.nameHash, false);
                }
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        public static float GetAnimationLength(this Animator animator, string layerName, string clipName) {
            int layerIndex = animator.GetLayerIndex(layerName);
            return GetAnimationLength(animator, layerIndex, clipName);
        }
        
        public static float GetAnimationLength(this Animator animator, int layerIndex, string clipName) {
            var clip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name.Contains(clipName, StringComparison.InvariantCultureIgnoreCase));
            return clip != null ? clip.length : 0;
        }
        
        // --- For Visual Scripting
        [UnityEngine.Scripting.Preserve]
        public static float GetFloat(this Animator animator, string parameterName) {
            if (animator.HasParameter(parameterName)) {
                return animator.GetFloat(parameterName);
            }
            return 0;
        }

        [UnityEngine.Scripting.Preserve]
        public static void SetFloat(this Animator animator, string parameterName, float value) {
            if (animator.HasParameter(parameterName)) {
                animator.SetFloat(parameterName, value);
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void SetFloat(this Animator animator, int parameterHash, float value) {
            if (animator.HasParameter(parameterHash)) {
                animator.SetFloat(parameterHash, value);
            }
        }

        public static void StartProcessingAnimationSpeed(ARHeroAnimancer animator, AnimancerLayer layer,
            HeroLayerType heroLayerType, HeroStateType currentStateType, bool isHeavy,
            WeaponRestriction weaponRestriction) {
            AnimationCurve animCurve = animator.GetAnimationSpeedCurve(heroLayerType, currentStateType);
            AnimationSpeedParams attackStateParams = new(isHeavy, layer, animCurve, weaponRestriction);
            Hero.Current.Trigger(Hero.Events.ProcessAnimationSpeed, attackStateParams);
        }

#if UNITY_EDITOR
        const string FppArmsAnimatorGUID = "2092f42dd4311ea40babf49e9085092a";
        static AnimatorController s_fppArmsAnimator;
#endif
        
        public static List<string> FppArmsLayers() {
            List<string> layers = new();
#if UNITY_EDITOR
            if (s_fppArmsAnimator == null) {
                string path = AssetDatabase.GUIDToAssetPath(FppArmsAnimatorGUID);
                s_fppArmsAnimator = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            }
            if (s_fppArmsAnimator != null) {
                layers.AddRange(s_fppArmsAnimator.layers.Select(t => t.name));
            }
#endif
            return layers;
        }
    }
}