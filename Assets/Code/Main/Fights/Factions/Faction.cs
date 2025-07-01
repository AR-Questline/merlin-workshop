using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions {
    /// <summary>
    /// Faction is a node in FactionTree. <br/>
    /// <br/>
    /// It inherits antagonism from BaseFaction and overrides it with its own settings where needed. <br/>
    /// The rules to calculate antagonisms are: <br/>
    /// [ If A is hostile to B's BaseFaction than A is hostile to B unless it is stated otherwise in A ] <br/>
    /// [ If A's BaseFaction is hostile to B than A is hostile to B unless it is stated otherwise in A ] <br/>
    /// <br/>
    /// It cache antagonism to all other factions so when one is changed you need to call RecalculateAntagonism <br/>
    /// </summary>
    public class Faction {
        public const string FactionContext = "faction";
        Dictionary<Faction, Antagonism> _cache = new();
        FactionTree Tree { get; }
        [UnityEngine.Scripting.Preserve] float[] _factionReputationThresholds = new float[3];
        
        public FactionTemplate Template { get; }
        public Faction BaseFaction { get; }
        public List<Faction> SubFactions { get; } = new();
        // List of overrides is shared from and handled by FactionService.
        List<FactionToFactionAntagonismOverride> AntagonismOverrides { get; set; }
        public string FactionName => Template.factionName;
        public string FactionDescription => Template.factionDescription;
        public ShareableSpriteReference FactionIconReference => Template.iconReference;

        Faction(Faction @base, FactionTemplate template, FactionTree tree) {
            Tree = tree;
            Template = template;
            BaseFaction = @base;
        }

        public static Faction Root(FactionTemplate template, FactionTree tree) {
            return new(null, template, tree);
        }
        public void AddSubFaction(FactionTemplate template) => SubFactions.Add(new Faction(this, template, Tree));
        Antagonism? OverridenAntagonismTo(Faction faction) {
            if (AntagonismOverrides == null) {
                return null;
            }
            foreach (var antagonismOverride in AntagonismOverrides) {
                if (antagonismOverride.TargetFactionTemplate == faction.Template) {
                    return antagonismOverride.Antagonism;
                }
            }
            return null;
        }

        public Antagonism DefaultAntagonismTo(Faction faction) => _cache[faction];
        public Antagonism AntagonismTo(Faction faction) => OverridenAntagonismTo(faction) ?? DefaultAntagonismTo(faction);
        
        [UnityEngine.Scripting.Preserve] public bool IsFriendlyTo(Faction faction) => AntagonismTo(faction) == Antagonism.Friendly;
        [UnityEngine.Scripting.Preserve] public bool IsNeutralTo(Faction faction) => AntagonismTo(faction) == Antagonism.Neutral;
        public bool IsHostileTo(Faction faction) => AntagonismTo(faction) == Antagonism.Hostile;
        
        public void RecalculateAntagonism(Faction[] leaves) {
            var myAntagonisms = new Dictionary<Faction, Antagonism>();
            _cache.Clear();

            // place for adding antagonism overrides eg. from story
            
            foreach (var template in Template.friendly) {
                myAntagonisms.TryAdd(Tree.FactionByTemplate(template), Antagonism.Friendly);
            }
            foreach (var template in Template.neutral) {
                myAntagonisms.TryAdd(Tree.FactionByTemplate(template), Antagonism.Neutral);
            }
            foreach (var template in Template.hostile) {
                myAntagonisms.TryAdd(Tree.FactionByTemplate(template), Antagonism.Hostile);
            }

            foreach (var leaf in leaves) {
                BuildUp(leaf, out _, myAntagonisms);
            }

            foreach (var subFaction in SubFactions) {
                subFaction.RecalculateAntagonism(leaves);
            }
        }
        
        public void SetAntagonismOverridesList(List<FactionToFactionAntagonismOverride> list) {
            AntagonismOverrides = list;
        }

        public void RemoveAntagonismOverridesList() {
            AntagonismOverrides = null;
        }

        bool BuildUp(Faction leaf, out Antagonism antagonism, Dictionary<Faction, Antagonism> myAntagonisms) {
            if (leaf == null) {
                antagonism = Antagonism.Neutral;
                return false;
            } else if (myAntagonisms.TryGetValue(leaf, out antagonism)) {
                _cache[leaf] = antagonism;
                BuildUp(leaf.BaseFaction, out _, myAntagonisms);
                return true;
            } else if (BuildUp(leaf.BaseFaction, out antagonism, myAntagonisms)) {
                _cache[leaf] = antagonism;
                return true;
            } else {
                _cache[leaf] = BaseFaction?._cache[leaf] ?? Antagonism.Neutral;
                return false;
            }
        }

        public bool Is(Faction faction) {
            Faction parent = this;
            while (parent != null) {
                if (parent == faction) {
                    return true;
                }
                parent = parent.BaseFaction;
            }
            return false;
        }
    }

    public enum Antagonism {
        Friendly, 
        Neutral,
        Hostile,
    }
}