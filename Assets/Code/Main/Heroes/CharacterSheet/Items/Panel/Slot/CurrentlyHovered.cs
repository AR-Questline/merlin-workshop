using System;
using Awaken.TG.Main.Utility.UI;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class CurrentlyHovered<THovered> where THovered : class {
        THovered _hovered;
        public THovered Get => _hovered;
        
        public event Action<Change<THovered>> OnChange;
        
        public void OnStartHover(THovered hovered) {
            if (_hovered != hovered) {
                OnChange?.Invoke(new Change<THovered>(_hovered, hovered));
                _hovered = hovered;
                RewiredHelper.VibrateUIHover(VibrationStrength.VeryLow, VibrationDuration.VeryShort);
            }
        }

        public void OnStopHover(THovered hovered) {
            if (_hovered == hovered) {
                OnChange?.Invoke(new Change<THovered>(_hovered, null));
                _hovered = null;
            }
        }
    }
}