using System;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.MVC;
using Awaken.Utility;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Utility.UI {
    /// <summary>
    /// Adjust rect transform size based on platform.
    /// Assume font size set in the editor is for PC platform, this is the default setting.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VCDeviceRectAdjuster : ViewComponent {
        [SerializeField] RectAdjusterTarget target = RectAdjusterTarget.All;
        [SerializeField] Vector2 consoleSizeFactor = new(1.4f, 1.4f);
        
        [SerializeField, ShowIf(nameof(IsLayoutGroup))]
        float consoleSizeSpacingFactor = 1f;
        [SerializeField, ShowIf(nameof(IsLayoutGroup))]
        float consoleSizePaddingFactor = 1f;

        RectTransform _rectTransform;
        LayoutElement _layoutElement;
        HorizontalOrVerticalLayoutGroup _layoutGroup;
        GridLayoutGroup _gridLayoutGroup;
        Vector3 _pcScale;
        Vector2 _pcSizeDelta;
        Vector2 _pcLayoutMinSize;
        Vector2 _pcLayoutPreferredSize;
        float _pcLayoutGroupSpacing;
        RectOffset _pcLayoutGroupPadding;
        RectOffset _consoleLayoutGroupPadding;
        Vector2 _pcGridLayoutGroupSpacing;
        Vector2 _pcGridLayoutGroupCellSize;
        RectOffset _pcGridLayoutGroupPadding;
        RectOffset _consoleGridLayoutGroupPadding;

        static bool s_debugConsolePlatform;
        bool IsConsole => PlatformUtils.IsConsole || PlatformUtils.IsSteamDeck || s_debugConsolePlatform || World.Any<ConsoleUISetting>()?.Enabled == true;
        bool IsLayoutGroup => target.HasFlag(RectAdjusterTarget.LayoutGroup) || target.HasFlag(RectAdjusterTarget.GridLayoutGroup);
        
        void Awake() {
            CacheValues();
            Execute();
        }
        
        protected override void OnAttach() {
            CacheValues();
            
            ModelUtils.DoForFirstModelOfType<ConsoleUISetting>(setting => {
                setting.ListenTo(Setting.Events.SettingChanged, Execute, this);
            }, this);
        }

        void CacheValues() {
            _rectTransform = GetComponent<RectTransform>();
            _pcSizeDelta = _rectTransform.sizeDelta;
            _pcScale = _rectTransform.localScale;
            
            if (TryGetComponent(out _layoutElement)) {
                _pcLayoutMinSize = new Vector2(_layoutElement.minWidth, _layoutElement.minHeight);
                _pcLayoutPreferredSize = new Vector2(_layoutElement.preferredWidth, _layoutElement.preferredHeight);
            }
            
            if (TryGetComponent(out _layoutGroup)) {
                _pcLayoutGroupSpacing = _layoutGroup.spacing;
                _pcLayoutGroupPadding = _layoutGroup.padding;
                _consoleLayoutGroupPadding = new RectOffset(
                    (int)(_pcLayoutGroupPadding.left * consoleSizePaddingFactor),
                    (int)(_pcLayoutGroupPadding.right * consoleSizePaddingFactor),
                    (int)(_pcLayoutGroupPadding.top * consoleSizePaddingFactor),
                    (int)(_pcLayoutGroupPadding.bottom * consoleSizePaddingFactor)
                );
            }
            
            if (TryGetComponent(out _gridLayoutGroup)) {
                _pcGridLayoutGroupSpacing = _gridLayoutGroup.spacing;
                _pcGridLayoutGroupPadding = _gridLayoutGroup.padding;
                _pcGridLayoutGroupCellSize = _gridLayoutGroup.cellSize;
                _consoleGridLayoutGroupPadding = new RectOffset(
                    (int)(_pcGridLayoutGroupPadding.left * consoleSizePaddingFactor),
                    (int)(_pcGridLayoutGroupPadding.right * consoleSizePaddingFactor),
                    (int)(_pcGridLayoutGroupPadding.top * consoleSizePaddingFactor),
                    (int)(_pcGridLayoutGroupPadding.bottom * consoleSizePaddingFactor)
                );
            }
        }

        void Execute() {
            if (target.HasFlag(RectAdjusterTarget.LocalScale)) {
                SetScale();
            }
            
            if (target.HasFlag(RectAdjusterTarget.SizeDelta)) {
                SetSizeDelta();
            }
            
            if (target.HasFlag(RectAdjusterTarget.LayoutElement) && _layoutElement != null) {
                SetLayoutElement();
            }
            
            if (target.HasFlag(RectAdjusterTarget.LayoutGroup) && _layoutGroup != null) {
                SetLayoutGroup();
            }
            
            if (target.HasFlag(RectAdjusterTarget.GridLayoutGroup) && _gridLayoutGroup != null) {
                SetGridLayoutGroup();
            }
        }
        
        void SetScale() {
            Vector3 consoleScale = new(_pcScale.x * consoleSizeFactor.x, _pcScale.y * consoleSizeFactor.y, _pcScale.z);
            _rectTransform.localScale = IsConsole ? consoleScale : _pcScale;
        }
        
        void SetSizeDelta() {
            _rectTransform.sizeDelta = IsConsole  ? _pcSizeDelta * consoleSizeFactor : _pcSizeDelta;
        }

        void SetLayoutElement() {
            var minSize = CalculateSize(_layoutElement.minWidth, _layoutElement.minHeight, _pcLayoutMinSize);
            var preferredSize = CalculateSize(_layoutElement.preferredWidth, _layoutElement.preferredHeight, _pcLayoutPreferredSize);
            
            _layoutElement.minWidth = minSize.x;
            _layoutElement.minHeight = minSize.y;
            _layoutElement.preferredWidth = preferredSize.x;
            _layoutElement.preferredHeight = preferredSize.y;
            return;

            Vector2 CalculateSize(float width, float height, Vector2 pcSize) {
                return new Vector2(IsConsole ? width * consoleSizeFactor.x : pcSize.x, IsConsole ? height * consoleSizeFactor.y : pcSize.y);
            }
        }
        
        void SetLayoutGroup() {
            _layoutGroup.spacing = IsConsole ? _layoutGroup.spacing * consoleSizeSpacingFactor : _pcLayoutGroupSpacing;
            _layoutGroup.padding = IsConsole ? _consoleLayoutGroupPadding : _pcLayoutGroupPadding;
        }

        void SetGridLayoutGroup() {
            _gridLayoutGroup.spacing = IsConsole ? _pcGridLayoutGroupSpacing * consoleSizeSpacingFactor : _pcGridLayoutGroupSpacing;
            _gridLayoutGroup.padding = IsConsole ? _consoleGridLayoutGroupPadding : _pcGridLayoutGroupPadding;
            _gridLayoutGroup.cellSize = IsConsole ? _pcGridLayoutGroupCellSize * consoleSizeFactor : _pcGridLayoutGroupCellSize;
        }
        
#if UNITY_EDITOR
        [FoldoutGroup("Debug"), SerializeField] bool showPCSizeDeltaGizmo;
        [FoldoutGroup("Debug"), SerializeField] bool showConsoleSizeDeltaGizmo;
        
        [FoldoutGroup("Debug"), Button]
        public static void DebugSetAll(bool isConsolePlatform) {
            s_debugConsolePlatform = isConsolePlatform;
            var result = FindObjectsByType<VCDeviceRectAdjuster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (var rectAdjuster in result) {
                if (rectAdjuster._rectTransform == null) {
                    rectAdjuster.CacheValues();
                }
                
                rectAdjuster.Execute();
            }
        }

        [FoldoutGroup("Debug")]
        void OnDrawGizmos() {
            if (PlatformUtils.IsPlaying == false) return;
            
            if (showPCSizeDeltaGizmo) {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(_rectTransform.position, _pcSizeDelta);
            }

            if (showConsoleSizeDeltaGizmo) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(_rectTransform.position, _pcSizeDelta * consoleSizeFactor);
            }
        }
#endif

        [Flags]
        enum RectAdjusterTarget : byte {
            [UnityEngine.Scripting.Preserve] None = 0,
            SizeDelta = 1,
            LayoutElement = 1 << 1,
            LayoutGroup = 1 << 2,
            LocalScale = 1 << 3,
            GridLayoutGroup = 1 << 4,
            All = SizeDelta | LayoutElement | LayoutGroup | GridLayoutGroup
        }
    }
}
