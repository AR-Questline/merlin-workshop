using System;
using System.Collections.Generic;
using Awaken.TG.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility {
    public class PrefabNotes : MonoBehaviour {
        [ListDrawerSettings(HideAddButton = true, ShowFoldout = false)]
        public List<Message> messages = new List<Message>();
        
        [TextArea(1,100)]
        public string notes;
        [HorizontalGroup("color_size", 0.2f, LabelWidth = 75)]public Color color = Color.white;
        [HorizontalGroup("color_size", 0.8f, LabelWidth = 75), Range(5, 32)] public int size = 12;
        [HorizontalGroup("bold_italic")]public bool bold;
        [HorizontalGroup("bold_italic")]public bool italic;

        [Button]
        void AddMessage() {
            var message = notes.ColoredText(color);
            message = $"<size={size}%>{message}</size>";
            if (bold) {
                message = $"<b>{message}</b>";
            }

            if (italic) {
                message = $"<i>{message}</i>";
            }
            messages.Add(new Message(){message = message});
        }

        [Serializable]
        public class Message {
            [ReadOnly, InfoBox("$message")]public string message;
        }
    }
}