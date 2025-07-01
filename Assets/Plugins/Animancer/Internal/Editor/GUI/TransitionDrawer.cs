// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using Animancer.Units;
using System;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] Draws the Inspector GUI for an <see cref="ITransitionDetailed"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/TransitionDrawer
    /// 
    [CustomPropertyDrawer(typeof(ITransitionDetailed), true)]
    public class TransitionDrawer : PropertyDrawer {
        static bool UseSimplifiedDrawer => EditorPrefs.GetBool("UseSimplifiedTransitionDrawer", false);
        
        /************************************************************************************************************************/

        /// <summary>The visual state of a drawer.</summary>
        protected enum Mode
        {
            Uninitialized,
            Normal,
            AlwaysExpanded,
            Simplified,
        }

        /// <summary>The current state of this drawer.</summary>
        protected Mode _Mode;

        /************************************************************************************************************************/

        /// <summary>
        /// If set, the field with this name will be drawn on the header line with the foldout arrow instead of in its
        /// regular place.
        /// </summary>
        protected readonly string MainPropertyName;

        /// <summary>"." + <see cref="MainPropertyName"/> (to avoid creating garbage repeatedly).</summary>
        protected readonly string MainPropertyPathSuffix;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="TransitionDrawer"/>.</summary>
        public TransitionDrawer() {
        }

        /// <summary>Creates a new <see cref="TransitionDrawer"/> and sets the <see cref="MainPropertyName"/>.</summary>
        public TransitionDrawer(string mainPropertyName)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Returns the property specified by the <see cref="MainPropertyName"/>.</summary>
        private SerializedProperty GetMainProperty(SerializedProperty rootProperty)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Can't cache because it breaks the <see cref="TimelineGUI"/>.</summary>
        public override bool CanCacheInspectorGUI(SerializedProperty property) => false;

        /************************************************************************************************************************/

        /// <summary>Returns the number of vertical pixels the `property` will occupy when it is drawn.</summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Draws the root `property` GUI and calls <see cref="DoChildPropertyGUI"/> for each of its children.</summary>
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
        }

        /************************************************************************************************************************/

        private void DoPropertyGUI(Rect area, SerializedProperty property, GUIContent label, bool isPreviewing)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the <see cref="_Mode"/> is <see cref="Mode.Uninitialized"/>, this method determines how it should start
        /// based on the number of properties in the `serializedObject`. If the only serialized field is an
        /// <see cref="ITransition"/> then it should start expanded.
        /// </summary>
        protected void InitializeMode(SerializedProperty property)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Draws the root property of a transition with an optional main property on the same line.</summary>
        protected virtual void DoHeaderGUI(
            ref Rect area,
            SerializedProperty rootProperty,
            SerializedProperty mainProperty,
            GUIContent label,
            bool isPreviewing)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Draws the GUI the the target transition's main property.</summary>
        protected virtual void DoMainPropertyGUI(Rect area, out Rect labelArea,
            SerializedProperty rootProperty, SerializedProperty mainProperty)
        {
            labelArea = default(Rect);
        }

        /************************************************************************************************************************/

        /// <summary>Draws a small button using the <see cref="TransitionPreviewWindow.Icon"/>.</summary>
        private static void DoPreviewButtonGUI(ref Rect area, SerializedProperty property, bool isPreviewing)
        {
        }

        /// <summary>Draws a small button using the <see cref="TransitionPreviewWindow.Icon"/>.</summary>
        public static bool DoPreviewButtonGUI(ref Rect area, bool selected, string tooltip)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static GUIStyle _PreviewButtonStyle;

        /// <summary>The style used for the button that opens the <see cref="TransitionPreviewWindow"/>.</summary>
        public static GUIStyle PreviewButtonStyle
        {
            get
            {
                if (_PreviewButtonStyle == null)
                {
                    _PreviewButtonStyle = new GUIStyle(AnimancerGUI.MiniButton)
                    {
                        padding = new RectOffset(0, 0, 0, 1),
                        fixedWidth = 0,
                        fixedHeight = 0,
                    };
                }

                return _PreviewButtonStyle;
            }
        }

        /************************************************************************************************************************/

        private void DoChildPropertiesGUI(Rect area, SerializedProperty rootProperty, SerializedProperty mainProperty, bool indent)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the `property` GUI in relation to the `rootProperty` which was passed into <see cref="OnGUI"/>.
        /// </summary>
        protected virtual void DoChildPropertyGUI(ref Rect area, SerializedProperty rootProperty,
            SerializedProperty property, GUIContent label)
        {
        }

        /************************************************************************************************************************/

        /// <summary>The name of the backing field of <c>ClipTransition.NormalizedStartTime</c>.</summary>
        public const string NormalizedStartTimeFieldName = "_NormalizedStartTime";

        /// <summary>
        /// If the `property` is a "Start Time" field, this method draws it as well as the "End Time" below it and
        /// returns true.
        /// </summary>
        public static bool TryDoStartTimeField(ref Rect area, SerializedProperty rootProperty,
            SerializedProperty property, GUIContent label)
        {
            return default;
        }

        /************************************************************************************************************************/
        #region Context
        /************************************************************************************************************************/

        /// <summary>The current <see cref="DrawerContext"/>.</summary>
        public static DrawerContext Context => DrawerContext.Stack.Current;

        /************************************************************************************************************************/

        /// <summary>Details of an <see cref="ITransition"/>.</summary>
        /// https://kybernetik.com.au/animancer/api/Animancer.Editor/DrawerContext
        /// 
        public class DrawerContext : IDisposable
        {
            /************************************************************************************************************************/

            /// <summary>The main property representing the <see cref="ITransition"/> field.</summary>
            public SerializedProperty Property { get; private set; }

            /// <summary>The actual transition object rerieved from the <see cref="Property"/>.</summary>
            public ITransitionDetailed Transition { get; private set; }

            /// <summary>The cached value of <see cref="ITransitionDetailed.MaximumDuration"/>.</summary>
            public float MaximumDuration { get; private set; }

            /************************************************************************************************************************/

            /// <summary>The stack of active contexts.</summary>
            public static readonly LazyStack<DrawerContext> Stack = new LazyStack<DrawerContext>();

            /// <summary>Returns a disposable <see cref="DrawerContext"/> representing the specified parameters.</summary>
            /// <remarks>
            /// Instances are stored in a <see cref="LazyStack{T}"/> and the current one can be accessed via
            /// <see cref="Context"/>.
            /// </remarks>
            public static IDisposable Get(SerializedProperty transitionProperty)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Decrements the <see cref="Stack"/>.</summary>
            public void Dispose()
            {
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

