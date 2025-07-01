using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Specs;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Thievery {
    public class SimpleCrimeRegion : SceneSpec {
        [SerializeField, TemplateType(typeof(CrimeOwnerTemplate))]
        TemplateReference crimeOwnerTemplate;

        public CrimeOwnerTemplate CrimeOwner => crimeOwnerTemplate.Get<CrimeOwnerTemplate>();
    }
}