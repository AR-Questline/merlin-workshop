namespace Awaken.Utility.UI {
    public readonly struct Width {
        readonly int _fixedWidth;
        readonly float _flexibleWidth;

        public static Width Fixed(int width) {
            return width;
        }

        public static Width Flexible(float width) {
            return width;
        }

        Width(int fixedWidth, float flexibleWidth) {
            _fixedWidth = fixedWidth;
            _flexibleWidth = flexibleWidth;
        }

        public float GetWidth(float totalWidth) {
            return _fixedWidth > 0 ? _fixedWidth : _flexibleWidth * totalWidth;
        }

        public static implicit operator Width(int fixedWidth) {
            if (fixedWidth < 0) {
                throw new System.ArgumentOutOfRangeException(nameof(fixedWidth), "Fixed width cannot be negative.");
            }
            return new Width(fixedWidth, 0);
        }

        public static implicit operator Width(float flexibleWidth) {
            if (flexibleWidth is <= 0 or > 1) {
                throw new System.ArgumentOutOfRangeException(nameof(flexibleWidth), "Flexible width must be in the range (0, 1).");
            }
            return new Width(0, flexibleWidth);
        }
    }
}