using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    public interface ILogicReceiver {
        void OnLogicReceiverStateSetup(bool state) { }
        void OnLogicReceiverStateChanged(bool state);
    }

    public interface ILogicReceiverElement : IElement, ILogicReceiver {
        public const string GroupName = "Logic Receiver Effect";
    }
}
