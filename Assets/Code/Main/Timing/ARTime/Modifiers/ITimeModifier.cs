using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Timing.ARTime.Modifiers {
    public interface ITimeModifier : IElement<TimeDependent> {
        int Order { get; }
        string SourceID { get; }
        float Modify(float timeScale);
        void Apply();
        void Remove();
    }
}