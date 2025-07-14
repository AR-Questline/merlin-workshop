using System.Collections.Generic;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.Utility.Graphics {
    public class LightsDebugger : UGUIWindowDisplay<LightsDebugger> {
        ImguiTable<Light> _table = new ImguiTable<Light>();

        List<Light> _allLights = new List<Light>();

        protected override bool WithSearch => false;

        protected override void Initialize() {
            base.Initialize();
            _table = new ImguiTable<Light>(
                SearchPrediction,
                ImguiTableUtils.NameColumn<Light>(),
                ImguiTableUtils.EnabledColumn<Light>(),
                ImguiTableUtils.ActiveColumn<Light>(),
                ImguiTable<Light>.ColumnDefinition.CreateNumeric("Intensity",  Width.Fixed(96), ImguiTableUtils.FloatDrawer, static l => l.intensity),
                ImguiTable<Light>.ColumnDefinition.CreateNumeric("Range",  Width.Fixed(96), ImguiTableUtils.FloatDrawer, static l => l.range),
                ImguiTable<Light>.ColumnDefinition.CreateNumeric("Distance to center",  Width.Fixed(128), ImguiTableUtils.FloatDrawer, DistanceToLightCenter),
                ImguiTable<Light>.ColumnDefinition.CreateNumeric("Distance to radius",  Width.Fixed(128), ImguiTableUtils.FloatDrawer, DistanceToLightRadius),
                ImguiTable<Light>.ColumnDefinition.Create("Shadows", Width.Fixed(96), ShadowsDrawer, static l => l.shadows != LightShadows.None ? 1 : 0, ImguiTableUtils.FloatDrawer, static l => l.shadows != LightShadows.None ? 1 : 0),
                ImguiTable<Light>.ColumnDefinition.CreateNumeric("Shadows fade",  Width.Fixed(96), ImguiTableUtils.FloatDrawer, ShadowsFadeDistance),
                ImguiTable<Light>.ColumnDefinition.Create("Volumetric",  Width.Fixed(96), VolumetricDrawer, static l => VolumetricEnabled(l) ? 1 : 0, ImguiTableUtils.FloatDrawer, VolumetricEnabled),
                ImguiTable<Light>.ColumnDefinition.Create("Type",  Width.Fixed(96), TypeDrawer, static l => l.type.ToString())
#if UNITY_EDITOR
                ,
                ImguiTableUtils.PingColumn<Light>()
#endif
                );

            RefreshLights();
        }

        protected override void Shutdown() {
            _allLights.Clear();
            _table.Dispose();
        }

        protected override void DrawWindow() {
            if (GUILayout.Button("Refresh lights")) {
                RefreshLights();
            }

            if (_table.Draw(_allLights, Position.height, Scroll.y, Position.width)) {
                _allLights.Sort(_table.Sorter);
            }
        }

        void RefreshLights() {
            _allLights.Clear();
            _allLights.AddRange(FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            _allLights.Sort(_table.Sorter);
        }

        static bool SearchPrediction(Light light, SearchPattern searchContext) {
            return searchContext.IsEmpty ||
                   searchContext.HasSearchInterest(light.name) ||
                   searchContext.HasSearchInterest(light.type.ToString()) ||
                   (searchContext.HasExactSearch("with_shadows") && light.shadows != LightShadows.None) ||
                   (searchContext.HasExactSearch("without_shadows") && light.shadows == LightShadows.None) ||
                   (searchContext.HasExactSearch("enabled") && light.enabled) ||
                   (searchContext.HasExactSearch("active") && light.gameObject.activeInHierarchy);
        }

        void ShadowsDrawer(in Rect rect, Light light) {
            var shadows = light.shadows;
            var isEnable = shadows != LightShadows.None;
            var shouldEnable = GUI.Toggle(rect, isEnable, isEnable ? "Enabled" : "Disabled");
            if (isEnable != shouldEnable) {
                if (shouldEnable) {
                    light.shadows = LightShadows.Soft;
                } else {
                    light.shadows = LightShadows.None;
                }
            }
        }

        void VolumetricDrawer(in Rect rect, Light light) {
            var hdData = light.GetComponent<HDAdditionalLightData>();
            var isEnable = hdData.affectsVolumetric;
            var shouldEnable = GUI.Toggle(rect, isEnable, isEnable ? "Enabled" : "Disabled");
            if (isEnable != shouldEnable) {
                hdData.affectsVolumetric = shouldEnable;
            }
        }

        float ShadowsFadeDistance(Light light) {
            if (light.shadows == LightShadows.None) {
                return 0;
            }
            var hdData = light.GetComponent<HDAdditionalLightData>();
            return hdData.shadowFadeDistance;
        }

        void TypeDrawer(in Rect rect, Light light) {
            GUI.Label(rect, light.type.ToString());
        }

        float DistanceToLightCenter(Light light) {
            var mainCamera = Camera.main;
            if (!mainCamera) {
                return 0;
            }
            return math.distance(mainCamera.transform.position, light.transform.position);
        }

        float DistanceToLightRadius(Light light) {
            var mainCamera = Camera.main;
            if (!mainCamera) {
                return 0;
            }
            return math.distance(mainCamera.transform.position, light.transform.position) - light.dilatedRange;
        }

        static bool VolumetricEnabled(Light light) {
            var hdData = light.GetComponent<HDAdditionalLightData>();
            return hdData.affectsVolumetric;
        }

        [StaticMarvinButton(state: nameof(IsDebugWindowShown))]
        static void ShowLightsWindow() {
            LightsDebugger.Toggle(new UGUIWindowUtils.WindowPositioning(UGUIWindowUtils.WindowPosition.TopLeft, 0.9f, 0.7f));
        }

        static bool IsDebugWindowShown() => LightsDebugger.IsShown;
    }
}
