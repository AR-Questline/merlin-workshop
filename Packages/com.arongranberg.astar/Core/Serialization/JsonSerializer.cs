using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding.Collections;
using Unity.Collections;


namespace Pathfinding.Serialization {
	/// <summary>Holds information passed to custom graph serializers</summary>
	public class GraphSerializationContext {
		private readonly GraphNode[] id2NodeMapping;

		/// <summary>
		/// Deserialization stream.
		/// Will only be set when deserializing
		/// </summary>
		public readonly BinaryReader reader;

		/// <summary>
		/// Serialization stream.
		/// Will only be set when serializing
		/// </summary>
		public readonly BinaryWriter writer;

		/// <summary>
		/// Index of the graph which is currently being processed.
		/// Version: uint instead of int after 3.7.5
		/// </summary>
		public readonly uint graphIndex;

		/// <summary>Metadata about graphs being deserialized</summary>
		public readonly GraphMeta meta;

		public bool[] persistentGraphs;

		public GraphSerializationContext (BinaryReader reader, GraphNode[] id2NodeMapping, uint graphIndex, GraphMeta meta) {
        }

        public GraphSerializationContext (BinaryWriter writer, bool[] persistentGraphs) {
        }

        public void SerializeNodeReference (GraphNode node) {
        }

        public void SerializeConnections (Connection[] connections, bool serializeMetadata) {
        }

        public Connection[] DeserializeConnections (bool deserializeMetadata) {
            return default;
        }

        public GraphNode DeserializeNodeReference () {
            return default;
        }

        /// <summary>Write a Vector3</summary>
        public void SerializeVector3 (Vector3 v) {
        }

        /// <summary>Read a Vector3</summary>
        public Vector3 DeserializeVector3()
        {
            return default;
        }

        /// <summary>Write an Int3</summary>
        public void SerializeInt3(Int3 v)
        {
        }

        /// <summary>Read an Int3</summary>
        public Int3 DeserializeInt3()
        {
            return default;
        }

        public UnsafeSpan<T> ReadSpan<T>(Allocator allocator) where T : unmanaged
        {
            return default;
        }
    }

	/// <summary>
	/// Handles low level serialization and deserialization of graph settings and data.
	/// Mostly for internal use. You can use the methods in the AstarData class for
	/// higher level serialization and deserialization.
	///
	/// See: AstarData
	/// </summary>
	public class AstarSerializer {
		private AstarData data;

		/// <summary>Memory stream with the zip data</summary>
		private MemoryStream zipStream;

		/// <summary>Graph metadata</summary>
		private GraphMeta meta;

		/// <summary>Settings for serialization</summary>
		private SerializeSettings settings;

		/// <summary>
		/// Root GameObject used for deserialization.
		/// This should be the GameObject which holds the AstarPath component.
		/// Important when deserializing when the component is on a prefab.
		/// </summary>
		private GameObject contextRoot;

		/// <summary>Graphs that are being serialized or deserialized</summary>
		private NavGraph[] graphs;
		bool[] persistentGraphs;

		/// <summary>
		/// Index used for the graph in the file.
		/// If some graphs were null in the file then graphIndexInZip[graphs[i]] may not equal i.
		/// Used for deserialization.
		/// </summary>
		private Dictionary<NavGraph, int> graphIndexInZip;

		private int graphIndexOffset;

		/// <summary>Extension to use for binary files</summary>
		const string binaryExt = ".binary";

		/// <summary>Extension to use for json files</summary>
		const string jsonExt = ".json";

		/// <summary>
		/// Checksum for the serialized data.
		/// Used to provide a quick equality check in editor code
		/// </summary>
		private uint checksum = 0xffffffff;

		System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

		/// <summary>Cached StringBuilder to avoid excessive allocations</summary>
		static System.Text.StringBuilder _stringBuilder = new System.Text.StringBuilder();

		/// <summary>
		/// Returns a cached StringBuilder.
		/// This function only has one string builder cached and should
		/// thus only be called from a single thread and should not be called while using an earlier got string builder.
		/// </summary>
		static System.Text.StringBuilder GetStringBuilder () {
            return default;
        }

        /// <summary>Cached version object for 3.8.3</summary>
        public static readonly System.Version V3_8_3 = new System.Version(3, 8, 3);

		/// <summary>Cached version object for 3.9.0</summary>
		public static readonly System.Version V3_9_0 = new System.Version(3, 9, 0);

		/// <summary>Cached version object for 4.1.0</summary>
		public static readonly System.Version V4_1_0 = new System.Version(4, 1, 0);

		/// <summary>Cached version object for 4.3.2</summary>
		public static readonly System.Version V4_3_2 = new System.Version(4, 3, 2);

		/// <summary>Cached version object for 4.3.6</summary>
		public static readonly System.Version V4_3_6 = new System.Version(4, 3, 6);

		/// <summary>Cached version object for 4.3.37</summary>
		public static readonly System.Version V4_3_37 = new System.Version(4, 3, 37);

		/// <summary>Cached version object for 4.3.12</summary>
		public static readonly System.Version V4_3_12 = new System.Version(4, 3, 12);

		/// <summary>Cached version object for 4.3.68</summary>
		public static readonly System.Version V4_3_68 = new System.Version(4, 3, 68);

		/// <summary>Cached version object for 4.3.74</summary>
		public static readonly System.Version V4_3_74 = new System.Version(4, 3, 74);

		/// <summary>Cached version object for 4.3.80</summary>
		public static readonly System.Version V4_3_80 = new System.Version(4, 3, 80);

		/// <summary>Cached version object for 4.3.83</summary>
		public static readonly System.Version V4_3_83 = new System.Version(4, 3, 83);

		/// <summary>Cached version object for 4.3.85</summary>
		public static readonly System.Version V4_3_85 = new System.Version(4, 3, 85);

		/// <summary>Cached version object for 4.3.87</summary>
		public static readonly System.Version V4_3_87 = new System.Version(4, 3, 87);

		/// <summary>Cached version object for 5.1.0</summary>
		public static readonly System.Version V5_1_0 = new System.Version(5, 1, 0);

		/// <summary>Cached version object for 5.2.0</summary>
		public static readonly System.Version V5_2_0 = new System.Version(5, 2, 0);

		public AstarSerializer (AstarData data, GameObject contextRoot) : this(data, SerializeSettings.Settings, contextRoot) {
        }

        public AstarSerializer (AstarData data, SerializeSettings settings, GameObject contextRoot) {
        }

        public void SetGraphIndexOffset (int offset) {
        }

        void AddChecksum (byte[] bytes) {
        }

        void AddEntry (string name, byte[] bytes) {
        }

        public uint GetChecksum () {
            return default;
        }

        #region Serialize

        public void OpenSerialize () {
        }

        public byte[] CloseSerialize () {
            return default;
        }

        public void SerializeGraphs(NavGraph[] _graphs)
        {
        }

        /// <summary>Serialize metadata about all graphs</summary>
        byte[] SerializeMeta()
        {
            return default;
        }

        /// <summary>Serializes the graph settings to JSON and returns the data</summary>
        public byte[] Serialize(NavGraph graph)
        {
            return default;
        }

        static int GetMaxNodeIndexInAllGraphs(NavGraph[] graphs)
        {
            return default;
        }

        static byte[] SerializeNodeIndices(NavGraph[] graphs)
        {
            return default;
        }

        /// <summary>Serializes info returned by NavGraph.SerializeExtraInfo</summary>
        static byte[] SerializeGraphExtraInfo(NavGraph graph, bool[] persistentGraphs)
        {
            return default;
        }

        /// <summary>
        /// Used to serialize references to other nodes e.g connections.
        /// Nodes use the GraphSerializationContext.GetNodeIdentifier and
        /// GraphSerializationContext.GetNodeFromIdentifier methods
        /// for serialization and deserialization respectively.
        /// </summary>
        static byte[] SerializeGraphNodeReferences(NavGraph graph, bool[] persistentGraphs)
        {
            return default;
        }

        public void SerializeExtraInfo()
        {
        }

        #endregion

        #region Deserialize


        bool ContainsEntry(string name)
        {
            return default;
        }

        public bool OpenDeserialize(ReadOnlySpan<byte> bytes)
        {
            return default;
        }

        /// <summary>
        /// Returns a version with all fields fully defined.
        /// This is used because by default new Version(3,0,0) > new Version(3,0).
        /// This is not the desired behaviour so we make sure that all fields are defined here
        /// </summary>
        static System.Version FullyDefinedVersion(System.Version v)
        {
            return default;
        }

        public void CloseDeserialize()
        {
        }

        NavGraph DeserializeGraph(int zipIndex, int graphIndex, System.Type[] availableGraphTypes)
        {
            return default;
        }

        /// <summary>
        /// Deserializes graph settings.
        /// Note: Stored in files named "graph<see cref=".json"/>" where # is the graph number.
        /// </summary>
        public NavGraph[] DeserializeGraphs(System.Type[] availableGraphTypes, bool allowLoadingNodes)
        {
            return default;
        }

        bool DeserializeExtraInfo(NavGraph graph)
        {
            return default;
        }

        bool AnyDestroyedNodesInGraphs()
        {
            return default;
        }

        GraphNode[] DeserializeNodeReferenceMap()
        {
            return default;
        }

        void DeserializeNodeReferences(NavGraph graph, GraphNode[] int2Node)
        {
        }

        void DeserializeAndRemoveOldNodeLinks(GraphSerializationContext ctx)
        {
        }



        /// <summary>
        /// Deserializes extra graph info.
        /// Extra graph info is specified by the graph types.
        /// See: Pathfinding.NavGraph.DeserializeExtraInfo
        /// Note: Stored in files named "graph<see cref="_extra.binary"/>" where # is the graph number.
        /// </summary>
        void DeserializeExtraInfo()
        {
        }

        /// <summary>Calls PostDeserialization on all loaded graphs</summary>
        public void PostDeserialization()
        {
        }

        /// <summary>
        /// Deserializes graph editor settings.
        /// For future compatibility this method does not assume that the graphEditors array matches the <see cref="graphs"/> array in order and/or count.
        /// It searches for a matching graph (matching if graphEditor.target == graph) for every graph editor.
        /// Multiple graph editors should not refer to the same graph.
        /// Note: Stored in files named "graph<see cref="_editor.json"/>" where # is the graph number.
        ///
        /// Note: This method is only used for compatibility, newer versions store everything in the graph.serializedEditorSettings field which is already serialized.
        /// </summary>
        void DeserializeEditorSettingsCompatibility()
        {
        }

        #endregion

        #region Utils

        /// <summary>Save the specified data at the specified path</summary>
        public static void SaveToFile(string path, byte[] data)
        {
        }

        /// <summary>Load the specified data from the specified path</summary>
        public static byte[] LoadFromFile(string path)
        {
            return default;
        }

        #endregion
    }

    /// <summary>Metadata for all graphs included in serialization</summary>
    public class GraphMeta {
        /// <summary>Project version it was saved with</summary>
        public Version version;

        /// <summary>Number of graphs serialized</summary>
        public int graphs;

        /// <summary>Guids for all graphs</summary>
        public List<string> guids;

        /// <summary>Type names for all graphs</summary>
        public List<string> typeNames;

        /// <summary>Returns the Type of graph number index</summary>
        public Type GetGraphType (int index, System.Type[] availableGraphTypes)
        {
            return default;
        }
    }

	/// <summary>Holds settings for how graphs should be serialized</summary>
	public class SerializeSettings {
		/// <summary>
		/// Enable to include node data.
		/// If false, only settings will be saved
		/// </summary>
		public bool nodes = true;

		/// <summary>Serialization settings for only saving graph settings</summary>
		public static SerializeSettings Settings => new SerializeSettings {
			nodes = false
		};

		/// <summary>Serialization settings for serializing nodes and settings</summary>
		public static SerializeSettings NodesAndSettings => new SerializeSettings();
	}
}
