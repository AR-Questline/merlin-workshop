using System.Linq;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Video.Subtitles {
    public abstract partial class SubtitlesBase<T> : Element<T>, ISubtitleBase where T : Model {
        int? _currentRecordIndex;

        public SubtitlesData.Record CurrentRecord {
            get {
                if (Records.IsNullOrEmpty()) {
                    return null;
                }
                Update();
                return _currentRecordIndex.HasValue ? Records[_currentRecordIndex.Value] : null;
            }
        }
        public abstract Transform SubtitlesHost { get; }
        protected abstract SubtitlesData.Record[] Records { get; }
        protected abstract float Time { get; }
        
        void Update() {
            if (Records == null || Records.Length == 0) return;
            var time = Time;
            var locale = LocalizationHelper.SelectedLocale;
            var current = Records.FirstOrDefault(r => r.Time(locale).Contains(time));
            _currentRecordIndex = current != null ? Records.IndexOf(current) : null;
        }
    }
}