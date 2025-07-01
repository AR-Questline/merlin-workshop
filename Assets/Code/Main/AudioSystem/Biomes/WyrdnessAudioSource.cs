using System;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem.Biomes {
    [Serializable]
    public class WyrdnessAudioSource : IAudioSource {
        [SerializeField] EventReference eventRef;
        [SerializeField, HideInInlineEditors] bool isCopyrighted;
        
        public int PriorityOverride() => 5;
        public EventReference EventReference() => eventRef;
        public Vector3? Position  { get; private set; }
        public bool IsCopyrighted => isCopyrighted;
        public void SetRefreshCallback(Action callback) { }
        public void SetPosition(Vector3 position) {
            Position = position;
        }
    }
}