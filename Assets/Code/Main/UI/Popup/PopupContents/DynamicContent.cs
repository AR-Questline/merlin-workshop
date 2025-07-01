using System;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.Popup.PopupContents {
    public class DynamicContent {
        public Element Element { get; }
        public Type ViewContentType { get; }
        
        public DynamicContent(Element element, Type viewContentType) {
            Element = element;
            ViewContentType = viewContentType;
        }
    }
}