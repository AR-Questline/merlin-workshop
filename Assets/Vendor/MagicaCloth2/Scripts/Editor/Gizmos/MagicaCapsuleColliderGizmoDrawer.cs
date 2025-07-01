// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaCapsuleColliderのギズモ表示
    /// </summary>
    public class MagicaCapsuleColliderGizmoDrawer
    {
        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy)]
        static void DrawGizmo(MagicaCapsuleCollider scr, GizmoType gizmoType)
        {
        }
    }
}
