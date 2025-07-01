using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen.PatchNotes {
    public class PatchNotesContainer : ScriptableObject, IService {
        [InfoBox("Duplicated version", InfoMessageType.Error, nameof(HasDuplication))]
        [ListDrawerSettings(DraggableItems = false)]
        public List<PatchNote> patchNotes = new List<PatchNote>();
        public PatchNote For(string version) => patchNotes.LastOrDefault(n => n.majorVersionNotes ? version.StartsWith(n.version) : n.version == version);

        // === Editor
        bool HasDuplication() {
            return patchNotes.Any(p => patchNotes.Any(p2 => p != p2 && p.version == p2.version));
        }
    }
}