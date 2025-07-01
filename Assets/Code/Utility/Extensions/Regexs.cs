using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Awaken.Utility.Extensions {
    public static class RegexExtensions {
        [UnityEngine.Scripting.Preserve]
        public static IEnumerable<Match> SuccessMatches(this Regex regex, string text) => regex.Matches(text).Cast<Match>().Where(m => m.Success);
    }
}