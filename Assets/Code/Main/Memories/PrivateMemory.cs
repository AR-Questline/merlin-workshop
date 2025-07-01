using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;

namespace Awaken.TG.Main.Memories {
    /// <summary>
    /// Instantiable memory used in situations where we need temporary memory (like combat memory).
    /// </summary>
    public partial class PrivateMemory : Element<Model>, IMemory {
        public override ushort TypeForSerialization => SavedModels.PrivateMemory;

        // === Properties and fields

        [Saved] Memory Memory { get; set; } = new Memory();

        // === Initialization

        protected override void OnInitialize() {
        }

        protected override void OnRestore() {
            Memory.Deserialize();
        }

        // === Public API

        public ContextualFacts Context() => Memory.Context();
        public ContextualFacts Context(params IModel[] context) => Memory.Context(context);
        public ContextualFacts Context(params string[] context) => Memory.Context(context);
        public string[] Contextify(params IModel[] context) => Memory.Contextify(context);

        // === Serialization callbacks

        protected override bool OnSave() {
            Memory.PrepareForSerialization();
            return true;
        }
    }
}