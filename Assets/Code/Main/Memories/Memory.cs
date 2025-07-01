using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Memories {
    /// <summary>
    /// Serializable object that holds information grouped by their context. 
    /// </summary>
    public partial class Memory : IMemory {
        public ushort TypeForSerialization => SavedTypes.Memory;

        // === Properties and fields
        readonly Dictionary<StringCollectionSelector, ContextualFacts> _facts = new Dictionary<StringCollectionSelector, ContextualFacts>();
        [Saved] List<ContextualFacts> _serializedList = new List<ContextualFacts>();
        
        // === Public logic

        public IEnumerable<ContextualFacts> All() => _facts.Values;
        public IEnumerable<ContextualFacts> FilteredByPartial(string partialSearch) => _facts.Where(kvp => kvp.Key.ContainsPartial(partialSearch))
                                                                                         .Select(kvp => kvp.Value);
        
        public ContextualFacts Context() => StringContext();
        public ContextualFacts Context(params IModel[] context) => StringContext(Contextify(context));
        public ContextualFacts Context(params string[] context) => StringContext(context);
        [UnityEngine.Scripting.Preserve] public ContextualFacts Context(StringCollectionSelector context) => GetOrCreateContext(context);

        ContextualFacts StringContext(params string[] context) {
            StringCollectionSelector selector = context is { Length: > 0 } ? new StringCollectionSelector(context) : StringCollectionSelector.Empty;
            return GetOrCreateContext(selector);
        }

        ContextualFacts GetOrCreateContext(StringCollectionSelector selector) {
            if (!_facts.TryGetValue(selector, out var facts)) {
                facts = new ContextualFacts(selector);
                _facts[selector] = facts;
            }
            return facts;
        }

        // === Helpers

        public string[] Contextify(params IModel[] context) {
            return context.Where(c => c != null).Select(c => c.ContextID).ToArray();
        }

        // === Serialization
        public void PrepareForSerialization() {
            _serializedList = _facts.Values.Where(v => !v.IsEmpty).ToList();
        }

        public void Deserialize() {
            foreach (ContextualFacts fact in _serializedList) {
                if (_facts.TryGetValue(fact.Selector, out ContextualFacts existingFasts)) {
                    existingFasts.Merge(fact);
                } else {
                    _facts.Add(fact.Selector, fact);
                }
            }
            _serializedList = new List<ContextualFacts>();
        }
    }
}