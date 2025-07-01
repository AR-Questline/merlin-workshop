using System;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Localization;
using Awaken.Utility;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization;

namespace Awaken.TG.Main.Utility.Video.Subtitles {
    public class SubtitlesData : ScriptableObject {
        public TextAsset source;
        [TableList(IsReadOnly = true)] public Record[] records = Array.Empty<Record>();
        
        [Serializable]
        public class Record {
            [VerticalGroup("Text"), LocStringCategory(Category.VideoSubtitles)] 
            public LocString text;

            [VerticalGroup("Time"), TableList(IsReadOnly = true, AlwaysExpanded = true, HideToolbar = true)] 
            public TimeOverride[] times;
            
            public Record(float from, float to) {
                times = new []{ new TimeOverride { locale = null, time = new FloatRange(from, to) } };
                text = null;
            }
            
            public FloatRange Time(Locale locale) {
                for (int i = 0; i < times.Length; i++) {
                    if (times[i].locale.Value == locale) {
                        return times[i].time;
                    }
                }
                return times[0].time;
            }

            public void AddTimeOverride(Locale locale) {
                for (int i = 0; i < times.Length; i++) {
                    if (times[i].locale.Value == locale) {
                        return;
                    }
                }
                Array.Resize(ref times, times.Length + 1);
                times[^1] = new TimeOverride { locale = locale, time = times[0].time };
            }

            [Serializable]
            public struct TimeOverride {
                [VerticalGroup("Locale"), HideLabel] public InspectorReadonly<Locale> locale;
                [VerticalGroup("Time"), HideLabel] public FloatRange time;
            }
        }
    }
}