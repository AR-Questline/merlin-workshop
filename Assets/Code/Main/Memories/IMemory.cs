using Awaken.TG.MVC;

namespace Awaken.TG.Main.Memories {
    public interface IMemory {
        ContextualFacts Context();
        ContextualFacts Context(params IModel[] context);
        ContextualFacts Context(params string[] context);
        ContextualFacts Context(IModel context);
        ContextualFacts Context(string context);
        ContextualFacts Context(StringCollectionSelector contextSelector);
        string[] Contextify(params IModel[] context);
        StringCollectionSelector ContextSelector(params IModel[] context);
        StringCollectionSelector ContextSelector(IModel context);
    }
}