using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.UI.EmptyContent {
    public interface IEmptyInfo : IView {
        CanvasGroup[] ContentGroups { get; }
        VCEmptyInfo EmptyInfoView { get; }
        
        void PrepareEmptyInfo();
        
        public static class Events {
            public static readonly Event<IModel, bool> OnEmptyStateChanged = new(nameof(OnEmptyStateChanged));
        }
    }
}
