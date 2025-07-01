using System;

namespace Awaken.TG.Main.Utility.UI.Keys {
    public class ByControlScheme<TElement> {
        readonly TElement[] _elements = new TElement[ControlSchemes.Count];
        public TElement this[ControlScheme map] {
            get => _elements[(int)map];
            set => _elements[(int)map] = value;
        }

        public ByControlScheme<TNewElement> Transformed<TNewElement>(Func<TElement, TNewElement> transformation) {
            var mapping = new ByControlScheme<TNewElement>();
            for (int i = 0; i < ControlSchemes.Count; i++) {
                mapping._elements[i] = transformation(_elements[i]);
            }
            return mapping;
        }
    }
}