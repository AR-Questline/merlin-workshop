using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments.Audio {
    public partial class AliveAudio : Element<IModel>, IRefreshedByAttachment<AliveAudioAttachment> {
        public override ushort TypeForSerialization => SavedModels.AliveAudio;

        AliveAudioContainer _audioContainer;
        AliveAudioContainer _wyrdAudioContainer;

        public virtual void InitFromAttachment(AliveAudioAttachment spec, bool isRestored) {
            _audioContainer = spec.aliveAudioContainerWrapper.Data;
            _wyrdAudioContainer = spec.usesExplicitWyrdConvertedAudio
                ? spec.wyrdConvertedAudioContainerWrapper.Data
                : CommonReferences.Get.AudioConfig.DefaultWyrdAudioContainer;
        }

        public virtual AliveAudioContainer GetContainer(bool isWyrdConverted) {
            return isWyrdConverted ? _wyrdAudioContainer : _audioContainer;
        }
    }
}