using System;
using System.Collections.Generic;
using System.Linq;

namespace Awaken.Utility.Editor.SearchableMenu {
    public class Entry {
        public string Path { get; set; }
        public string Name { get; private init; }
        public Action Action { get; private init; }
        public List<Entry> Children { get; } = new();

        public bool IsLeaf => Children == null || !Children.Any();

        public void AddEntry(string name, Action action) {
            var split = name.Split("/", StringSplitOptions.RemoveEmptyEntries);
            var firstSplitWord = split.First();
            if (firstSplitWord != null) {
                var matchChildren = Children?.FirstOrDefault(p => p.Name == firstSplitWord);
                var rest = split.Length > 1 ? name[firstSplitWord.Length..].Remove(0, 1) : string.Empty;

                if (matchChildren != null) {
                    if (split.Length > 1) {
                        matchChildren.AddEntry(rest, action);
                    }
                } else {
                    var newEntry = new Entry() {
                        Name = firstSplitWord,
                        Action = action
                    };

                    if (split.Length > 1) {
                        newEntry.AddEntry(rest, action);
                    }

                    Children?.Add(newEntry);
                }
            }
        }
    }
}