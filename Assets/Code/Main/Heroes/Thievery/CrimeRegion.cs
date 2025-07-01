using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Awaken.TG.Main.Heroes.Thievery {
    /// <summary>
    /// Defines a region in which an action will be a crime against the owner faction
    /// </summary>
    public partial class CrimeRegion : SceneSpec {
        public const string IllegalTerritoryTag = "IllegalTerritory";

        [SerializeField, PropertySpace(spaceBefore: 0, spaceAfter: 8), Tooltip("Default is: no flag, inactive; flag exists, active")] 
        FlagLogic regionEnabled;
        
        [ShowIf(nameof(AnyValidFaction), animate: false)] 
        [SerializeField] int regionPriority = 0;
        
        [FormerlySerializedAs("ownerFactions")]
        [InfoBox(" <color=#32CD32><b>Leave empty or not set to override area to allowed</b></color> \nWill set RenderLayers.TriggerVolumes and IsTrigger on all children colliders in playmode")]
        [SerializeField, ListDrawerSettings(CustomAddFunction = nameof(AddNewFactionReputationPair))]
        OwnerReputationPair[] owners = Array.Empty<OwnerReputationPair>();
        [SerializeField] CrimeType crimeCommittedHere;
        [SerializeField] bool isLockpickingArea = true;
        
        bool IsTrespassing => CrimeCommittedHere.HasFlagFast(CrimeType.Trespassing);

        public bool Enabled =>
#if UNITY_EDITOR
            Application.isPlaying ? isActiveAndEnabled && regionEnabled.Get(true) : isActiveAndEnabled;
#else
            isActiveAndEnabled && regionEnabled.Get(true);
#endif
        public string EnablingFlag => regionEnabled.Flag;
        
        public IEnumerable<CrimeOwnerTemplate> CrimeOwners => isActiveAndEnabled 
            ? owners.Where(pair => pair.ReputationInRange).Select(pair => pair.OwnerFaction)
            : Enumerable.Empty<CrimeOwnerTemplate>();
        public CrimeType CrimeCommittedHere => crimeCommittedHere | (isLockpickingArea ? CrimeType.Lockpicking : CrimeType.None);
        public int RegionPriority => regionPriority;
        
#if UNITY_EDITOR
        static FactionTemplate s_heroFaction;
#endif
        
        public bool IsSafeRegion {
            get {
                bool isSafe = true;
                
                foreach (var pair in owners) {
                    if (!pair.crimeOwner.IsSet) {
                        return true;
                    } else if (pair.ReputationInRange) {
                        isSafe = false;
                    }
                }
                return isSafe;
            }
        }

        public bool IsForCrime(CrimeType crime) => crime == CrimeType.None || CrimeCommittedHere.HasFlagFast(crime);

        public void ApplyModification(Data data) {
            regionPriority = data.regionPriority;
            owners = data.ownerFactions;
            crimeCommittedHere = data.crimeCommittedHere;
            Refresh();
        }
        
        // === System Methods

        void Awake() {
            FactionRegionsService.Initialize(this);
        }

        [Button("Apply settings to child colliders")]
        public void Refresh() {
            // TODO: Disable component based on hero reputation to target faction
            foreach (Collider colliderChild in GetComponentsInChildren<Collider>()) {
                colliderChild.gameObject.layer = RenderLayers.TriggerVolumes;
                colliderChild.isTrigger = true;
                if (IsTrespassing) {
                    colliderChild.tag = IllegalTerritoryTag;
                } else {
                    colliderChild.tag = "Untagged";
                }
            }
        }

        void OnDestroy() {
            FactionRegionsService.Uninitialize(this);
        }

        // === Modification Data
        [Serializable]
        public partial struct Data {
            public ushort TypeForSerialization => SavedTypes.Data;

            [Saved] public int regionPriority;
            [TemplateType(typeof(FactionTemplate)), HideReferenceObjectPicker]
            public OwnerReputationPair[] ownerFactions;
            [Saved] public CrimeType crimeCommittedHere;
            [Saved] [UnityEngine.Scripting.Preserve] public float bountyModifier;
        }

        public Data GetData() {
            return new Data {
                regionPriority = RegionPriority,
                ownerFactions = owners,
                crimeCommittedHere = crimeCommittedHere,
            };
        }

        [Serializable]
        public struct OwnerReputationPair {
            [SerializeField, MinMaxSlider(-3, 3, true), Tooltip("Will be active if the hero's reputation with the owner faction is within this range")] 
            public Vector2Int activeReputationRange;
            [SerializeField, TemplateType(typeof(CrimeOwnerTemplate))]
            public TemplateReference crimeOwner;

            public readonly CrimeOwnerTemplate OwnerFaction => crimeOwner.Get<CrimeOwnerTemplate>();
            public readonly int CurrentReputation => OwnerReputationUtil.CurrentReputation(crimeOwner.GUID);
            public readonly bool ReputationInRange => activeReputationRange.InRange(Application.isPlaying ? CurrentReputation : 0);
        }
        
        // === Editor
        [Space(20)]
        [Title("Editor tools")]
        [ShowInInspector, PropertyOrder(1)] Data _data;

        [Button("Apply"), DisableInEditorMode, PropertyOrder(1)]
        void Editor_Apply() {
            World.Services.Get<FactionRegionsService>().Modify(this, _data);
        }

        bool AnyValidFaction => owners is {Length: > 0} && owners.Any(f => f.crimeOwner is {IsSet: true});

        void AddNewFactionReputationPair() {
            var newPair = new OwnerReputationPair();
            newPair.activeReputationRange = new Vector2Int(-3, 3);
            owners = owners.Append(newPair).ToArray();
        }
    }
}
