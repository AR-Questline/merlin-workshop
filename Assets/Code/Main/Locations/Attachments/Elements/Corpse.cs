using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class Corpse : Element<Location>, IAIEntity, IWithFaction, IGrounded, IWithCrimeNpcValue {
        public override ushort TypeForSerialization => SavedModels.Corpse;

        const float DeathNoiseMultiplier = 1.2f;
        [Saved] FactionTemplate _defaultFaction;
        [Saved] FactionContainer _factionContainer;
        [Saved] WeakModelRef<ICharacter> _killer;
        [Saved] CrimeNpcValue _crimeValue;
        readonly VisionDetectionSetup[] _visionDetectionSetups = new VisionDetectionSetup[1];

        readonly HashSet<IAIEntity> _viewedBy = new();
        
        public NpcChunk NpcChunk { get; set; }
        
        public CrimeNpcValue CrimeValue => _crimeValue;

        public Faction Faction => _factionContainer.Faction;
        public FactionTemplate GetFactionTemplateForSummon() => _factionContainer.GetFactionTemplateForSummon();
        public IWithFaction WithFaction => this;
        public Vector3 VisionDetectionOrigin => ParentModel.Coords;
        public VisionDetectionSetup[] VisionDetectionSetups {
            get {
                _visionDetectionSetups[0] = new(VisionDetectionOrigin, 0, VisionDetectionTargetType.Main);
                return _visionDetectionSetups;
            }
        }

        public Vector3 Coords => ParentModel.Coords;
        public Quaternion Rotation => ParentModel.Rotation;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public Corpse() {}

        public Corpse(NpcElement npc, ICharacter killer) {
            _defaultFaction = npc.Faction.Template;
            _factionContainer = new FactionContainer();
            _factionContainer.SetDefaultFaction(_defaultFaction);
            _crimeValue = npc.CrimeValue;
            _killer = new WeakModelRef<ICharacter>(killer);
            foreach (var npcEnemy in npc.GetTargeting().OfType<NpcElement>()) {
                TryToMarkAsViewed(npcEnemy.NpcAI);
            }
        }

        protected override void OnInitialize() {
            if (_killer.Get() != null) {
                MakeDeathSoundAfterDelay().Forget();
            }
        }

        protected override void OnRestore() {
            _factionContainer.SetDefaultFaction(_defaultFaction);
        }

        public void OverrideFaction(FactionTemplate faction, FactionOverrideContext context = FactionOverrideContext.Default) => _factionContainer.OverrideFaction(faction, context);
        public void ResetFactionOverride(FactionOverrideContext context = FactionOverrideContext.Default) => _factionContainer.ResetFactionOverride(context);

        public bool TryToMarkAsViewed(IAIEntity aiEntity) {
            return _viewedBy.Add(aiEntity);
        }
        
        public bool WasViewedBy(IAIEntity aiEntity) {
            return _viewedBy.Contains(aiEntity);
        }

        async UniTaskVoid MakeDeathSoundAfterDelay() {
            var fullyWaited = await AsyncUtil.DelayTime(this, Services.Get<GameConstants>().DeathNoiseDelay);
            if (!fullyWaited) {
                return;
            }

            InformAboutCrime();

            var noiseRange = Services.Get<GameConstants>().DeathNoiseRange;
            var point = ParentModel.Coords;
            
            foreach (NpcElement npc in Services.Get<NpcGrid>().GetHearingNpcs(ParentModel.Coords, noiseRange)) {
                if (npc.Faction.AntagonismTo(Faction) != Antagonism.Friendly) {
                    continue;
                }
                
                NpcAI noiseTarget = npc.NpcAI;
                bool canListen = TryToMarkAsViewed(noiseTarget) && !noiseTarget.InCombat;
                if (!canListen) {
                    continue;
                }
                
                bool inHearingRange = noiseTarget.TryCalculateAlertStrength(noiseRange, NoiseStrength.Strong, 1f, true, point, out float alertStrength, out _);
                if (!inHearingRange) {
                    continue;
                }
                
                // This is "Death noise", so it should be more alerting than regular noise
                noiseTarget.AlertStack.NewPoi(alertStrength * DeathNoiseMultiplier, this);
            }
        }

        void InformAboutCrime() {
            ICharacter killer = _killer.Get();
            // If player killed someone and he was by default NOT Hostile to hero, then it's a crime
            if (killer is Hero && !Faction.IsHostileTo(killer.Faction)) {
                Crime crime = Crime.Murder(this);
                if (!crime.TryCommitCrime()) {
                    HeroCrimeWithProlong.ProlongHeroMurder(crime, this, new TimeDuration(120));
                }
            }
        }
    }
}
