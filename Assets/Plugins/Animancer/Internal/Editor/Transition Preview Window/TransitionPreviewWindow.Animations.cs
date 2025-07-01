// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Animancer.Editor.AnimancerGUI;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/TransitionPreviewWindow
    partial class TransitionPreviewWindow
    {
        /// <summary>Animation details for the <see cref="TransitionPreviewWindow"/>.</summary>
        /// <remarks>
        /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/transitions#previews">Previews</see>
        /// </remarks>
        [Serializable]
        private class Animations
        {
            /************************************************************************************************************************/

            public const string
                PreviousAnimationKey = "Previous Animation",
                NextAnimationKey = "Next Animation";

            /************************************************************************************************************************/

            [NonSerialized] private AnimationClip[] _OtherAnimations;

            [SerializeField]
            private AnimationClip _PreviousAnimation;
            public AnimationClip PreviousAnimation => _PreviousAnimation;

            [SerializeField]
            private AnimationClip _NextAnimation;
            public AnimationClip NextAnimation => _NextAnimation;

            /************************************************************************************************************************/

            public void DoGUI()
            {
            }

            /************************************************************************************************************************/

            private void DoModelGUI()
            {
            }

            /************************************************************************************************************************/

            private static void AddModelSelectionFunctions(GenericMenu menu, List<GameObject> models, GameObject selected)
            {
            }

            /************************************************************************************************************************/

            private string GetModelWarning(GameObject model)
            {
                return default;
            }

            /************************************************************************************************************************/

            private void DoAnimatorSelectorGUI()
            {
            }

            /************************************************************************************************************************/

            public void GatherAnimations()
            {
            }

            /************************************************************************************************************************/

            private void DoAnimationFieldGUI(GUIContent label, ref AnimationClip clip, Action<AnimationClip> setClip)
            {
            }

            /************************************************************************************************************************/

            private static bool DoDropdownObjectField<T>(GUIContent label, bool showDropdown, ref T obj,
                SpacingMode spacingMode = SpacingMode.None) where T : Object
            {
                return default;
            }

            /************************************************************************************************************************/

            private void DoCurrentAnimationGUI(AnimancerPlayable animancer)
            {
            }

            /************************************************************************************************************************/

            private void PlaySequence(AnimancerPlayable animancer)
            {
            }

            private void PlayTransition()
            {
            }

            /************************************************************************************************************************/

            public void OnPlayAnimation()
            {
            }

            /************************************************************************************************************************/

            private void StepBackward()
                => StepTime(-AnimancerSettings.FrameStep);

            private void StepForward()
                => StepTime(AnimancerSettings.FrameStep);

            private void StepTime(float timeOffset)
            {
            }

            /************************************************************************************************************************/

            [SerializeField]
            private float _NormalizedTime;

            public float NormalizedTime
            {
                get => _NormalizedTime;
                set
                {
                    if (!value.IsFinite())
                        return;

                    _NormalizedTime = value;

                    if (!TryShowTransitionPaused(out var animancer, out var transition, out var state))
                        return;

                    var length = state.Length;
                    var speed = state.Speed;
                    var time = value * length;
                    var fadeDuration = transition.FadeDuration * Math.Abs(speed);

                    var startTime = TimelineGUI.GetStartTime(transition.NormalizedStartTime, speed, length);
                    var normalizedEndTime = state.NormalizedEndTime;
                    var endTime = normalizedEndTime * length;
                    var fadeOutEnd = TimelineGUI.GetFadeOutEnd(speed, endTime, length);

                    if (speed < 0)
                    {
                        time = length - time;
                        startTime = length - startTime;
                        value = 1 - value;
                        normalizedEndTime = 1 - normalizedEndTime;
                        endTime = length - endTime;
                        fadeOutEnd = length - fadeOutEnd;
                    }

                    if (time < startTime)// Previous animation.
                    {
                        if (_PreviousAnimation != null)
                        {
                            PlayOther(PreviousAnimationKey, _PreviousAnimation, value);
                            value = 0;
                        }
                    }
                    else if (time < startTime + fadeDuration)// Fade from previous animation to the target.
                    {
                        if (_PreviousAnimation != null)
                        {
                            var fromState = PlayOther(PreviousAnimationKey, _PreviousAnimation, value);

                            state.IsPlaying = true;
                            state.Weight = (time - startTime) / fadeDuration;
                            fromState.Weight = 1 - state.Weight;
                        }
                    }
                    else if (_NextAnimation != null)
                    {
                        if (value < normalizedEndTime)
                        {
                            // Just the main state.
                        }
                        else
                        {
                            var toState = PlayOther(NextAnimationKey, _NextAnimation, value - normalizedEndTime);

                            if (time < fadeOutEnd)// Fade from the target transition to the next animation.
                            {
                                state.IsPlaying = true;
                                toState.Weight = (time - endTime) / (fadeOutEnd - endTime);
                                state.Weight = 1 - toState.Weight;
                            }
                            // Else just the next animation.
                        }
                    }

                    if (speed < 0)
                        value = 1 - value;

                    state.NormalizedTime = state.Weight > 0 ? value : 0;
                    animancer.Evaluate();

                    RepaintEverything();
                }
            }

            /************************************************************************************************************************/

            private bool TryShowTransitionPaused(
                out AnimancerPlayable animancer, out ITransitionDetailed transition, out AnimancerState state)
            {
                animancer = default(AnimancerPlayable);
                transition = default(ITransitionDetailed);
                state = default(AnimancerState);
                return default;
            }

            /************************************************************************************************************************/

            private AnimancerState PlayOther(object key, AnimationClip animation, float normalizedTime, float fadeDuration = 0)
            {
                return default;
            }

            /************************************************************************************************************************/

            internal class WindowMatchStateTime : Key, IUpdatable
            {
                /************************************************************************************************************************/

                public static readonly WindowMatchStateTime Instance = new WindowMatchStateTime();

                /************************************************************************************************************************/

                void IUpdatable.Update()
                {
                }

                /************************************************************************************************************************/

            }

            /************************************************************************************************************************/
        }
    }
}

#endif

