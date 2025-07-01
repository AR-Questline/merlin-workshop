using Awaken.Utility;
using System;
using System.Linq;
using Awaken.TG.Main.Templates;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public readonly partial struct CrimeOwners {
        public static CrimeOwners None { get; } = new(Array.Empty<CrimeOwnerTemplate>());
        
        readonly CrimeOwnerTemplate[] _crimeOwners;
        
        public CrimeOwners(CrimeOwnerTemplate owner) {
            _crimeOwners = owner != null 
                               ? new[] {owner} 
                               : Array.Empty<CrimeOwnerTemplate>();
        }

        public CrimeOwners(CrimeOwnerTemplate[] crimeOwner) {
            _crimeOwners = crimeOwner ?? throw new ArgumentException("Crime owners cannot be null");
        }
        
        public bool IsEmpty => _crimeOwners.Length == 0;
        public bool IsValid => _crimeOwners != null;
        public CrimeOwnerTemplate[] AllOwners => _crimeOwners ?? Array.Empty<CrimeOwnerTemplate>();
        public CrimeOwnerTemplate PrimaryOwner => _crimeOwners.Length == 0 ? null : _crimeOwners[0];
        
        public bool Contains(CrimeOwnerTemplate owner) {
            return _crimeOwners?.Contains(owner) ?? false;
        }
        
        [Serializable]
        public partial struct SerializedCrimeOwners {
            public ushort TypeForSerialization => SavedTypes.SerializedCrimeOwners;

            [Saved] public TemplateReference[] owners;
            
            public SerializedCrimeOwners(CrimeOwners source) {
                owners = new TemplateReference[source._crimeOwners.Length];
                for (int i = 0; i < source._crimeOwners.Length; i++) {
                    owners[i] = new(source._crimeOwners[i]);
                }
            }
            
            public readonly CrimeOwners ToCrimeOwners() {
                CrimeOwnerTemplate[] ownerTemplates = new CrimeOwnerTemplate[this.owners.Length];
                for (int i = 0; i < ownerTemplates.Length; i++) {
                    ownerTemplates[i] = owners[i].Get<CrimeOwnerTemplate>();
                }
                return new CrimeOwners(ownerTemplates);
            }
        }
    }
}