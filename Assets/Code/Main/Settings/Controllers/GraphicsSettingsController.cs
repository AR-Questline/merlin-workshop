using System;
using Awaken.TG.Main.Settings.Controllers.Switchers;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Controllers {
    public abstract class GraphicsSettingsController<T> : StartDependentView<T>, IGeneralGraphicsSceneView where T : Setting, IGraphicsIndexedSetting {
        [HideInInspector] 
        public Vector2Int qualityRange = new(0, 1);

        [InfoBox("$RangeText"), NonSerialized, ShowInInspector, LabelText("Active For Qualities")]
        [EnumToggleButtons, OnInspectorInit(nameof(EditorRefreshEnumValues)), OnValueChanged(nameof(EditorChangeQualityRange))]
        QualityFlags _editorQualityFlag;
        
        [SerializeReference] IControllerSwitcher[] switchers = { new EnabledSwitcher() };
        
        protected override void OnInitialize() {
            if (typeof(T) != typeof(GeneralGraphics)) {
                Target.ListenTo(Setting.Events.SettingRefresh, SettingChanged, this);
            }
            SettingChanged(Target);
        }

        public void SettingsRefreshed(GeneralGraphics graphicsSetting) {
            SettingChanged(graphicsSetting);
        }

        void SettingChanged(Setting setting) {
            T graphicsQualitySetting = (T)setting;
            bool shouldBeActive = graphicsQualitySetting.ActiveIndex >= qualityRange.x && graphicsQualitySetting.ActiveIndex <= qualityRange.y;
            foreach (var switcher in switchers) {
                switcher?.Refresh(shouldBeActive, gameObject);
            }
        }

        // === Editor
        // ReSharper disable once UnusedMember.Local
        protected abstract string SettingName { get; }
        
        string RangeText => $"Currently Active from {HumanName(qualityRange.x)} to {HumanName(qualityRange.y)} ({SettingName} setting).\n\n{PossibleControllers}";
        protected virtual string HumanName(int index) {
            return index switch {
                0 => "Low",
                1 => "Medium",
                2 => "High",
                _ => "INVALID",
            };
        }

        string PossibleControllers => $"You can use:\n  {nameof(QualityController)} for General Graphics quality\n" +
                                      $"  {nameof(VfxQualityController)} for VFX Quality\n";
        
        void EditorChangeQualityRange() {
            var min = math.tzcnt((uint)_editorQualityFlag);
            var max = 31-math.lzcnt((uint)_editorQualityFlag);
            qualityRange = new Vector2Int(min, max);
        }
        
        void EditorRefreshEnumValues() {
            var maskMin = unchecked(~((1 << qualityRange.x) - 1));
            var maskMax = unchecked((1 << qualityRange.y+1) - 1);
            _editorQualityFlag = (QualityFlags)(maskMin & maskMax);
        }

        [Flags]
        enum QualityFlags : byte {
            [UsedImplicitly, UnityEngine.Scripting.Preserve] Low = 1 << 0,
            [UsedImplicitly, UnityEngine.Scripting.Preserve] Medium = 1 << 1,
            [UsedImplicitly, UnityEngine.Scripting.Preserve] High = 1 << 2,
        }
    }
}
