// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor
{
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/TransitionPreviewWindow
    partial class TransitionPreviewWindow
    {
        /// <summary>Persistent settings for the <see cref="TransitionPreviewWindow"/>.</summary>
        /// <remarks>
        /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/transitions#previews">Previews</see>
        /// </remarks>
        [Serializable]
        internal class Settings : AnimancerSettings.Group
        {
            /************************************************************************************************************************/

            private static Settings Instance => AnimancerSettings.TransitionPreviewWindow;

            /************************************************************************************************************************/

            public static void DoInspectorGUI()
            {
            }

            /************************************************************************************************************************/
            #region Misc
            /************************************************************************************************************************/

            private static void DoMiscGUI()
            {
            }

            /************************************************************************************************************************/

            [SerializeField]
            [Tooltip("Should this window automatically close if the target object is destroyed?")]
            private bool _AutoClose = true;

            public static bool AutoClose => Instance._AutoClose;

            /************************************************************************************************************************/

            [SerializeField]
            [Tooltip("Should the scene lighting be enabled?")]
            private bool _SceneLighting = false;

            public static bool SceneLighting
            {
                get => Instance._SceneLighting;
                set
                {
                    if (SceneLighting == value)
                        return;

                    var property = Instance.GetSerializedProperty(nameof(_SceneLighting));
                    property.boolValue = value;
                    AnimancerSettings.SerializedObject.ApplyModifiedProperties();
                }
            }

            /************************************************************************************************************************/

            [SerializeField]
            [Tooltip("Should the skybox be visible?")]
            private bool _ShowSkybox = false;

            public static bool ShowSkybox
            {
                get => Instance._ShowSkybox;
                set
                {
                    if (ShowSkybox == value)
                        return;

                    var property = Instance.GetSerializedProperty(nameof(_ShowSkybox));
                    property.boolValue = value;
                    AnimancerSettings.SerializedObject.ApplyModifiedProperties();
                }
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Environment
            /************************************************************************************************************************/

            [SerializeField]
            [Tooltip("If set, the default preview scene lighting will be replaced with this prefab.")]
            private GameObject _SceneEnvironment;

            public static GameObject SceneEnvironment => Instance._SceneEnvironment;

            /************************************************************************************************************************/

            private static void DoEnvironmentGUI()
            {
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Models
            /************************************************************************************************************************/

            private static void DoModelsGUI()
            {
            }

            /************************************************************************************************************************/

            [SerializeField]
            private List<GameObject> _Models;

            /// <summary>The models previously used in the <see cref="TransitionPreviewWindow"/>.</summary>
            /// <remarks>Accessing this property removes missing and duplicate models from the list.</remarks>
            public static List<GameObject> Models
            {
                get
                {
                    var instance = Instance;
                    AnimancerEditorUtilities.RemoveMissingAndDuplicates(ref instance._Models);
                    return instance._Models;
                }
            }

            private static SerializedProperty ModelsProperty => Instance.GetSerializedProperty(nameof(_Models));

            /************************************************************************************************************************/

            public static void AddModel(GameObject model)
            {
            }

            private static void AddModel(List<GameObject> models, GameObject model)
            {
            }

            /************************************************************************************************************************/

            private static GameObject _DefaultHumanoid;

            public static GameObject DefaultHumanoid
            {
                get
                {
                    if (_DefaultHumanoid == null)
                    {
                        // Try to load Animancer's DefaultHumanoid.
                        var path = AssetDatabase.GUIDToAssetPath("c9f3e1113795a054c939de9883b31fed");
                        if (!string.IsNullOrEmpty(path))
                        {
                            _DefaultHumanoid = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            if (_DefaultHumanoid != null)
                                return _DefaultHumanoid;
                        }

                        // Otherwise try to load Unity's DefaultAvatar.
                        _DefaultHumanoid = EditorGUIUtility.Load("Avatar/DefaultAvatar.fbx") as GameObject;

                        if (_DefaultHumanoid == null)
                        {
                            // Otherwise just create an empty object.
                            _DefaultHumanoid = EditorUtility.CreateGameObjectWithHideFlags(
                                "DefaultAvatar", HideFlags.HideAndDontSave, typeof(Animator));
                            _DefaultHumanoid.transform.parent = _Instance._Scene.PreviewSceneRoot;
                        }
                    }

                    return _DefaultHumanoid;
                }
            }

            public static bool IsDefaultHumanoid(GameObject gameObject) => gameObject == DefaultHumanoid;

            /************************************************************************************************************************/

            private static GameObject _DefaultSprite;

            public static GameObject DefaultSprite
            {
                get
                {
                    if (_DefaultSprite == null)
                    {
                        _DefaultSprite = EditorUtility.CreateGameObjectWithHideFlags(
                            "DefaultSprite", HideFlags.HideAndDontSave, typeof(Animator), typeof(SpriteRenderer));
                        _DefaultSprite.transform.parent = _Instance._Scene.PreviewSceneRoot;
                    }

                    return _DefaultSprite;
                }
            }

            public static bool IsDefaultSprite(GameObject gameObject) => gameObject == DefaultSprite;

            /************************************************************************************************************************/

            /// <summary>
            /// Tries to choose the most appropriate model to use based on the properties animated by the target
            /// <see cref="Transition"/>.
            /// </summary>
            public static Transform TrySelectBestModel()
            {
                return default;
            }

            /************************************************************************************************************************/

            private static Transform TrySelectBestModel(HashSet<AnimationClip> clips, List<GameObject> models)
            {
                return default;
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Scene Hierarchy
            /************************************************************************************************************************/

            private static void DoHierarchyGUI()
            {
            }

            /************************************************************************************************************************/

            private static GUIStyle _HierarchyButtonStyle;

            private static void DoHierarchyGUI(Transform root)
            {
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }
    }
}

#endif

