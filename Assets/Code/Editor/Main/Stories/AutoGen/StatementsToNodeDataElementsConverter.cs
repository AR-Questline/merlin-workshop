using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Stories.AutoGen {
    public class StatementsToNodeDataElementsConverter {
        readonly List<NodeDataElement> _nodes = new();
        readonly Dictionary<string, ChoiceNodeDataElement> _choiceNodesByMarkers = new();

        readonly Regex _choiceRegexMatch = new(@"(\[)c[1-9]+(\])");
        readonly Regex _remarksAndCommentsRegexMatch = new(@"\{(?<=\{)(.*?)(?=\})\}");
        readonly Regex _markersRegexMatch = new(@"\[(?<=\[)(.*?)(?=\])\]");
        readonly Regex _actorNamesRegexMatch = new(@"\<(?<=\<)(.*?)(?=\>)\>");
        readonly Regex _sepRegexMath = new(@"\[sep\]");
        readonly Regex _randRegexMatch = new(@"\[rand\]");
        readonly Regex _bookmarkRegexMatch = new(@"\[bookmark\]");
        
        int _uniqueId;
        int _currentDepth;
        bool _wasLastStatementAChoice;
        string[] _statements;

        TextNodeDataElement _lastTextNodeDataElement;
        TextNodeDataElement _curTextNode;

        public StatementsToNodeDataElementsConverter(string[] statements) {
            _statements = statements;
        }

        public void ConvertInputToNodeDataElements(out List<NodeDataElement> nodes) {
            _nodes.Clear();
            _choiceNodesByMarkers.Clear();

            CreateProperNodesFromStatements();

            nodes = _nodes;
        }

        void CreateProperNodesFromStatements() {
            //By using Unique ID instead of reference to object we can do all of the work within one loop,
            //using the possibility to point to the not yet existing nodes.
            _uniqueId = 0;
            _currentDepth = 0;
            _wasLastStatementAChoice = false;

            CreateFirstNode();
            ExcludeSeparateStatements(out string[] separateStatements);
            CreateNodesForStatementsSeparated(separateStatements);
            CreateNodesForStatementsStandard();
            ConnectTheFirstNodeWithTheFirstChoice();
        }
        
        void CreateFirstNode() {
            _curTextNode = new TextNodeDataElement(_uniqueId, _currentDepth);
            _lastTextNodeDataElement = _curTextNode;
            _nodes.Add(_curTextNode);
        }

        void ExcludeSeparateStatements(out string[] separatedStatements) {
            separatedStatements = _statements.Where(p => _sepRegexMath.IsMatch(p)).ToArray();
            _statements = _statements.Where(p => !_sepRegexMath.IsMatch(p)).ToArray();
        }

        void CreateNodesForStatementsSeparated(string[] separateStatements) {
            foreach (string separateStatement in separateStatements) {
                _uniqueId++;
                var speakerListener = ExtractSpeakerAndActorFromStatement(separateStatement);
                _curTextNode = new TextNodeDataElement(_uniqueId, _currentDepth) {
                    IsSeparate = true
                };
                var cleanedStatement = CleanUpStatement(separateStatement);
                Message message = new (cleanedStatement,speakerListener.Item1, speakerListener.Item2 );
                _curTextNode.Message.Add(message);
                _nodes.Add(_curTextNode);
            }
        }

        (string, string) ExtractSpeakerAndActorFromStatement(string statement) {
            (string, string) result = new (string.Empty, string.Empty);
            var matches = _actorNamesRegexMatch.Matches(statement).ToArray();
            if (matches.Length >= 1) {
                string matchValue = matches[0].Value;
                result.Item1 = matchValue.Substring(1, matchValue.Length - 2);
            }
            if (matches.Length >= 2) {
                string matchValue = matches[1].Value;
                result.Item2 = matchValue.Substring(1, matchValue.Length - 2);
            }

            return result;
        }

        string CleanUpStatement(string statement) {
            string result = _remarksAndCommentsRegexMatch.Replace(statement, string.Empty);
            result = _markersRegexMatch.Replace(result, string.Empty);
            result = _actorNamesRegexMatch.Replace(result, string.Empty);
            return result.Trim();
        }

        void CreateNodesForStatementsStandard() {
            for (int i = 0; i < _statements.Length; i++) {
                string curStatement = _statements[i];
                string nextStatement = i + 1 == _statements.Length ? null : _statements[i + 1];
                HandleSingularStatement(i,curStatement, nextStatement);
            }
        }
        
        void HandleSingularStatement(int statementId, string statement, string nextStatement) {
            bool isRand = _randRegexMatch.IsMatch(statement);
            bool isChoice = _choiceRegexMatch.IsMatch(statement);
            bool isBookmark = _bookmarkRegexMatch.IsMatch(statement); 
            var speakerListener = ExtractSpeakerAndActorFromStatement(statement);
            var cleanedStatement = CleanUpStatement(statement);

            if (isBookmark) {
                    CreateBookmarkNodes(cleanedStatement);
                    CheckIfNewTextNodeNeeded(nextStatement);
            } else if (isChoice) {
                //Find existing choice node and add current statement as one of it's choices or create the new one.
                CreateChoiceNodeOrFindExisting(statement, cleanedStatement);
                //Usually Choice statements separate text nodes, so upon reaching choice statement create new TextNode.
                //For rare cases when choice follow up other choice do not create a TextNode yet.
                CheckIfNewTextNodeNeeded(nextStatement);
            } else if(_lastTextNodeDataElement.Message.Count == 0) {
                //If the last text node is empty, just add the message to it, making sure it's randomized status is correct.
                AddMessageToTheLastTextNode(cleanedStatement, speakerListener);
                _lastTextNodeDataElement.IsRandomized = isRand;
            }else if (_lastTextNodeDataElement.IsRandomized != isRand) {
                //If the last text node is not empty, but it's randomized status is different from the current statement,
                //set the quitNodeID of the last text node to the next node and create new text node.
                //add the message to the new text node, and set it's randomized status.
                _lastTextNodeDataElement.QuitNodeID = _uniqueId + 1;
                AddNewTextNode();
                AddMessageToTheLastTextNode(cleanedStatement, speakerListener);
                _lastTextNodeDataElement.IsRandomized = isRand;
            } else {
                //If the last text node is not empty, and it's randomized status is the same as the current statement,
                //just add the message to it.
                AddMessageToTheLastTextNode(cleanedStatement, speakerListener);
            }
            
            _wasLastStatementAChoice = isChoice;
            return;
            
            void AddMessageToTheLastTextNode(string cleanedStatement, (string, string) speakerListener) {
                Message message = new (cleanedStatement,speakerListener.Item1, speakerListener.Item2);
                _lastTextNodeDataElement.Message.Add(message);
            }
        }

        void CreateBookmarkNodes(string cleanedStatement) {
            var bookmarks = cleanedStatement.Split(',');
            int sharedQuitNodeID = _uniqueId + bookmarks.Length + 1;
            foreach (var bookmark in bookmarks) {
                _uniqueId++;
                _currentDepth = 0;
                var newBookmark = new BookmarkNodeDataElement(_uniqueId,_currentDepth, bookmark, sharedQuitNodeID);
                _nodes.Add(newBookmark);
                
            }
        }

        void CreateChoiceNodeOrFindExisting(string statement, string cleanedStatement) {
            //Get marker of the choice statement.
            string markerKey = _choiceRegexMatch.Match(statement).Value;

            var newChoiceData = new ChoiceData(cleanedStatement);

            //If the marker matches any existing choice node, just add new choice as another of it's choices.
            if (_choiceNodesByMarkers.ContainsKey(markerKey)) {
                _choiceNodesByMarkers[markerKey].Choices.Add(newChoiceData);
                //Reset current depth to the one of the found choice node.
                _currentDepth = _choiceNodesByMarkers[markerKey].Depth;
            } else {
                //If there are no choice nodes marked by given marker, create new choice node.
                //Increasing depth level and uniqueID variables first. 
                _currentDepth++;
                _uniqueId++;

                CreateNewChoiceNode(newChoiceData, markerKey);
            }

            //Set the quitNodeID variable of the newChoiceData now.
            //It will ensure that this choice will point to the NEXT Node, no matter of it's type or existence
            newChoiceData.QuitNodeID = _uniqueId + 1;
        }

        void CreateNewChoiceNode(ChoiceData newChoiceData, string markerKey) {
            var newChoiceNode = new ChoiceNodeDataElement(_uniqueId, _currentDepth);

            //If last statement was a choice as well, it means that last text node leads to nowhere. 
            if (!_wasLastStatementAChoice) {
                _lastTextNodeDataElement.QuitNodeID = _uniqueId;
            }

            newChoiceNode.Choices.Add(newChoiceData);
            _choiceNodesByMarkers.Add(markerKey, newChoiceNode);
            _nodes.Add(newChoiceNode);
        }

        void CheckIfNewTextNodeNeeded(string nextStatement) {
            if (nextStatement == null || _choiceRegexMatch.IsMatch(nextStatement)) {
                return;
            }
            AddNewTextNode();
        }

        void AddNewTextNode() {
            _uniqueId++;
            _currentDepth++;

            var newTextNode = new TextNodeDataElement(_uniqueId, _currentDepth);

            _nodes.Add(newTextNode);
            _lastTextNodeDataElement = newTextNode; 
        }

        void ConnectTheFirstNodeWithTheFirstChoice() {
            if (_choiceNodesByMarkers.Count <= 0) {
                return;
            }

            _curTextNode.QuitNodeID = _choiceNodesByMarkers.First().Value.ID;
        }
    }
}