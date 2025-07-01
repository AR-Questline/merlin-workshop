using Awaken.TG.Assets;

namespace Awaken.Tests.Performance.TestCases {
    public interface IPerformanceTestCase {
        public SceneReference Scene { get; }
        public string Name { get; }
        void Run();
        void Update(out bool ended, out bool capture);
    }
}