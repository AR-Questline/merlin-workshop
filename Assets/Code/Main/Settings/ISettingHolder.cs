using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Settings {
    public interface ISettingHolder : IModel {
        Prompts Prompts { get; }
        
        public new static class Events {
            public static readonly Event<ISettingHolder, ISettingHolder> KeyProcessed = new(nameof(KeyProcessed));
        }
    }
}