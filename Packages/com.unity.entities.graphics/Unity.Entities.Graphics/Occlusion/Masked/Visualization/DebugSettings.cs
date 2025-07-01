#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)
// This class contains the debug settings exposed to the rendering debugger window
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Rendering.Occlusion.Masked.Visualization
{
    enum DebugRenderMode
    {
        None = 0,
        Depth = 1,
        Test = 2,
        Mesh = 3,
        Bounds = 4,
        Inverted = 5
    }

    class DebugSettings : IDebugData
    {
        public bool freezeOcclusion;
        public DebugRenderMode debugRenderMode;

        GUIContent[] m_ViewNames;
        ulong[] m_ViewIDs;
        int[] m_ViewIndices;
        int m_SelectedViewIndex;
        DebugUI.Widget[] m_Widgets;

        readonly string k_PanelName = "Culling";

        void Reset()
        {
            freezeOcclusion = false;
            debugRenderMode = DebugRenderMode.None;
            m_ViewNames = new GUIContent[]{new GUIContent("None")};
            m_ViewIDs = new ulong[]{0};
            m_ViewIndices = new int[]{0};
            m_SelectedViewIndex = 0;
        }

        Action IDebugData.GetReset() => Reset;

        public DebugSettings()
        {
            Reset();
        }

        public ulong? GetPinnedViewID()
        {
            return (m_SelectedViewIndex == 0) ? null : m_ViewIDs[m_SelectedViewIndex];
        }

        public void Register()
        {
#if PLATFORM_ANDROID
            // FK: No support for this feature on ARM platform with 32Bit since Neon Intrinsics aren't supported
            // Yury: Android is the only 32-bit Arm platform we support
            bool is32Bit = System.IntPtr.Size == 4;
            if (is32Bit)
            {
                return;
            }
#endif
            var widgetList = new List<DebugUI.Widget>();
            widgetList.Add(new DebugUI.Container
            {
                displayName = "Occlusion Culling",
                children =
                {
                    new DebugUI.BoolField
                    {
                        nameAndTooltip = new()
                        {
                            name = "Freeze Occlusion",
                            tooltip = "Enable to pause updating the occlusion while freely moving the camera."
                        },
                        getter = () => freezeOcclusion,
                        setter = value => { freezeOcclusion = value; },
                    },
                    new DebugUI.EnumField
                    {
                        nameAndTooltip = new()
                        {
                            name = "Pinned View",
                            tooltip =
                                "Use the drop-down to pin a view. All scene views and game views will cull objects from the pinned view's perspective."
                        },
                        getter = () => m_SelectedViewIndex,
                        setter = value => { m_SelectedViewIndex = value; },
                        enumNames = m_ViewNames,
                        enumValues = m_ViewIndices,
                        getIndex = () => m_SelectedViewIndex,
                        setIndex = value => { m_SelectedViewIndex = value; }
                    },
                    new DebugUI.EnumField
                    {
                        nameAndTooltip = new()
                        {
                            name = "Debug Mode",
                            tooltip =
                                "Use the drop-down to select a rendering mode to display as an overlay on the screen."
                        },
                        getter = () => (int)debugRenderMode,
                        setter = value => { debugRenderMode = (DebugRenderMode)value; },
                        getIndex = () => (int)debugRenderMode,
                        setIndex = value => { debugRenderMode = (DebugRenderMode)value; },
                        autoEnum = typeof(DebugRenderMode),
                    }
                }
            });

            var panel = DebugManager.instance.GetPanel(k_PanelName, true);
            m_Widgets = widgetList.ToArray();
            panel.children.Add(m_Widgets);

            DebugManager.instance.RegisterData(this);
        }

        public void RefreshViews(Dictionary<ulong, BufferGroup> bufferGroups)
        {
#if UNITY_EDITOR
            var ids = new List<ulong>(bufferGroups.Count);
            var names = new List<GUIContent>();
            ids.Add(0);
            names.Add(new GUIContent("None"));

            foreach (var pair in bufferGroups)
            {
                var instanceID = (int)(pair.Key & 0xffffffff);
                var splitIndex = (int)(pair.Key >> 32);
                var viewType = pair.Value.ViewType;
                var obj = (Behaviour) EditorUtility.InstanceIDToObject(instanceID);
                if (!obj || !obj.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var label = $"{obj.name}";
                if (viewType != BatchCullingViewType.Camera)
                {
                    label += $", Split {splitIndex}";
                }

                names.Add(new GUIContent(label));
                ids.Add(pair.Key);
            }

            m_SelectedViewIndex = 0;
            m_ViewNames = names.ToArray();
            m_ViewIndices = Enumerable.Range(0, m_ViewNames.Count()).ToArray();
            m_ViewIDs = ids.ToArray();

            Unregister();
            Register();
#endif
        }

        public void Unregister()
        {
            var panel = DebugManager.instance.GetPanel(k_PanelName);
            panel?.children.Remove(m_Widgets);
        }
    }
}

#endif // ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)
