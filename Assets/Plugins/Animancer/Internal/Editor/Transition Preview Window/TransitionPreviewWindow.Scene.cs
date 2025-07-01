// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/TransitionPreviewWindow
    partial class TransitionPreviewWindow
    {
        /************************************************************************************************************************/

        /// <summary>The <see cref="Scene"/> of the current <see cref="TransitionPreviewWindow"/> instance.</summary>
        public static Scene InstanceScene => _Instance != null ? _Instance._Scene : null;

        /************************************************************************************************************************/

        /// <summary>Temporary scene management for the <see cref="TransitionPreviewWindow"/>.</summary>
        /// <remarks>
        /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/transitions#previews">Previews</see>
        /// </remarks>
        [Serializable]
        public class Scene
        {
            /************************************************************************************************************************/
            #region Fields and Properties
            /************************************************************************************************************************/

            /// <summary><see cref="HideFlags.HideAndDontSave"/> without <see cref="HideFlags.NotEditable"/>.</summary>
            private const HideFlags HideAndDontSave = HideFlags.HideInHierarchy | HideFlags.DontSave;

            /// <summary>The scene displayed by the <see cref="TransitionPreviewWindow"/>.</summary>
            [SerializeField]
            private UnityEngine.SceneManagement.Scene _Scene;

            /// <summary>The root object in the preview scene.</summary>
            public Transform PreviewSceneRoot { get; private set; }

            /// <summary>The root of the model in the preview scene. A child of the <see cref="PreviewSceneRoot"/>.</summary>
            public Transform InstanceRoot { get; private set; }

            /// <summary>
            /// An instance of the <see cref="Settings.SceneEnvironment"/>.
            /// A child of the <see cref="PreviewSceneRoot"/>.
            /// </summary>
            public GameObject EnvironmentInstance { get; private set; }

            /************************************************************************************************************************/

            [SerializeField]
            private Transform _OriginalRoot;

            /// <summary>The original model which was instantiated to create the <see cref="InstanceRoot"/>.</summary>
            public Transform OriginalRoot
            {
                get => _OriginalRoot;
                set
                {
                    _OriginalRoot = value;
                    InstantiateModel();

                    if (value != null)
                        Settings.AddModel(value.gameObject);
                }
            }

            /************************************************************************************************************************/

            /// <summary>The <see cref="Animator"/> components attached to the <see cref="InstanceRoot"/> and its children.</summary>
            public Animator[] InstanceAnimators { get; private set; }

            [SerializeField] private int _SelectedInstanceAnimator;
            [NonSerialized] private AnimationType _SelectedInstanceType;

            /// <summary>The <see cref="Animator"/> component currently being used for the preview.</summary>
            public Animator SelectedInstanceAnimator
            {
                get
                {
                    if (InstanceAnimators == null ||
                        InstanceAnimators.Length == 0)
                        return null;

                    if (_SelectedInstanceAnimator > InstanceAnimators.Length)
                        _SelectedInstanceAnimator = InstanceAnimators.Length;

                    return InstanceAnimators[_SelectedInstanceAnimator];
                }
            }

            /************************************************************************************************************************/

            [NonSerialized]
            private AnimancerPlayable _Animancer;

            /// <summary>The <see cref="AnimancerPlayable"/> being used for the preview.</summary>
            public AnimancerPlayable Animancer
            {
                get
                {
                    if ((_Animancer == null || !_Animancer.IsValid) &&
                        InstanceRoot != null)
                    {
                        var animator = SelectedInstanceAnimator;
                        if (animator != null)
                        {
                            AnimancerPlayable.SetNextGraphName($"{animator.name} (Animancer Preview)");
                            _Animancer = AnimancerPlayable.Create();
                            _Animancer.CreateOutput(
                                new AnimancerEditorUtilities.DummyAnimancerComponent(animator, _Animancer));
                            _Animancer.RequirePostUpdate(Animations.WindowMatchStateTime.Instance);
                            _Instance._Animations.NormalizedTime = _Instance._Animations.NormalizedTime;
                        }
                    }

                    return _Animancer;
                }
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Initialization
            /************************************************************************************************************************/

            /// <summary>Initializes this <see cref="Scene"/>.</summary>
            public void OnEnable()
            {
            }

            /************************************************************************************************************************/

            private void CreateScene()
            {
            }

            /************************************************************************************************************************/

            internal void OnEnvironmentPrefabChanged()
            {
            }

            /************************************************************************************************************************/

            private void InstantiateModel()
            {
            }

            /************************************************************************************************************************/

            /// <summary>Disables all unnecessary components on the `root` or its children.</summary>
            private static void DisableUnnecessaryComponents(GameObject root)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Sets the <see cref="SelectedInstanceAnimator"/>.</summary>
            public void SetSelectedAnimator(int index)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Called when the target transition property is changed.</summary>
            public void OnTargetPropertyChanged()
            {
            }

            /************************************************************************************************************************/

            private void FocusCamera()
            {
            }

            /************************************************************************************************************************/

            private static Bounds CalculateBounds(Transform transform)
            {
                return default;
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Execution
            /************************************************************************************************************************/

            /// <summary>Called when the window GUI is drawn.</summary>
            public void OnGUI()
            {
            }

            /************************************************************************************************************************/

            private void OnPlayModeChanged(PlayModeStateChange change)
            {
            }

            /************************************************************************************************************************/

            private void OnSceneOpening(string path, OpenSceneMode mode)
            {
            }

            /************************************************************************************************************************/

            private void DoCustomGUI(SceneView sceneView)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Is the `obj` a <see cref="GameObject"/> in the preview scene?</summary>
            public bool IsSceneObject(Object obj)
            {
                return default;
            }

            /************************************************************************************************************************/

            [SerializeField]
            private List<Transform> _ExpandedHierarchy;

            /// <summary>A list of all objects with their child hierarchy expanded.</summary>
            public List<Transform> ExpandedHierarchy
            {
                get
                {
                    if (_ExpandedHierarchy == null)
                        _ExpandedHierarchy = new List<Transform>();
                    return _ExpandedHierarchy;
                }
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Cleanup
            /************************************************************************************************************************/

            /// <summary>Called by <see cref="TransitionPreviewWindow.OnDisable"/>.</summary>
            public void OnDisable()
            {
            }

            /************************************************************************************************************************/

            /// <summary>Called by <see cref="TransitionPreviewWindow.OnDestroy"/>.</summary>
            public void OnDestroy()
            {
            }

            /************************************************************************************************************************/

            /// <summary>Destroys the <see cref="InstanceRoot"/>.</summary>
            public void DestroyModelInstance()
            {
            }

            /************************************************************************************************************************/

            private void DestroyAnimancerInstance()
            {
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }
    }
}

#endif

