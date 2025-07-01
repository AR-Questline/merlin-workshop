using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.Memories {
    /// <summary>
    /// Stores contextual facts about given models. F.E that Hero X visited Location Y.
    /// </summary>
    public partial class GameplayMemory : SerializedService, IMemory, IDomainBoundService {
        public override ushort TypeForSerialization => SavedServices.GameplayMemory;
        public Domain Domain => Domain.Gameplay;

        // === Properties and fields

        [Saved] Memory Memory { get; set; } = new Memory();

        // === Public API
        [UnityEngine.Scripting.Preserve] public IEnumerable<ContextualFacts> AllContexts => Memory.All();
        public IEnumerable<ContextualFacts> FilteredContextsBy(string partialSearch) => Memory.FilteredByPartial(partialSearch);
        
        public ContextualFacts Context() => Memory.Context();
        public ContextualFacts Context(params IModel[] context) => Memory.Context(context);
        public ContextualFacts Context(params string[] context) => Memory.Context(context);
        public string[] Contextify(params IModel[] context) => Memory.Contextify(context);
        
        // === Init
        public void Init() {
            World.EventSystem.ListenTo(EventSelector.AnySource, Model.Events.BeforeDiscarded, this, OnModelDiscarded);
        }

        // === Serialization callbacks
        public override void OnBeforeSerialize() {
            Memory.PrepareForSerialization();
        }

        public override void OnAfterDeserialize() {
            Memory.Deserialize();
        }

        // === Event callback
        void OnModelDiscarded(IModel model) {
            foreach (var facts in Memory.All()) {
                facts.OnModelDiscarded(model);
            }
        }

        public bool RemoveOnDomainChange() {
            return true;
        }
    }
}