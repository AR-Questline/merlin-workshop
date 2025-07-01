using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using FMODUnity;

namespace Awaken.TG.Main.Utility.Audio {
    [RichEnumAlwaysDisplayCategory]
    public class AliveAudioType : ARAudioType<IAliveAudio> {
        protected AliveAudioType(string id, Func<IAliveAudio, EventReference> getter, string inspectorCategory = "") : base(id, getter, inspectorCategory) {}

        [UnityEngine.Scripting.Preserve]
        public static readonly AliveAudioType
            Idle = new(nameof(Idle), c => GetAudioContainer(c).idle),
            Hurt = new(nameof(Hurt), c => GetAudioContainer(c).hurt),
            Die = new(nameof(Die), c => GetAudioContainer(c).die),
            Attack = new(nameof(Attack), c => GetAudioContainer(c).attack),
            SpecialAttack = new(nameof(SpecialAttack), c => GetAudioContainer(c).specialAttack),
            SpecialBegin = new(nameof(SpecialBegin), c => GetAudioContainer(c).specialBegin),
            SpecialRelease = new(nameof(SpecialRelease), c => GetAudioContainer(c).specialRelease),
            Fall = new(nameof(Fall), c => GetAudioContainer(c).fall),
            Dash = new(nameof(Dash), c => GetAudioContainer(c).dash),
            Roar = new(nameof(Roar), c => GetAudioContainer(c).roar, "Beast"),
            FootStep = new(nameof(FootStep), c => GetAudioContainer(c).footStep, "Beast");
        
        static AliveAudioContainer GetAudioContainer(IAliveAudio audioOwner) {
            if (audioOwner?.AliveAudio != null) {
                return audioOwner.AliveAudio.GetContainer(audioOwner.WyrdConverted);
            }
            
            return (audioOwner?.WyrdConverted ?? false)
                ? CommonReferences.Get.AudioConfig.DefaultWyrdAudioContainer
                : CommonReferences.Get.AudioConfig.DefaultAliveAudioContainer;
        }
    }
}