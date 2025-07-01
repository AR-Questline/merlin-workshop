using System;
using System.Globalization;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.Utility.UI {
    public static class ImguiTableUtils {
        public const float ToolbarHeight = 24;
        public const float HeaderHeight = 34;
        public const float FooterHeight = 34;
        public const float CellHeight = 30;

        public static readonly Color HeaderColor = new Color(0.8f, 0.6f, 0.2f);
        public static readonly Color EvenColor = new Color(0.9f, 0.9f, 0.6f);
        public static readonly Color OddColor = new Color(0.7f, 0.7f, 1f);

        public static readonly Color EvenRowColor = new Color(0.1f, 0.1f, 0.1f, 0.55f);
        public static readonly Color OddRowColor = new Color(0.25f, 0.0f, 0.0f, 0.55f);

        public static ImguiTable<T>.ColumnDefinition EnabledColumn<T>(int width = 96) where T : Behaviour {
            return ImguiTable<T>.ColumnDefinition.Create("Enabled", width, EnabledDrawer, static l => l.enabled ? 1 : 0, FloatDrawer, static l => l.enabled);
        }

        public static void EnabledDrawer<T>(in Rect rect, T behaviour) where T : Behaviour {
            var isEnable = behaviour.enabled;
            var shouldEnable = GUI.Toggle(rect, behaviour.enabled, isEnable ? "Enabled" : "Disabled");
            if (isEnable != shouldEnable) {
                behaviour.enabled = shouldEnable;
            }
        }

        public static ImguiTable<T>.ColumnDefinition ActiveColumn<T>(int width = 96) where T : Behaviour {
            return ImguiTable<T>.ColumnDefinition.Create("Active", width, ActiveDrawer, static l => l.gameObject.activeInHierarchy ? 1 : 0, FloatDrawer, static l => l.gameObject.activeInHierarchy);
        }

        public static void ActiveDrawer<T>(in Rect rect, T behaviour) where T : Component {
            var isActive = behaviour.gameObject.activeInHierarchy;

            Color oldColor = GUI.color;
            GUI.color *= isActive ? new Color(1f, 1f, 1f) : new Color(0.5f, 0.5f, 0.5f);

            GUI.Label(rect, isActive ? "Active" : "Inactive");

            GUI.color = oldColor;
        }

        public static ImguiTable<T>.ColumnDefinition NameColumn<T>(int width = 192) where T : UnityEngine.Object {
            return ImguiTable<T>.ColumnDefinition.Create("Name", width, NameDrawer, static l => l.name);
        }

        public static void NameDrawer<T>(in Rect rect, T element) where T : UnityEngine.Object {
            GUI.Label(rect, element.name);
        }

        public static void FloatDrawer(in Rect rect, float value) {
            GUI.Label(rect, value.ToString(CultureInfo.InvariantCulture));
        }

        public static void FloatTwoDrawer(in Rect rect, float value) {
            GUI.Label(rect, value.ToString("F2", CultureInfo.InvariantCulture));
        }

        public static void MemoryDrawer(in Rect rect, float value) {
            GUI.Label(rect, M.HumanReadableBytes(value));
        }

#if UNITY_EDITOR
        public static ImguiTable<T>.ColumnDefinition PingColumn<T>(int width = 64) where T : UnityEngine.Object {
            return ImguiTable<T>.ColumnDefinition.Create("Ping", width, ImguiTableUtils.PingDrawer, static l => l.name);
        }

        public static void PingDrawer<T>(in Rect rect, T element) where T : UnityEngine.Object {
            if (GUI.Button(rect, "Ping")) {
                UnityEditor.Selection.activeObject = element;
                UnityEditor.EditorGUIUtility.PingObject(element);
            }
        }

        public static ImguiTable<T>.ColumnDefinition PingColumn<T, U>(Func<T, U> selector, int width = 64) where U : UnityEngine.Object {
            return ImguiTable<T>.ColumnDefinition.Create("Ping", width, PingDrawer(selector), e => selector(e).name);
        }

        public static ImguiTable<T>.Drawer<T> PingDrawer<T, U>(Func<T, U> selector) where U : UnityEngine.Object {
            return (in Rect rect, T element) => {
                if (GUI.Button(rect, "Ping")) {
                    var toSelect = selector(element);
                    UnityEditor.Selection.activeObject = toSelect;
                    UnityEditor.EditorGUIUtility.PingObject(toSelect);
                }
            };
        }
#endif
    }
}
