using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.UI
{
    [CustomEditor(typeof(SharedTableData))]
    class SharedTableDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Hide all the properties as they are edited through the Table Window.
            // We want use the HideInInspector attribute to hide them as it prevents us editing them in the Table Window.
        }
    }
}
