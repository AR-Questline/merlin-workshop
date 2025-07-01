using UnityEngine;

namespace Awaken.TG.Main.UI.ButtonSystem {
    public class FooterPromptsHost : IPromptHost {
        public Transform PromptsHost { get; }

        public FooterPromptsHost(Transform promptHost) {
            PromptsHost = promptHost;
        }
    }
}