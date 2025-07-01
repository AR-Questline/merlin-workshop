using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Character {
    public interface IAliveAudio : IModel {
        AliveAudio AliveAudio { get; }
        bool WyrdConverted => false;
    }
}