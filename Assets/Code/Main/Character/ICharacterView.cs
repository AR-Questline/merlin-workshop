using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Character {
    public interface ICharacterView : IView {
        public bool IsCharacter { get; }
        public ICharacter Character { get; }
        public void PlayAudioClip(AliveAudioType audioType, bool asOneShot, GameObject followObject = null, params FMODParameter[] eventParams);
        public void PlayAudioClip(EventReference eventReference, bool asOneShot, GameObject followObject = null, params FMODParameter[] eventParams);
    }
}