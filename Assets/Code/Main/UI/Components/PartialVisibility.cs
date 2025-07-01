using System;

namespace Awaken.TG.Main.UI.Components {
    public struct PartialVisibility {
        public bool IsVisible => InternalVisibility && MiddleVisibility && ExternalVisibility;
        public event Action<bool> OnVisibilityChanged;
        
        bool InternalVisibility {
            get => _internalVisibility;
            set {
                if (_internalVisibility == value) return;
                _internalVisibility = value;
                OnVisibilityChanged?.Invoke(IsVisible);
            }
        }
        
        bool MiddleVisibility {
            get => _middleVisibility;
            set {
                if (_middleVisibility == value) return;
                _middleVisibility = value;
                OnVisibilityChanged?.Invoke(IsVisible);
            }
        }
        
        bool ExternalVisibility {
            get => _externalVisibility;
            set {
                if (_externalVisibility == value) return;
                _externalVisibility = value;
                OnVisibilityChanged?.Invoke(IsVisible);
            }
        }
        
        bool _internalVisibility;
        bool _middleVisibility;
        bool _externalVisibility;

        public PartialVisibility(bool internalVisibility, bool middleVisibility, bool externalVisibility) {
            _internalVisibility = internalVisibility;
            _middleVisibility = middleVisibility;
            _externalVisibility = externalVisibility;
            OnVisibilityChanged = null;
        }

        public void SetInternal(bool active) => InternalVisibility = active;
        public void SetMiddle(bool active) => MiddleVisibility = active;
        public void SetExternal(bool active) => ExternalVisibility = active;

        public static implicit operator bool(PartialVisibility visibility) => visibility.IsVisible;

        public static PartialVisibility Visible = new(true, true, true);
    }
}