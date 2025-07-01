using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Extensions;
using UnityEditor;

namespace Awaken.TG.Editor.ToolbarTools.TopToolbars {
    public interface ITopToolbarElement {
        const int DefaultLabelWidth = 70;

        string Name { get; }
        bool CanChangeSide { get; }
        bool DefaultEnabled { get; }
        TopToolbarButtons.Side DefaultSide { get; }

        string MainKey => $"{nameof(ITopToolbarElement)}.{Name}";
        string ShowName {
            get => EditorPrefs.GetString($"{MainKey}.ShowName", Name);
            set => EditorPrefs.SetString($"{MainKey}.ShowName", value);
        }
        bool Enabled {
            get => EditorPrefs.GetBool($"{MainKey}.Enabled", DefaultEnabled);
            set => EditorPrefs.SetBool($"{MainKey}.Enabled", value);
        }
        int Order {
            get => EditorPrefs.GetInt($"{MainKey}.Order", 0);
            set => EditorPrefs.SetInt($"{MainKey}.Order", value);
        }
        TopToolbarButtons.Side Side {
            get => EditorPrefs.GetBool($"{MainKey}.RightSide", DefaultSide.IsRight()) ? TopToolbarButtons.Side.Right : TopToolbarButtons.Side.Left;
            set => EditorPrefs.SetBool($"{MainKey}.RightSide", value.IsRight());
        }

        IEnumerable<string> DefaultKeys => new[] {
            $"{MainKey}.ShowName",
            $"{MainKey}.Enabled",
            $"{MainKey}.Order",
            $"{MainKey}.RightSide",
        };
        IEnumerable<string> CustomKeys { get; }
        IEnumerable<string> PrefsKeys => DefaultKeys.Concat(CustomKeys);

        void SettingsGUI();
        bool HasSearchInterest(string[] searchParts) => Name.ContainsAny(searchParts);
        void AfterResetPrefsBasedValues();

        void OnGUI();
    }
}
