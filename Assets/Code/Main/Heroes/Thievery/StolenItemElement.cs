using System;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Thievery {
    public partial class StolenItemElement : Element<Item> {
        public override ushort TypeForSerialization => SavedModels.StolenItemElement;

        [Saved] CrimeOwners.SerializedCrimeOwners _owners;
        CrimeOwners _runtimeOwners;

        [JsonConstructor, UnityEngine.Scripting.Preserve] StolenItemElement() { }

        public StolenItemElement(CrimeDataRuntime data) {
            _owners = data.crimeOwners;
            _runtimeOwners = _owners.ToCrimeOwners();
        }
        
        public StolenItemElement(in Crime crime) {
            _owners = new(crime.Owners);
            _runtimeOwners = crime.Owners;
        }

        protected override void OnInitialize() {
            if (!_runtimeOwners.IsValid) {
                _runtimeOwners = _owners.ToCrimeOwners();
            }
        }

        public CrimeDataRuntime GetCrimeData() => CrimeDataRuntime.GetCrimeData(this);
        
        public static bool IsStolen(Item item) => item.HasElement<StolenItemElement>();
        public static bool IsStolenFrom(Item item, CrimeOwnerTemplate crimeOwner) {
            var stolenItemElement = item.TryGetElement<StolenItemElement>();
            return stolenItemElement != null && stolenItemElement._runtimeOwners.Contains(crimeOwner);
        }

        public static string StolenText(Item item) {
            var crimeOwnerName = item.Element<StolenItemElement>()._runtimeOwners.PrimaryOwner.DisplayName;
            return $"<color=#{ColorUtility.ToHtmlStringRGB(ARColor.MainRed)}>{LocTerms.StolenItem.Translate()}:</color> {crimeOwnerName}";
        }

        [Serializable]
        public partial class CrimeDataRuntime {
            public ushort TypeForSerialization => SavedTypes.CrimeDataRuntime;

            [Saved] public CrimeOwners.SerializedCrimeOwners crimeOwners;
            
            [JsonConstructor, UnityEngine.Scripting.Preserve] CrimeDataRuntime() { }

            CrimeDataRuntime(CrimeOwners.SerializedCrimeOwners owners) {
                crimeOwners = owners;
            }
            
            public static CrimeDataRuntime GetCrimeData(StolenItemElement stolenItem) => new(stolenItem._owners);
        }
    }
}