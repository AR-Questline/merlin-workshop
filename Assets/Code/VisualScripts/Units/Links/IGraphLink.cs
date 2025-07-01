using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Links {
    public interface IGraphLink : IUnit {
        string Label { get; }
    }
}