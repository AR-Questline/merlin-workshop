using Awaken.Utility;
using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Skills {
    public partial class DummyCharacter : Model, ICharacter, ISkillOwner, IAIEntity, IWithActor {
        public override ushort TypeForSerialization => SavedModels.DummyCharacter;

        public override Domain DefaultDomain => Domain.Gameplay;

        // === ICharacter
        public Stat Stat(StatType statType) => null;
        public string Name => "Dummy";
        public Actor Actor => DefinedActor.None.Retrieve();
        public Transform ActorTransform => ParentTransform;
        public bool Grounded => false;
        public AliveStats AliveStats => null;
        public AliveStats.ITemplate AliveStatsTemplate => null;
        public float Height => 0;
        public int Tier => 0;
        public CharacterStats CharacterStats => null;
        public CharacterStats.ITemplate CharacterStatsTemplate => null;
        public ProficiencyStats ProficiencyStats => null;
        public StatusStats StatusStats => null;
        public StatusStats.ITemplate StatusStatsTemplate => null;
        public LimitedStat Health => null;
        public LimitedStat HealthStat => null;
        public Stat MaxHealth => null;
        public HealthElement HealthElement => null;
        public bool IsAlive => true;
        public bool IsDying => false;
        public bool IsBlocking => false;
        public bool IsBlinded => true;
        public CharacterStatuses Statuses => null;
        public ICharacterInventory Inventory => null;
        public IBaseClothes<IItemOwner> Clothes => null;
        public ICharacterSkills Skills => null;
        public ICharacterView CharacterView => null;
        public ShareableARAssetReference HitVFX => null;
        public virtual Transform ParentTransform => null;
        public Transform MainHand => null;
        public Transform OffHand => null;
        public virtual Transform Head => null;
        public virtual Transform Torso => null;
        public virtual Transform Hips => null;
        public VFXBodyMarker VFXBodyMarker => null;
        [UnityEngine.Scripting.Preserve] public CharacterClothes CharacterClothes => null;
        public float Radius => 0;
        public void AttachWeapon(CharacterHandBase characterHand) => throw new NotImplementedException();
        public void DetachWeapon(CharacterHandBase characterHand) => throw new NotImplementedException();
        public CharacterHandBase MainHandWeapon => null;
        public CharacterHandBase OffHandWeapon => null;
        public CharacterDealingDamage CharacterDealingDamage => null;
        public SurfaceType AudioSurfaceType => null;
        public AliveAudio AliveAudio => null;
        public void PlayAudioClip(AliveAudioType audioType, bool asOneShot = false, params FMODParameter[] eventParams) => throw new NotImplementedException();
        public void PlayAudioClip(EventReference audioType, bool asOneShot = false, params FMODParameter[] eventParams) => throw new NotImplementedException();
        public AliveVfx AliveVfx => null;
        [UnityEngine.Scripting.Preserve] public CrimeOwnerTemplate LocationOwner => null;
        public CrimeOwners GetCurrentCrimeOwnersFor(CrimeArchetype type) => default;
        public CrimeOwnerTemplate DefaultCrimeOwner => null;
        public IAIEntity AIEntity => this;
        public void EnterParriedState() { }

        // === ISkillOwner
        public ICharacter Character => this;
        public ICollection<string> Tags => new List<string>();
        public virtual Vector3 Coords => default;
        public Quaternion Rotation => Quaternion.identity;

        public Faction Faction => World.Services.Get<FactionService>().Villagers;
        public FactionTemplate GetFactionTemplateForSummon() => Faction.Template;
        public void OverrideFaction(FactionTemplate faction, FactionOverrideContext context = FactionOverrideContext.Default) { }
        public void ResetFactionOverride(FactionOverrideContext context = FactionOverrideContext.Default) { }

        public bool DisableTargetRecalculation { 
            get => false;
            set { }
        }

        public bool CanDealDamageToFriendlies => false;
        public Vector3 HorizontalVelocity => Vector3.zero;
        public float RelativeForwardVelocity => 0;
        public float RelativeRightVelocity => 0;

        public virtual ref FightingPair.LeftStorage PossibleTargets => ref FightingPair.LeftStorage.Null;
        public virtual ref FightingPair.RightStorage PossibleAttackers => ref FightingPair.RightStorage.Null;

        // === IEntityInAIWorld
        public IWithFaction WithFaction => this;
        public Vector3 VisionDetectionOrigin => Vector3.zero;
        public VisionDetectionSetup[] VisionDetectionSetups => Array.Empty<VisionDetectionSetup>();
    }
}
