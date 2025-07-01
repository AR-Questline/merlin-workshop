using System;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Thievery {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Overrides crime archetypes for this location.")]
    public class CrimeOverrideAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] CrimeMapping<CrimeArchetypeOverride> overrides;

        public ref readonly CrimeArchetype Override(in CrimeArchetype archetype) {
            ref readonly var result = ref overrides.Get(archetype, CrimeArchetypeOverride.KeepOriginal);
            if (result.overrides) {
                return ref result.archetype;
            } else {
                return ref archetype;
            }
        }

        public Element SpawnElement() {
            return new CrimeOverride();
        }

        public bool IsMine(Element element) => element is CrimeOverride;

        [Serializable]
        struct CrimeArchetypeOverride {
            public bool overrides;
            [ShowIf(nameof(overrides)), HideLabel, InlineProperty] public CrimeArchetype archetype;
            
            public static readonly CrimeArchetypeOverride KeepOriginal = new() { overrides = true };
        }
    }
}