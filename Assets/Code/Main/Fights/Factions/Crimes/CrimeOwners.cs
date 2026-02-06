using Awaken.Utility;
using System;
using System.Collections.Generic;
using Awaken.TG.Main.Templates;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public readonly partial struct CrimeOwners : IDisposable {
        public static CrimeOwners None { get; } = new CrimeOwners(null as CrimeOwnerTemplate);
        
        readonly RentedArray<CrimeOwnerTemplate> _crimeOwners;
        
        public CrimeOwners(CrimeOwnerTemplate owner) {
            if (owner == null) {
                _crimeOwners = RentedArray<CrimeOwnerTemplate>.Borrow(0);
            } else {
                _crimeOwners = RentedArray<CrimeOwnerTemplate>.Borrow(1);
                _crimeOwners[0] = owner;
            }
        }

        public CrimeOwners(HashSet<CrimeOwnerTemplate> crimeOwners) {
            _crimeOwners = RentedArray<CrimeOwnerTemplate>.Borrow(crimeOwners);
        }

        CrimeOwners(RentedArray<CrimeOwnerTemplate> crimeOwners) {
            _crimeOwners = crimeOwners;
        }

        public void Dispose() {
            if (_crimeOwners.IsCreated) {
                _crimeOwners.Dispose();
            }
        }

        public bool IsEmpty => _crimeOwners.length == 0;
        public bool IsValid => _crimeOwners.IsCreated;
        public  RentedArray<CrimeOwnerTemplate> AllOwners => _crimeOwners;
        public CrimeOwnerTemplate PrimaryOwner => _crimeOwners.length == 0 ? null : _crimeOwners[0];
        
        public bool Contains(CrimeOwnerTemplate owner) {
            return _crimeOwners.Contains(owner);
        }
        
        [Serializable]
        public partial struct SerializedCrimeOwners {
            public ushort TypeForSerialization => SavedTypes.SerializedCrimeOwners;

            [Saved] public TemplateReference[] owners;
            
            public SerializedCrimeOwners(CrimeOwners source) {
                owners = new TemplateReference[source._crimeOwners.length];
                for (int i = 0; i < source._crimeOwners.length; i++) {
                    owners[i] = new(source._crimeOwners[i]);
                }
            }
            
            public readonly CrimeOwners ToCrimeOwners() {
                var ownerTemplates = RentedArray<CrimeOwnerTemplate>.Borrow(owners.Length);
                for (int i = 0; i < owners.Length; i++) {
                    ownerTemplates[i] = owners[i].Get<CrimeOwnerTemplate>();
                }
                return new CrimeOwners(ownerTemplates);
            }
        }
    }
}