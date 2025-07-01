using System.Collections.Generic;

namespace Awaken.TG.Main.Settings.Graphics {
    public interface IGraphicSetting : ISetting {
        void SetValueForPreset(Preset preset);
        /// <summary>
        /// When player changes settings manually and accidentally all settings return in this enumerable that one single preset fits them all,
        /// we will show this preset as active. 
        /// </summary>
        IEnumerable<Preset> MatchingPresets { get; }
    }
}