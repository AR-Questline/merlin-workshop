using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Pathfinding.Graphs.Util;
using Pathfinding.Util;
using UnityEditor.SceneManagement;

namespace Pathfinding {
	[CustomEditor(typeof(AstarPath))]
	public class AstarPathEditor : Editor {
		/// <summary>List of all graph editors available (e.g GridGraphEditor)</summary>
		static Dictionary<string, CustomGraphEditorAttribute> graphEditorTypes = new Dictionary<string, CustomGraphEditorAttribute>();

		/// <summary>
		/// Holds node counts for each graph to avoid calculating it every frame.
		/// Only used for visualization purposes
		/// </summary>
		static Dictionary<NavGraph, (float, int, int)> graphNodeCounts;

		/// <summary>List of all graph editors for the graphs. May be larger than script.data.graphs.Length</summary>
		GraphEditor[] graphEditors;

		System.Type[] graphTypes => AstarData.graphTypes;

		static int lastUndoGroup = -1000;

		/// <summary>Used to make sure correct behaviour when handling undos</summary>
		static uint ignoredChecksum;

		const string scriptsFolder = "Assets/AstarPathfindingProject";

		#region SectionFlags

		static bool showSettings, showCustomAreaColors, showTagNames;

		FadeArea settingsArea, colorSettingsArea, editorSettingsArea, aboutArea, optimizationSettingsArea, serializationSettingsArea;
		FadeArea tagsArea, graphsArea, addGraphsArea, alwaysVisibleArea;

		/// <summary>Graph editor which has its 'name' field focused</summary>
		GraphEditor graphNameFocused;

		#endregion

		/// <summary>AstarPath instance that is being inspected</summary>
		public AstarPath script { get; private set; }
		public bool isPrefab { get; private set; }

		#region Styles

		static bool stylesLoaded;
		public static GUISkin astarSkin { get; private set; }

		static GUIStyle level0AreaStyle, level0LabelStyle;
		static GUIStyle level1AreaStyle, level1LabelStyle;

		static GUIStyle graphDeleteButtonStyle, graphInfoButtonStyle, graphGizmoButtonStyle, graphEditNameButtonStyle, graphDuplicateButtonStyle;

		public static GUIStyle helpBox  { get; private set; }
		public static GUIStyle thinHelpBox  { get; private set; }

		#endregion

		/// <summary>Holds defines found in script files, used for optimizations.</summary>
		List<OptimizationHandler.DefineDefinition> defines;

		/// <summary>Enables editor stuff. Loads graphs, reads settings and sets everything up</summary>
		public void OnEnable () {
        }

        /// <summary>
        /// Hide position/rotation/scale tools for the AstarPath object. Instead, OnSceneGUI will draw position tools for each graph.
        ///
        /// We cannot rely on the inspector's OnEnable/OnDisable events, because they are tied to the lifetime of the inspector,
        /// which does not necessarily follow which object is selected. In particular if there are multiple inspector windows, or
        /// an inspector window is locked.
        /// </summary>
        void HideToolsWhileActive()
        {
        }

        void CreateFadeAreas()
        {
        }

        /// <summary>Cleans up editor stuff</summary>
        public void OnDisable()
        {
        }

        /// <summary>Reads settings frome EditorPrefs</summary>
        void GetAstarEditorSettings()
        {
        }

        void SetAstarEditorSettings()
        {
        }

        void RepaintSceneView()
        {
        }

        /// <summary>Tell Unity that we want to use the whole inspector width</summary>
        public override bool UseDefaultMargins()
        {
            return default;
        }

        public override void OnInspectorGUI()
        {
        }

        /// <summary>
        /// Loads GUISkin and sets up styles.
        /// See: EditorResourceHelper.LocateEditorAssets
        /// Returns: True if all styles were found, false if there was an error somewhere
        /// </summary>
        public static bool LoadStyles()
        {
            return default;
        }

        /// <summary>Draws the main area in the inspector</summary>
        void DrawMainArea()
        {
        }

        static void BuildPipelineBake()
        {
        }

        /// <summary>Draws optimizations settings.</summary>
        void DrawOptimizationSettings()
        {
        }

        /// <summary>
        /// Returns a version with all fields fully defined.
        /// This is used because by default new Version(3,0,0) > new Version(3,0).
        /// This is not the desired behaviour so we make sure that all fields are defined here
        /// </summary>
        public static System.Version FullyDefinedVersion(System.Version v)
        {
            return default;
        }

        void DrawAboutArea()
        {
        }

        void DrawGraphHeader(GraphEditor graphEditor)
        {
        }

        void DrawGraphInfoArea(GraphEditor graphEditor)
        {
        }

        /// <summary>Draws the inspector for the given graph with the given graph editor</summary>
        void DrawGraph(GraphEditor graphEditor)
        {
        }

        public void OnSceneGUI()
        {
        }

        void DrawSceneGUISettings()
        {
        }

        void SaveGraphData(byte[] bytes, AstarPath path)
        {
        }

        static void GetUniqueFilePath(string sceneName, string directory, out string cacheFileName, out string fullPath)
        {
            cacheFileName = default(string);
            fullPath = default(string);
        }

        void DrawSerializationSettings()
        {
        }

        public void RunTask(System.Action action)
        {
        }

        void DrawSettings()
        {
        }

        void DrawPathfindingSettings()
        {
        }

        readonly string[] heuristicOptimizationOptions = new[] {
            "None",
            "Random (low quality)",
            "RandomSpreadOut (high quality)",
            "Custom"
        };

        void DrawHeuristicOptimizationSettings()
        {
        }

        /// <summary>Opens the A* Inspector and shows the section for editing tags</summary>
        public static void EditTags()
        {
        }

        void DrawTagSettings()
        {
        }

        void DrawEditorSettings()
        {
        }

        static void DrawColorSlider(ref float left, ref float right, bool editable) {
        }

        void DrawDebugSettings () {
        }

        void DrawColorSettings()
        {
        }

        /// <summary>Make sure every graph has a graph editor</summary>
        void CheckGraphEditors()
        {
        }

        void RemoveGraph(NavGraph graph)
        {
        }

        void DuplidateGraph(NavGraph graph)
        {
        }

        void AddGraph(System.Type type)
        {
        }

        /// <summary>Creates a GraphEditor for a graph</summary>
        GraphEditor CreateGraphEditor(NavGraph graph)
        {
            return default;
        }

        void HandleUndo()
        {
        }

        void SerializeIfDataChanged()
        {
        }

        /// <summary>Called when an undo or redo operation has been performed</summary>
        void OnUndoRedoPerformed()
        {
        }

        public void SaveGraphsAndUndo (EventType et = EventType.Used, string eventCommand = "") {
        }

        public byte[] SerializeGraphs(out uint checksum)
        {
            checksum = default(uint);
            return default;
        }

        public byte[] SerializeGraphs(Pathfinding.Serialization.SerializeSettings settings, out uint checksum)
        {
            checksum = default(uint);
            return default;
        }

        void DeserializeGraphs()
        {
        }

        void DeserializeGraphs(byte[] bytes)
        {
        }

        [MenuItem("Edit/Pathfinding/Scan All Graphs %&s")]
        public static void MenuScan()
        {
        }

        /// <summary>Searches in the current assembly for GraphEditor and NavGraph types</summary>
        void FindGraphTypes()
        {
        }
    }
}
