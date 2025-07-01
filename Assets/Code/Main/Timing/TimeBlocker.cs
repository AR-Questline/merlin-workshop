using Awaken.Utility;
using System;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Timing {
    public partial class TimeBlocker : Model {
        public override ushort TypeForSerialization => SavedModels.TimeBlocker;

        public override Domain DefaultDomain => Domain.Gameplay;

        // Use this ID to identify who blocked the time in case of unblocked instance
        [Saved] public string SourceID { get; private set; }
        [Saved] public TimeType BlockType { get; private set; }

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        TimeBlocker() { }

        public TimeBlocker(string id, TimeType type) {
            SourceID = id;
            BlockType = type;
        }

        protected override void OnInitialize() {
            UIStateStack.Instance.PushState(CreateUIState(), this);
        }

        UIState CreateUIState() {
            var state = UIState.TransparentState;
            state = BlockType switch {
                TimeType.Weather => state.WithPauseWeatherTime(),
                _ => throw new ArgumentOutOfRangeException()
            };
            return state;
        }
    }

    public enum TimeType {
        Weather = 0,
    }
}