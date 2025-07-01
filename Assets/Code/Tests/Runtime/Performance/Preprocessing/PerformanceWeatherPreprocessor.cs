using System.Collections.Generic;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Graphics;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;

namespace Awaken.Tests.Performance.Preprocessing {
    public class PerformanceWeatherPreprocessor : IPerformancePreprocessor {
        readonly Variant[] _variants = {
            new("clear-day", 13, 0),
            new("clear-night", 1, 0),
        };

        public string Name => "weather";
        public IReadOnlyList<IPerformancePreprocessorVariant> Variants => _variants;

        class Variant : IPerformancePreprocessorVariant {
            public string Name { get; }
            readonly int _hours;
            readonly int _minutes;

            public Variant(string name, int hours, int minutes) {
                Name = name;
                _hours = hours;
                _minutes = minutes;
            }
            
            public void Process() {
                MarvinMode.SetPerformanceWeather(_hours, _minutes);
            }
        }
    }
}