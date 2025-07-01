using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations {
    public readonly struct RenderingErrorMessage {
        readonly string _fullMessage;
        public RenderingErrorLogType MessageType { get; }
        public string Message { get; }

        [ShowInInspector, HideLabel, DisplayAsString(false, EnableRichText = true)]
        public string FullMessage {
            get => _fullMessage;
            set { return; }
        }

        public RenderingErrorMessage(string message, RenderingErrorLogType messageType) {
            MessageType = messageType;
            Message = message;
            _fullMessage = $"<color={MessageType.ToHexColor()}>{messageType}</color>: {message}";
        }
    }
}
