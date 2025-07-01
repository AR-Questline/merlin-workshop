#if UNITY_EDITOR && ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using System.Collections.Generic;
using System;
using UnityEngine.Rendering;
using Unity.Rendering.Occlusion.Masked;
using System.Linq;

namespace Unity.Rendering.Occlusion
{
    class OcclusionBrowseWindow : EditorWindow
    {
        public static bool IsVisible;
        private static OcclusionBrowseWindow Window;

        OcclusionBrowserView occlusionBrowserView;
        int selectedIndex = 0;
        List<KeyValuePair<Behaviour, List<BufferGroup>>> ViewSlices = new List<KeyValuePair<Behaviour, List<BufferGroup>>>();

        [MenuItem("Occlusion/Browse")]
        public static void ShowMenuItem()
        {
            OcclusionBrowseWindow wnd = GetWindow<OcclusionBrowseWindow>();
            wnd.titleContent = new GUIContent("Occlusion Browser");
        }

        internal StyleSheet StyleSheet
        {
            get
            {
                return AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.entities.graphics/Unity.Entities.Graphics/Occlusion/OcclusionBrowseWindow.uss");
            }
        }

        public void OnEnable()
        {
            autoRepaintOnSceneChange = true;
            rootVisualElement.Clear();
            CreateGUI();
        }

        public void OnHierarchyChange()
        {
            rootVisualElement.Clear();
            CreateGUI();

            if (selectedIndex < ViewSlices.Count)
            {
                var (obj, bufferGroups) = ViewSlices[selectedIndex];
                OnSelectionChanged(obj, bufferGroups);
            }
        }

        private void OnBecameVisible()
        {
            IsVisible = true;
            Window = GetWindow<OcclusionBrowseWindow>();
        }

        private void OnBecameInvisible()
        {
            IsVisible = false;
            Window = null;
        }

        public static void Refresh()
        {
            if (IsVisible && Window != null)
            {
                Window.OnHierarchyChange();
            }
        }

        public void CreateGUI()
        {
            var root = this.rootVisualElement;
            root.Clear();

            if (World.DefaultGameObjectInjectionWorld == null)
            {
                return;
            }
            var entitiesGraphicsSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();

            if (entitiesGraphicsSystem.OcclusionCulling != null && entitiesGraphicsSystem.OcclusionCulling.IsEnabled)
            {
                var bufferGroups = entitiesGraphicsSystem.OcclusionCulling.BufferGroups;
                var slices = new Dictionary<Behaviour, List<BufferGroup>>();

                foreach (var pair in bufferGroups)
                {
                    var id = pair.Key & 0xffffffff;
                    var slice = (int)(pair.Key >> 32);

                    var obj = (Behaviour)EditorUtility.InstanceIDToObject((int)id);
                    if (obj == null)
                    {
                        // can happen if we're updating while deleting a view.
                        selectedIndex = 0;
                        continue;
                    }

                    if (obj is Camera camera && camera.cameraType == CameraType.SceneView)
                    {
                        // always include the scene camera
                    }
                    else
                    {
                        var viewSettings = obj.gameObject.GetComponent<OcclusionView>();
                        if (viewSettings == null
                            || !viewSettings.OcclusionEnabled
                            || !viewSettings.enabled
                            || !viewSettings.gameObject.activeInHierarchy)
                        {
                            // otherwise, ignore views without settings and views that disable occlusion
                            continue;
                        }
                    }

                    if (!slices.TryGetValue(obj, out var list))
                    {
                        list = new List<BufferGroup>();
                    }
                    list.Add(pair.Value);
                    slices[obj] = list;
                }

                ViewSlices = slices.ToList();
            }
            else
            {
                ViewSlices = new List<KeyValuePair<Behaviour, List<BufferGroup>>>();
            }

            occlusionBrowserView = new OcclusionBrowserView();

            selectedIndex = Math.Max(0, Math.Min(selectedIndex, ViewSlices.Count - 1));
            occlusionBrowserView.ViewList.itemsSource = ViewSlices.ConvertAll(pair => pair.Key.name);

            occlusionBrowserView.ViewList.makeItem =
                () => new Label();
            occlusionBrowserView.ViewList.bindItem =
                (e, i) => (e as Label).text = ViewSlices[i].Key.name;
            occlusionBrowserView.ViewList.selectedIndicesChanged +=
                idx =>
                {
                    selectedIndex = idx.First();
                    if (selectedIndex < ViewSlices.Count)
                    {
                        // make sure we don't throw when the scene changes
                        var (obj, bufferGroups) = ViewSlices[selectedIndex];
                        OnSelectionChanged(obj, bufferGroups);
                    }
                };

            occlusionBrowserView.ViewList.selectedIndex = selectedIndex;
            root.Add(occlusionBrowserView);
            occlusionBrowserView.StretchToParentSize();

        }

        void OnSelectionChanged(Behaviour obj, List<BufferGroup> groups)
        {
            BufferGroup bufferGroup = groups.First();

            var tex = bufferGroup.GetVisualizationTexture();
            occlusionBrowserView.CurrentSliceTexture = tex;

            var infoText = $"<b>Type:</b> {bufferGroup.ViewType}    "
                         + $"<b>Size:</b> {tex?.width}x{tex?.height}    "
                         + $"<b>Slice count:</b> {groups.Count}";
            occlusionBrowserView.InfoText.text = infoText;
            occlusionBrowserView.SetBufferGroups(groups);
            occlusionBrowserView.SetupGUI();

            Repaint();
        }
    }
}
#endif
