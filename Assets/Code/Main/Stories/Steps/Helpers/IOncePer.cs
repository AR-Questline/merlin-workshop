using Awaken.Utility.Times;

namespace Awaken.TG.Main.Stories.Steps.Helpers {
    public interface IOncePer {
        string SpanFlag { get; set; }
        TimeSpans Span { get; }
    }
}