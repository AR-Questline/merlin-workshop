using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Cysharp.Threading.Tasks;
using FMODUnity;
using AudioType = Awaken.TG.Main.AudioSystem.AudioType;

namespace Awaken.TG.Main.Stories {
    public partial class StoryMusic : Model {
        public override Domain DefaultDomain => Domain.CurrentScene();
        public sealed override bool IsNotSaved => true;

        public readonly AudioType managerType;
        readonly IAudioSource _audioSource;
        readonly bool _asOneShot;
        bool _registered;

        public StoryMusic(AudioType managerType, IAudioSource audioSource, bool asOneShot) {
            this.managerType = managerType;
            _audioSource = audioSource;
            _asOneShot = asOneShot;
        }

        protected override void OnInitialize() {
            World.Services.Get<AudioCore>().RegisterAudioSource(_audioSource, managerType);
            _registered = true;
            if (_asOneShot) {
                DiscardAfterPlayed().Forget();
            }
        }

        async UniTaskVoid DiscardAfterPlayed() {
            int length = 0;
            // if (RuntimeManager.TryGetEventDescription(_audioSource.EventReference(), out var desc)) {
            //     desc.getLength(out length);
            // } else {
            //     length = 0;
            // }
            if (await AsyncUtil.DelayTime(this, length * 0.001f)) {
                Discard();
            }
        }

        void UnRegister() {
            if (!_registered) {
                return;
            }
            World.Services.Get<AudioCore>().UnregisterAudioSource(_audioSource, managerType);
            _registered = false;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop) {
                UnRegister();
            }
        }
    }
}