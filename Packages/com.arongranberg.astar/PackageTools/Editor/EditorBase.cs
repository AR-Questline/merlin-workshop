using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Pathfinding {
	/// <summary>Helper for creating editors</summary>
	[CustomEditor(typeof(VersionedMonoBehaviour), true)]
	[CanEditMultipleObjects]
	public class EditorBase : Editor {
		static System.Collections.Generic.Dictionary<string, string> cachedTooltips;
		static System.Collections.Generic.Dictionary<string, string> cachedURLs;
		Dictionary<string, SerializedProperty> props = new Dictionary<string, SerializedProperty>();

		static GUIContent content = new GUIContent();
		static GUIContent showInDocContent = new GUIContent("Show in online documentation", "");
		static GUILayoutOption[] noOptions = new GUILayoutOption[0];
		public static System.Func<string> getDocumentationURL;

		protected HashSet<string> remainingUnhandledProperties;


		static void LoadMeta () {
        }

        static string LookupPath (System.Type type, string path, Dictionary<string, string> lookupData) {
            return default;
        }

        string FindTooltip (string path) {
            return default;
        }

        protected virtual void OnEnable () {
        }

        protected virtual void OnDisable () {
        }

        void OnContextMenu (GenericMenu menu, SerializedProperty property) {
        }

        public sealed override void OnInspectorGUI () {
        }

        protected virtual void Inspector () {
        }

        /// <summary>Draws an inspector for all fields that are likely not handled by the editor script itself</summary>
        protected virtual void InspectorForRemainingAttributes (bool showHandled, bool showUnhandled) {
        }

        protected SerializedProperty FindProperty (string name) {
            return default;
        }

        protected void Section (string label) {
        }

        protected bool SectionEnableable (string label, string enabledProperty) {
            return default;
        }

        /// <summary>Bounds field using center/size instead of center/extent</summary>
        protected void BoundsField (string propertyPath) {
        }

        protected void FloatField (string propertyPath, string label = null, string tooltip = null, float min = float.NegativeInfinity, float max = float.PositiveInfinity) {
        }

        protected void FloatField (SerializedProperty prop, string label = null, string tooltip = null, float min = float.NegativeInfinity, float max = float.PositiveInfinity) {
        }

        protected bool PropertyField (string propertyPath, string label = null, string tooltip = null) {
            return default;
        }

        protected bool PropertyField (SerializedProperty prop, string label = null, string tooltip = null) {
            return default;
        }

        bool PropertyField (SerializedProperty prop, string label, string tooltip, string propertyPath) {
            return default;
        }

        protected void Popup (string propertyPath, GUIContent[] options, string label = null) {
        }

        protected void IntSlider(string propertyPath, int left, int right)
        {
        }

        protected void Slider(string propertyPath, float left, float right)
        {
        }

        protected bool ByteAsToggle(string propertyPath, string label)
        {
            return default;
        }

        protected void Clamp(SerializedProperty prop, float min, float max = float.PositiveInfinity)
        {
        }

        protected void Clamp(string name, float min, float max = float.PositiveInfinity)
        {
        }

        protected void ClampInt(string name, int min, int max = int.MaxValue)
        {
        }
    }
}
