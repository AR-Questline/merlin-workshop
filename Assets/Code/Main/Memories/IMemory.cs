using Awaken.TG.MVC;

namespace Awaken.TG.Main.Memories {
    public interface IMemory {
        ContextualFacts Context();
        ContextualFacts Context(params IModel[] context);
        ContextualFacts Context(params string[] context);
        string[] Contextify(params IModel[] context);
    }
}