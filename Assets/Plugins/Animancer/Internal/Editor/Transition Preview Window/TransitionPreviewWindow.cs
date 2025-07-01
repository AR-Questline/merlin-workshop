// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only]
    /// An <see cref="EditorWindow"/> which allows the user to preview animation transitions separately from the rest
    /// of the scene in Edit Mode or Play Mode.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/transitions#previews">Previews</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/TransitionPreviewWindow
    /// 
    [HelpURL(Strings.DocsURLs.TransitionPreviews)]
#if UNITY_2020_1_OR_NEWER
    [EditorWindowTitle]// Prevent the base SceneView from trying to use this type name to find the icon.
#endif
    public partial class TransitionPreviewWindow : SceneView
    {
        /************************************************************************************************************************/
        #region Public API
        /************************************************************************************************************************/

        private static Texture _Icon;

        /// <summary>The icon image used by this window.</summary>
        public static Texture Icon
        {
            get
            {
                if (_Icon == null)
                {
                    // Possible icons: "UnityEditor.LookDevView", "SoftlockInline", "ViewToolOrbit", "ClothInspector.ViewValue".
                    var name = EditorGUIUtility.isProSkin ? "ViewToolOrbit On" : "ViewToolOrbit";

                    _Icon = AnimancerGUI.LoadIcon(name);
                    if (_Icon == null)
                        _Icon = EditorGUIUtility.whiteTexture;
                }

                return _Icon;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Focusses the <see cref="TransitionPreviewWindow"/> or creates one if none exists.
        /// Or closes the existing window if it was already previewing the `transitionProperty`.
        /// </summary>
        public static void OpenOrClose(SerializedProperty transitionProperty)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="AnimancerState.NormalizedTime"/> of the current transition. Can only be set if the property
        /// being previewed matches the current <see cref="TransitionDrawer.Context"/>.
        /// </summary>
        public static float PreviewNormalizedTime
        {
            get => _Instance._Animations.NormalizedTime;
            set
            {
                if (value.IsFinite() &&
                    IsPreviewingCurrentProperty())
                    _Instance._Animations.NormalizedTime = value;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the <see cref="AnimancerState"/> of the current transition if the property being previewed matches
        /// the <see cref="TransitionDrawer.Context"/>. Otherwise returns null.
        /// </summary>
        public static AnimancerState GetCurrentState()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Is the current <see cref="TransitionDrawer.DrawerContext.Property"/> being previewed at the moment?
        /// </summary>
        public static bool IsPreviewingCurrentProperty()
        {
            return default;
        }

        /// <summary>Is the `property` being previewed at the moment?</summary>
        public static bool IsPreviewing(SerializedProperty property)
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Messages
        /************************************************************************************************************************/

        private static TransitionPreviewWindow _Instance;

        [SerializeField] private Object[] _PreviousSelection;
        [SerializeField] private Animations _Animations;
        [SerializeField] private Scene _Scene;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnEnable()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnDisable()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Cleans up this window.</summary>
        protected virtual new void OnDestroy()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
#if UNITY_2021_2_OR_NEWER
        protected override void OnSceneGUI()
#else
        protected override void OnGUI()
#endif
        {
        }

        /************************************************************************************************************************/

        /// <summary>Called multiple times per second while this window is visible.</summary>
        private void Update()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Returns false.</summary>
        /// <remarks>Returning true makes it draw the main scene instead of the custom scene in Unity 2020.</remarks>
        protected override bool SupportsStageHandling() => false;

        /************************************************************************************************************************/

        private void OnSelectionChanged()
        {
        }

        /************************************************************************************************************************/

        private void DeselectPreviewSceneObjects()
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Transition Property
        /************************************************************************************************************************/

        [SerializeField]
        private Serialization.PropertyReference _TransitionProperty;

        /// <summary>The <see cref="SerializedProperty"/> currently being previewed.</summary>
        public static SerializedProperty TransitionProperty => _Instance._TransitionProperty;

        /************************************************************************************************************************/

        /// <summary>The <see cref="ITransitionDetailed"/> currently being previewed.</summary>
        public static ITransitionDetailed Transition
        {
            get
            {
                var property = _Instance._TransitionProperty;
                if (!property.IsValid())
                    return null;

                return property.Property.GetValue<ITransitionDetailed>();
            }
        }

        /************************************************************************************************************************/

        /// <summary>Indicates whether the `property` is able to be previewed by this system.</summary>
        public static bool CanBePreviewed(SerializedProperty property)
        {
            return default;
        }

        /************************************************************************************************************************/

        private void SetTargetProperty(SerializedProperty property)
        {
        }

        /************************************************************************************************************************/

        private void DestroyTransitionProperty()
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Error Intercepts
#if !UNITY_2020_1_OR_NEWER
        /************************************************************************************************************************/

        /// <summary>Prevents log messages between <see cref="Activate"/> and <see cref="IDisposable.Dispose"/>.</summary>
        private class BlockAllLogs : IDisposable, ILogHandler
        {
            private static readonly BlockAllLogs Instance = new BlockAllLogs();

            private ILogHandler _PreviousHandler;

            public static IDisposable Activate()
            {
                AnimancerUtilities.Assert(Instance._PreviousHandler == null,
                    $"{nameof(BlockAllLogs)} can't be used recursively.");

                Instance._PreviousHandler = Debug.unityLogger.logHandler;
                Debug.unityLogger.logHandler = Instance;
                return Instance;
            }

            void IDisposable.Dispose()
            {
                Debug.unityLogger.logHandler = _PreviousHandler;
                _PreviousHandler = null;
            }

            void ILogHandler.LogFormat(LogType logType, Object context, string format, params object[] args) { }

            void ILogHandler.LogException(Exception exception, Object context) { }
        }

        /************************************************************************************************************************/
#endif
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

