using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Editor.Main.Utility;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using XNode;

// User Manual for AutoGraphCreator can be found at https://www.notion.so/awaken/Story-Graphs-Automatyczne-generowanie-graf-w-f242f24d22ce4caa8de71e04978a694e

// Auto graph creator is a tool that allows you to create a story graph from a formatted dialogue input.
// Generic rules for creating said input can be found in the User Manual, and they are as follows:
// 1. In the first line of text, the first word should include the name of the NPC with whom the player's character is conversing.
//    This is optional—when creating the graph, not every NPC may be defined yet.
// 2. Before each individual line of dialogue from the player or NPC—whether it is a choice or a regular statement—a ‘$’ symbol should be inserted.
// 3. Before each player choice, an additional choice context marker should be introduced in the form of [CX],
//    where X represents a unique number for that specific choice node.
//    Simply put, choices displayed on the screen at the same time should have the same context markers.
//
// The example of the input that can be used to generate a story graph is as follows:
//
// Frithir
// $Ah, a new set of eyes. And how hateful they are… Will you close them for me, please? Would you grant me this little bit of mercy?
// $[c1]Why do you want me to close my eyes?
// $I dislike seeing what they see. There is something about you that reminds me of Mother.
// $She sees everything, and when she allows, I see it all too. It feels like a thousand eyes opening at once, revealing nothing but boundless suffering wherever they look.
// $Your eyes seem to carry a similar kind of rot, just around the edges. It’s as if you were a freshly slain corpse, yet to comprehend the fate that has befallen it.
// $[c2]Can you tell me more about Mother?
// $Mother is bathed in shadows and swaddled in raven wings. All she ever sees is misery.
// $The day she appeared in the Temple, she requested a frith from me. As I cast my gaze upon her, my mind shattered, and my soul was torn into pieces.
// $On that very day, I made her an offering of my eyes, begging to be freed from my Gift. She deemed it an insult, and has been punishing me ever since…
// $[c2]So you can… see through my eyes?
// $All eyes in the Temple are at my disposal, whether I wish it or not.
// $My Sight knows no limits, and all I see, I take upon myself – the sins, the impurities, the perversity. They fill me to the brim and cling to my throat like a black tar.
// $Mother made me an unwilling confessor to any unfortunate soul who dares to set foot in her sanctum.
// $The only thing I can grant them is absolution, albeit a hollow one. After all, they’ve already doomed themselves by coming here. 

// TODO: PR - Document Randomize, Separate, Bookmark, and Actor markers

namespace Awaken.TG.Editor.Main.Stories.AutoGen {
    /// <summary>
    /// User Manual for AutoGraphCreator can be found at https://www.notion.so/awaken/Story-Graphs-Automatyczne-generowanie-graf-w-f242f24d22ce4caa8de71e04978a694e
    /// </summary>
    public class AutoGraphCreatorWindow : OdinEditorWindow {
        readonly Regex _remarksAndCommentsRegexMatch = new(@"((\{|\[).+(\}|\]))");
        readonly Regex _statementRegexMatch = new(@"\$");
        
        [TextArea(1, 6), ShowInInspector, PropertyOrder(0)]
        string _input = "";
        
        [ShowInInspector, PropertyOrder(1), BoxGroup("BoxGroup", false)]
        [InfoBox("Set this field up if you want to add new nodes to the existing story graph, " +
                 "instead of creating a new one.")]
        Node _startNode;
        
        HashSet<ActorRef> _allowedActors = new();
        ActorRef _mainSpeaker;
        StoryGraph _storyGraph;
        
        List<NodeDataElement> _nodes;
        
        [MenuItem("TG/Graphs/Auto Graph Creator")]
        public static void Open() {
            OdinEditorWindow.CreateWindow<AutoGraphCreatorWindow>();
        }

        [Button, BoxGroup("BoxGroup"), HorizontalGroup("BoxGroup/Buttons"), PropertyOrder(2)]
        void CreateNewStoryGraph() {
            Init();
            new AutoGraphCreator(_mainSpeaker, _nodes).CreateNewStoryGraph();
        }

        [Button, BoxGroup("BoxGroup"), HorizontalGroup("BoxGroup/Buttons"), ShowIf("_startNode"),
         PropertyOrder(3)]
        void AddToExisting() {
            Init((StoryGraph)_startNode.graph);
            new AutoGraphCreator(_storyGraph, _startNode, _nodes, _mainSpeaker, _allowedActors).AddToExisting();
        }

        void Init(StoryGraph graph = null) {
            _storyGraph = graph;
            var input = AutographUtils.CorrectAlternativeMarkers(_input);
            var statements = _statementRegexMatch.Split(input);
            var converter = new StatementsToNodeDataElementsConverter(statements.Skip(1).ToArray());
            converter.ConvertInputToNodeDataElements(out List<NodeDataElement> nodes);
            
            _nodes = nodes;
            DesignateActors(statements.First(), _storyGraph?.allowedActors);
        }
        
        void DesignateActors(string header, IEnumerable<ActorRef> allowedActors = null) {
            _allowedActors = allowedActors == null ? new HashSet<ActorRef>() : allowedActors.ToHashSet();
            _mainSpeaker = DefinedActor.None.ActorRef;
            var cleanHeader = _remarksAndCommentsRegexMatch.Replace(header, "");
            
            if (string.IsNullOrWhiteSpace(cleanHeader)) {
                return;
            }

            string[] namesProvided = cleanHeader.Split(',').Where(s => string.IsNullOrWhiteSpace(s) == false).ToArray();

            foreach (var providedName in namesProvided) {
                string actorName = providedName.Trim();

                if (_allowedActors.TryGetFirst(p => Regex.IsMatch(p.guid, actorName), out ActorRef actorRef)) {
                    continue;
                }

                if (!ActorFinder.TryGetActorWithFix(actorName, out actorRef)) {
                    continue;
                }

                if (_allowedActors.Count == 0) {
                    _allowedActors = new HashSet<ActorRef>() { DefinedActor.Hero.ActorRef };
                }

                _allowedActors.Add(actorRef);
            }

            if (!ActorFinder.TryGetActorRef(namesProvided[0], out _mainSpeaker)) {
                if (!_allowedActors.TryGetFirst(p => RichEnum.AllValuesOfType<DefinedActor>().All(e => e.ActorRef != p), out _mainSpeaker)) {
                    _mainSpeaker = DefinedActor.None.ActorRef;
                }
            }
        }
    }
}