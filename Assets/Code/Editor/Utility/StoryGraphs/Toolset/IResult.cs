using System.Collections.Generic;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    public interface IResult<T> where T : IResultEntry {
        void Feed(IEnumerable<T> entries);
        void Feed(T entry);
        List<T> GatherResults();
        void Clear();
    }
}