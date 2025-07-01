using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.ToolbarTools {
    /// <summary>
    /// https://forum.unity.com/threads/feature-request-show-solid-colliders.985064/
    /// </summary>
    [EditorToolbarElement(DrawSolidCollidersToolbar.ID, typeof(SceneView))]
    internal sealed class DrawSolidCollidersToolbar : EditorToolbarToggle {
        public const string ID = "Colliders/DrawSolidColliders";
        const string DrawSolidCollidersID = "Editor_DrawSolidColliders";

        public static bool DrawSolidColliders => EditorPrefs.GetBool(DrawSolidCollidersID, false);

        public DrawSolidCollidersToolbar() {
            text = "Solid Colliders";
            tooltip = "Toggles whether colliders should be drawn as solid or as wireframe.";
            icon = (Texture2D)EditorGUIUtility.Load("d_BoxCollider Icon");
            this.SetValueWithoutNotify(DrawSolidColliders);
            this.RegisterValueChangedCallback(HandleToggleValue);
        }

        static void HandleToggleValue(ChangeEvent<bool> value) {
            EditorPrefs.SetBool(DrawSolidCollidersID, value.newValue);
        }
    }

    internal static class ColliderGizmosDrawer {
        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(BoxCollider))]
        public static void HandleDrawBoxColliderGizmo(BoxCollider collider, GizmoType gizmoType) {
            if (gizmoType.HasFlag(GizmoType.NotInSelectionHierarchy)) {
                return;
            }

            bool solid = DrawSolidCollidersToolbar.DrawSolidColliders;
            Color c = Color.green;
            c.a = 0.5f;
            DrawGizmosUtilities.DrawBoxColliderGizmos(collider, c, solid);
        }
    }

    public static class DrawGizmosUtilities {
        public static void DrawBoxColliderGizmos(BoxCollider box, Color color, bool drawSolid) {
            Gizmos.matrix = box.transform.localToWorldMatrix;
            Gizmos.color = color;

            Vector3 offset = box.center;
            Vector3 size = box.size;

            if (drawSolid) {
                Gizmos.DrawCube(offset, size);
            } else {
                Gizmos.DrawWireCube(offset, size);
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}