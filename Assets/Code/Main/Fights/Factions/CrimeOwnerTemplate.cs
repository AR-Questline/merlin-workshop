using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Duels;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.Factions.FactionEffects;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions {
    public class CrimeOwnerTemplate : Template {
        [SerializeField, LocStringCategory(Category.Faction)] LocString displayName;
        [ShowAssetPreview, ARAssetReferenceSettings(new[] {typeof(Texture2D), typeof(Sprite)}, true, AddressableGroup.StatusEffects)]
        [UnityEngine.Scripting.Preserve] public ShareableSpriteReference iconReference;
        [Space]
        [SerializeField] int unforgivableCrimeBountyLimit = int.MaxValue;

        [SerializeField] bool hasBounty = true;
        
        [SerializeField] CrimeMapping<CrimeSeverity> severities = new(new[] {
            CrimeMapping<CrimeSeverity>.Entry.AllCrimes().WithOutput(CrimeSeverity.Normal),
        });

        [SerializeField, FoldoutGroup("Bounties/Item", VisibleIf = nameof(hasBounty))] CrimeItemValueData lowItemValue = new(10, 1f);
        [SerializeField, FoldoutGroup("Bounties/Item")] CrimeItemValueData mediumItemValue = new(25, 1.5f);
        [SerializeField, FoldoutGroup("Bounties/Item")] CrimeItemValueData highItemValue = new(100, 2.5f);
        
        [SerializeField, FoldoutGroup("Bounties/Npc")] CrimeNpcValueData lowNpcValue = new(1, 10, 1f, 200);
        [SerializeField, FoldoutGroup("Bounties/Npc")] CrimeNpcValueData mediumNpcValue = new(2, 0, 1.5f, 500);
        [SerializeField, FoldoutGroup("Bounties/Npc")] CrimeNpcValueData highNpcValue = new(4, -50, 2.5f, 1000);
        
        [SerializeField, FoldoutGroup("Bounties", VisibleIf = nameof(hasBounty))] float combat = 100;
        [SerializeField, FoldoutGroup("Bounties")] float trespassing = 100;
        [SerializeField, FoldoutGroup("Bounties")] float lockpicking = 100;

        [SerializeField] DuelArenaData duelArena;
        [SerializeField, ShowIf(nameof(hasBounty))] 
        SceneReference prison;
        
        [Title("Relations")]
        // Reputation
        public bool hasReputation;
        [SerializeField, ShowIf(nameof(hasReputation))] public StoryBookmark reputationBookmark;
        [SerializeField, ShowIf(nameof(hasReputation)), ListDrawerSettings(IsReadOnly = true)]
        public IntRange[] reputationRanges = new IntRange[4];
        [SerializeField, ShowIf(nameof(hasReputation))] public FactionEffect[] factionEffects = Array.Empty<FactionEffect>();

        public int MaxReputation => reputationRanges[3].high;

        public string DisplayName => displayName;
        public bool IsUnforgivableCrimeBountyLimit(int bounty) => bounty >= unforgivableCrimeBountyLimit;
        public bool HasBounty => hasBounty;
        public bool IsAcceptable(in CrimeArchetype archetype) => severities.Get(archetype, CrimeSeverity.Normal) == CrimeSeverity.Acceptable;
        public bool IsUnforgivable(in CrimeArchetype archetype) => severities.Get(archetype, CrimeSeverity.Normal) == CrimeSeverity.Unforgivable;

        public ref readonly CrimeItemValueData ItemBounty(CrimeItemValue value) {
            switch (value) {
                case CrimeItemValue.None: return ref CrimeItemValueData.None;
                case CrimeItemValue.Low: return ref lowItemValue;
                case CrimeItemValue.Medium: return ref mediumItemValue;
                case CrimeItemValue.High: return ref highItemValue;
                default: throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        public ref readonly CrimeNpcValueData NpcBounty(CrimeNpcValue value) {
            switch (value) {
                case CrimeNpcValue.None: return ref CrimeNpcValueData.None;
                case CrimeNpcValue.Low: return ref lowNpcValue;
                case CrimeNpcValue.Medium: return ref mediumNpcValue;
                case CrimeNpcValue.High: return ref highNpcValue;
                default: throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
        
        public float CombatBounty => combat;
        public float TrespassingBounty => trespassing;
        public float LockpickingBounty => lockpicking;
        public DuelArenaData DuelArena => duelArena;
        public SceneReference Prison => prison;
        public CrimeOwnerData CrimeSavedData => World.Services.Get<CrimeService>().GetCrimeData(this);
        
#if UNITY_EDITOR
        [Title("Editor tools")]
        [Button] void AddBounty(float bounty) => CrimeUtils.AddBounty(this, bounty, out _);
        [Button] void ClearBounty() => CrimeUtils.ClearBounty(this);
#endif
    }
    
    public enum CrimeSeverity : byte {
        Normal,
        Acceptable,
        Unforgivable,
    }
}