using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Debugging.RenderingValidations {
    public readonly struct RenderingError {
        readonly RenderingContextObject _context;
        readonly long _score;
        readonly RenderingErrorLogType _highestType;
        readonly string _highestTypeString;
        readonly RenderingErrorMessage[] _messages;

        public RenderingErrorLogType HighestLogType => _highestType;

        // We want these to be not gray-out in the inspector, so we need empty setters
        [ShowInInspector, TableColumnWidth(70, Resizable = false), DisplayAsString]
        public long Score {
            get => _score;
            set { return; }
        }
        
        // DisplayAsString(false) is needed to show color
        [ShowInInspector, TableColumnWidth(80, Resizable = false), DisplayAsString(false, EnableRichText = true)]
        public string HighestType {
            get => _highestTypeString;
            set { return; }
        }
        
        [ShowInInspector, TableColumnWidth(150)]
        public Object ContextObject {
            get => _context.context;
            set { return; }
        }

        [ShowInInspector, LabelText("Messages"), ListDrawerSettings(IsReadOnly = true)]
        public RenderingErrorMessage[] Messages {
            get => _messages;
            set { return; }
        }
        
        [ShowInInspector, TableColumnWidth(60), LabelText("Scene Objects"), ReadOnly, ListDrawerSettings(IsReadOnly = true)]
        public List<GameObject> SceneObjects {
            get => _context.sceneObjectsList;
            set { return; }
        }

        public RenderingError(RenderingContextObject context, RenderingErrorMessage[] messages) {
            _context = context;
            Array.Sort(messages,
                static (a, b) => b.MessageType.Value().CompareTo(a.MessageType.Value()));
            _messages = messages;
            _highestType = messages[0].MessageType;
            _highestTypeString = $"<color={_highestType.ToHexColor()}>{_highestType}</color>";
            _score = CalculateScoring(messages);
        }
        
        public void Bake() {
            _context.Bake();
        }

        static long CalculateScoring(RenderingErrorMessage[] messages) {
            long score = 0;
            foreach (var message in messages) {
                score += message.MessageType.Value();
            }
            return score;
        }
    }
}
