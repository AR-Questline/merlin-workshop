namespace Awaken.TG.MVC.UI {
    public interface IRecyclableViewsManager {
        void AddElement(IWithRecyclableView model, RetargetableView prefab);
        void FocusTarget(IWithRecyclableView target);
    }
}
