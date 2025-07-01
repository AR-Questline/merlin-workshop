using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Fights.Factions {
    /// <summary>
    /// Tree of all factions where child nodes are subfactions of parent node and inherit its antagonisms
    /// </summary>
    public class FactionTree {
        
        Faction _root;
        Dictionary<FactionTemplate, Faction> _factionByTemplates = new();

        public FactionTree(FactionTemplate[] templates) {
            try {
                _root = Faction.Root(templates.Only(t => t.parent == null), this);
            } catch (MultipleMatchException) {
                throw new Exception("There is more than one FactionTemplate with no parent");
            } catch (NoMatchException) {
                throw new Exception("There is no FactionTemplate with no parent");
            }
            
            InitStructure(_root, templates);

            foreach (var faction in AllFactions) {
                _factionByTemplates[faction.Template] = faction;
            }

            _root.RecalculateAntagonism(templates.Select(FactionByTemplate).Where(f => f.SubFactions.Count == 0).ToArray());
        }
        
        void InitStructure(Faction faction, FactionTemplate[] templates) {
            foreach (var template in templates) {
                if (faction.Template == template.parent) {
                    faction.AddSubFaction(template);
                }
            }

            foreach (var child in faction.SubFactions) {
                InitStructure(child, templates);
            }
        }
        
        public Faction FactionByTemplate(FactionTemplate template) => _factionByTemplates[template];
        
        /// <summary> Traverse tree in DFS order </summary>
        public IEnumerable<Faction> AllFactions => DepthFirstSearchFactions(_root);
        static IEnumerable<Faction> DepthFirstSearchFactions(Faction root) {
            yield return root;
            foreach (var child in root.SubFactions) {
                foreach (var faction in DepthFirstSearchFactions(child)) {
                    yield return faction;
                }
            }
        }
        
        /// <summary> Traverse tree in DFS order </summary>
        public IEnumerable<(Faction, int)> AllFactionsWithIndent => DepthFirstSearchFactionsWithIndent(_root, 0);
        static IEnumerable<(Faction, int)> DepthFirstSearchFactionsWithIndent(Faction root, int indent) {
            yield return (root, indent++);
            foreach (var child in root.SubFactions) {
                foreach (var factionWithIndent in DepthFirstSearchFactionsWithIndent(child, indent)) {
                    yield return factionWithIndent;
                }
            }
        }
    }
}