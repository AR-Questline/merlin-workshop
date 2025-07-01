using System;
using Awaken.TG.Editor.Assets;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Data {
    [Serializable]
    public class DataViewPreferences {
        public static DataViewPreferences Instance => TGEditorPreferences.Instance.dataView;
        
        public Color headerColor = new(0.16f, 0.16f, 0.16f, 1);
        
        public Background<ZebraStripes> bgBulk = new () {
            background = new() {
                odd = ColorUtils.HexToColor("3B57A4FF"),
                even = ColorUtils.HexToColor("30447BFF"),
            },
            fieldColor = new() {
                odd = ColorUtils.HexToColor("FFFFFFFF"),
                even = ColorUtils.HexToColor("CCCCCCFF"),
            }
        };
        public Background<ZebraStripes> bgNormal = new () {
            background = new() {
                odd = ColorUtils.HexToColor("404040FF"),
                even = ColorUtils.HexToColor("333333FF"),
            },
            fieldColor = new() {
                odd = ColorUtils.HexToColor("FFFFFFFF"),
                even = ColorUtils.HexToColor("808080FF"),
            }
        };
        public Background<Color> bgHover = new () {
            background = ColorUtils.HexToColor("216A1BFF"),
            fieldColor = ColorUtils.HexToColor("E6E6E6FF"),
        };
        public float padding = 1;
        public float heightScale = 1.4f;
        
        public TextAnchor numberAlignment = TextAnchor.MiddleLeft;
        public TextAnchor enumAlignment = TextAnchor.MiddleLeft;
        public TextAnchor textAlignment = TextAnchor.MiddleLeft;

        public Color PopupBackground = new(0.2f, 0.2f, 0.2f, 1);
        public Color NotActivePopupButtonBackground = new(0.6f, 0.6f, 0.6f, 1);

        [Serializable]
        public struct ZebraStripes {
            public Color odd;
            public Color even;
            
            public Color this[int i] => i % 2 == 0 ? odd : even;
        }

        [Serializable]
        public struct Background<T> {
            public T background;
            public T fieldColor;
        }
    }
}