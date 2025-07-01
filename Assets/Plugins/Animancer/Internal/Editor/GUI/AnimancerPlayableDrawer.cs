// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] Draws the Inspector GUI for an <see cref="IAnimancerComponent.Playable"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AnimancerPlayableDrawer
    /// 
    public class AnimancerPlayableDrawer
    {
        /************************************************************************************************************************/

        /// <summary>A lazy list of information about the layers currently being displayed.</summary>
        private readonly List<AnimancerLayerDrawer>
            LayerInfos = new List<AnimancerLayerDrawer>();

        /// <summary>The number of elements in <see cref="LayerInfos"/> that are currently being used.</summary>
        private int _LayerCount;

        /************************************************************************************************************************/

        /// <summary>Draws the GUI of the <see cref="IAnimancerComponent.Playable"/> if there is only one target.</summary>
        public void DoGUI(IAnimancerComponent[] targets)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Draws the GUI of the <see cref="IAnimancerComponent.Playable"/>.</summary>
        public void DoGUI(IAnimancerComponent target)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Draws a GUI for the <see cref="Animator.runtimeAnimatorController"/> if there is one.</summary>
        private void DoNativeAnimatorControllerGUI(IAnimancerComponent target)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Returns the state with the specified <see cref="AnimatorState.nameHash"/>.</summary>
        private static AnimatorState GetState(ChildAnimatorState[] states, int nameHash)
        {
            return default;
        }

        /************************************************************************************************************************/

        private void DoPlayableNotInitializedGUI(IAnimancerComponent target)
        {
        }

        /************************************************************************************************************************/

        private void DoLayerWeightWarningGUI(IAnimancerComponent target)
        {
        }

        /************************************************************************************************************************/

        private void DoWeightlessPlayWarningGUI(IAnimancerComponent target)
        {
        }

        /************************************************************************************************************************/

        private void DoMultipleAnimationSystemWarningGUI(IAnimancerComponent target)
        {
        }

        /************************************************************************************************************************/

        private string _UpdateListLabel;
        private static GUIStyle _InternalDetailsStyle;

        /// <summary>Draws a box describing the internal details of the `playable`.</summary>
        internal void DoInternalDetailsGUI(AnimancerPlayable playable)
        {
        }

        /************************************************************************************************************************/
        #region Context Menu
        /************************************************************************************************************************/

        /// <summary>
        /// Checks if the current event is a context menu click within the `clickArea` and opens a context menu with various
        /// functions for the `playable`.
        /// </summary>
        private void CheckContextMenu(Rect clickArea, AnimancerPlayable playable)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Adds functions for controlling the `playable`.</summary>
        public static void AddRootFunctions(GenericMenu menu, AnimancerPlayable playable)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Adds menu functions to set the <see cref="DirectorUpdateMode"/>.</summary>
        private void AddUpdateModeFunctions(GenericMenu menu, AnimancerPlayable playable)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Adds disabled items for each disposable.</summary>
        private void AddDisposablesFunctions(GenericMenu menu, List<IDisposable> disposables)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Adds a menu function to open the Playable Graph Visualiser if it exists in the project.</summary>
        public static void AddPlayableGraphVisualizerFunction(GenericMenu menu, string prefix, PlayableGraph graph)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Prefs
        /************************************************************************************************************************/

        private const string
            KeyPrefix = "Inspector ",
            MenuPrefix = "Display Options/";

        internal static readonly BoolPref
            SortStatesByName = new BoolPref(KeyPrefix, MenuPrefix + "Sort States By Name", true),
            HideInactiveStates = new BoolPref(KeyPrefix, MenuPrefix + "Hide Inactive States", false),
            HideSingleLayerHeader = new BoolPref(KeyPrefix, MenuPrefix + "Hide Single Layer Header", true),
            RepaintConstantly = new BoolPref(KeyPrefix, MenuPrefix + "Repaint Constantly", true),
            SeparateActiveFromInactiveStates = new BoolPref(KeyPrefix, MenuPrefix + "Separate Active From Inactive States", false),
            ScaleTimeBarByWeight = new BoolPref(KeyPrefix, MenuPrefix + "Scale Time Bar by Weight", true),
            ShowInternalDetails = new BoolPref(KeyPrefix, MenuPrefix + "Show Internal Details", false),
            VerifyAnimationBindings = new BoolPref(KeyPrefix, MenuPrefix + "Verify Animation Bindings", true),
            AutoNormalizeWeights = new BoolPref(KeyPrefix, MenuPrefix + "Auto Normalize Weights", true),
            UseNormalizedTimeSliders = new BoolPref("Inspector", nameof(UseNormalizedTimeSliders), false);

        /************************************************************************************************************************/

        /// <summary>Adds functions to the menu for each of the Display Options.</summary>
        public static void AddDisplayOptions(GenericMenu menu)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

