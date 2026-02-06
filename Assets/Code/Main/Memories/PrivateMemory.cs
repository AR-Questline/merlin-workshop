using Awaken.TG.MVC;

namespace Awaken.TG.Main.Memories {
    /// <summary>
    /// Instantiable memory used in situations where we need temporary memory (like combat memory).
    /// </summary>
    public partial class PrivateMemory : IMemory {
        // === Properties and fields

        Memory Memory { get; set; } = new Memory();

        // === Public API

        public ContextualFacts Context() => Memory.Context();
        public ContextualFacts Context(params IModel[] context) => Memory.Context(context);
        public ContextualFacts Context(params string[] context) => Memory.Context(context);
        public ContextualFacts Context(IModel context) => Memory.Context(context);
        public ContextualFacts Context(string context) => Memory.Context(context);
        public ContextualFacts Context(StringCollectionSelector contextSelector) => Memory.Context(contextSelector);
        public string[] Contextify(params IModel[] context) => Memory.Contextify(context);
        public StringCollectionSelector ContextSelector(params IModel[] context) => Memory.ContextSelector(context);
        public StringCollectionSelector ContextSelector(IModel context) => Memory.ContextSelector(context);
    }
}