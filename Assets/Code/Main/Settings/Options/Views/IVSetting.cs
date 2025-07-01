using Awaken.TG.MVC;
using UnityEngine.UI;

namespace Awaken.TG.Main.Settings.Options.Views {
    public interface IVSetting : IView {
        Selectable MainSelectable { get; }
        void Setup(PrefOption option);
    }
}