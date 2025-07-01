using System.Collections.Generic;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Graphics.FloatingTexts {
    [UsesPrefab("HUD/VFloatingTextHUD")]
    public class VFloatingTextHUD : View<HUD> {
        // === References
        public RectTransform anchor;
        
        // === Fields 
        FloatingText _lastFloatingText;
        readonly List<Message> _texts = new List<Message>();

        // === Properties
        bool CanSpawnNew => _lastFloatingText == null || _lastFloatingText.Progress > 0.99f;
        
        // === Initialization
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnHUD();

        protected override void OnInitialize() {
            
        }

        // === Show
        public void ShowText(string text, byte priority) {
            if (CanSpawnNew) {
                _lastFloatingText = Services.Get<FloatingTextService>().SpawnGlobalFloatingText(text, anchor, transform);
            } else {
                var message = new Message() {text = text, priority = priority};
                for (int i = 0; i < _texts.Count; i++) {
                    if (_texts[i].priority > priority) {
                        _texts.Insert(i, message);
                        return;
                    }
                }
                _texts.Add(message);
            }
        }
        
        // === Update
        void Update() {
            if (_texts.Count > 0) {
                var message = _texts[0];
                _texts.RemoveAt(0);
                ShowText(message.text, message.priority);
            }
        }

        struct Message {
            public string text;
            public byte priority;
        }
    }
}