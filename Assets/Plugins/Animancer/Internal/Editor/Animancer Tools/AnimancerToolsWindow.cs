// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor.Tools
{
    /// <summary>[Editor-Only] [Pro-Only]
    /// An <see cref="EditorWindow"/> with various utilities for managing sprites and generating animations.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/tools">Animancer Tools</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.Tools/AnimancerToolsWindow
    /// 
    public sealed partial class AnimancerToolsWindow : EditorWindow
    {
        /************************************************************************************************************************/

        /// <summary>The display name of this window.</summary>
        public const string Name = "Animancer Tools";

        /// <summary>The singleton instance of this window.</summary>
        public static AnimancerToolsWindow Instance { get; private set; }

        [SerializeReference] private List<Tool> _Tools;

        [SerializeField] private Vector2 _Scroll;
        [SerializeField] private int _CurrentTool = -1;

        /************************************************************************************************************************/

        private SerializedObject _SerializedObject;

        private SerializedObject SerializedObject
            => _SerializedObject ?? (_SerializedObject = new SerializedObject(this));

        /// <summary>Returns the <see cref="SerializedProperty"/> which represents the specified `tool`.</summary>
        public SerializedProperty FindSerializedPropertyForTool(Tool tool)
        {
            return default;
        }

        /************************************************************************************************************************/

        private void OnEnable()
        {
        }

        /************************************************************************************************************************/

        private void InitializeTools()
        {
        }

        /************************************************************************************************************************/

        private int IndexOfTool(Type type)
        {
            return default;
        }

        /************************************************************************************************************************/

        private void OnDisable()
        {
        }

        /************************************************************************************************************************/

        private void OnSelectionChange()
        {
        }

        /************************************************************************************************************************/

        private void OnGUI()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Causes this window to redraw its GUI.</summary>
        public static new void Repaint() => ((EditorWindow)Instance).Repaint();

        /// <summary>Calls <see cref="Undo.RecordObject(Object, string)"/> for this window.</summary>
        public static void RecordUndo() => Undo.RecordObject(Instance, Name);

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="EditorGUI.BeginChangeCheck"/>.</summary>
        public static void BeginChangeCheck() => EditorGUI.BeginChangeCheck();

        /// <summary>Calls <see cref="EditorGUI.EndChangeCheck"/> and <see cref="RecordUndo"/> if it returned true.</summary>
        public static bool EndChangeCheck()
        {
            return default;
        }

        /// <summary>Calls <see cref="EndChangeCheck"/> and sets the <c>field = value</c> if it returned true.</summary>
        public static bool EndChangeCheck<T>(ref T field, T value)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Creates and initializes a new <see cref="ReorderableList"/>.</summary>
        public static ReorderableList CreateReorderableList<T>(
            List<T> list, string name, ReorderableList.ElementCallbackDelegate drawElementCallback, bool showFooter = false)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Creates and initializes a new <see cref="ReorderableList"/> for <see cref="Sprite"/>s.</summary>
        public static ReorderableList CreateReorderableObjectList<T>(
            List<T> objects, string name, bool showFooter = false)
            where T : Object
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="ReorderableList"/> for <see cref="string"/>s.</summary>
        public static ReorderableList CreateReorderableStringList(
            List<string> strings, string name, Func<Rect, int, string> doElementGUI)
        {
            return default;
        }

        /// <summary>Creates a new <see cref="ReorderableList"/> for <see cref="string"/>s.</summary>
        public static ReorderableList CreateReorderableStringList(List<string> strings, string name)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Opens the <see cref="AnimancerToolsWindow"/>.</summary>
        [MenuItem(Strings.AnimancerToolsMenuPath)]
        public static void Open() => GetWindow<AnimancerToolsWindow>();

        /// <summary>Opens the <see cref="AnimancerToolsWindow"/> showing the specified `tool`.</summary>
        public static void Open(Type toolType)
        {
        }

        /************************************************************************************************************************/
    }
}

#endif

