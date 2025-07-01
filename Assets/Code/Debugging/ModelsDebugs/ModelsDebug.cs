using System;
using System.Collections.Generic;
using Awaken.TG.Debugging.ModelsDebugs.Inspectors;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Collections;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs {
    public partial class ModelsDebug {
        public static readonly char[] SearchSeparators = { ' ', ',' };

        OnDemandCache<object, MembersList> _additionalMembersCache = new(o => new(o));
        HashSet<object> _callStack = new();

        IModelsDebugNavigator _navigator;
        Vector2 _scroll;
        MembersList _lastSelection;
        bool _inGame;
        string _fullSearchContext;

        public void Init(bool inGame) {
            _inGame = inGame;
            _navigator = ModelsDebugNavigatorFactory.Get(inGame);
            MembersList.BuildCache();
        }
        
        public void RefreshNavigation() => _navigator.RefreshNavigation();

        public void Draw() {
            if (_inGame) {
                TGGUILayout.BeginGUILayout();
            }
            TGGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            DrawNavigation();
            DrawSelection();

            TGGUILayout.EndHorizontal();
            if (_inGame) {
                TGGUILayout.EndGUILayout();
            }
        }

        public void DrawObject(object objectToDraw, string[] searchContext, int searchHash, bool withMethods) {
            var membersList = _additionalMembersCache[objectToDraw];
            if (_callStack.Contains(objectToDraw) && objectToDraw != null) {
                GUILayout.Label($"<color=red>Recursive member {membersList.Name}</color>:", MemberListItemInspector.LabelStyle);
                return;
            }

            _callStack.Add(objectToDraw);
            TGGUILayout.BeginIndent();
            DrawMember(membersList, searchContext, searchHash, withMethods);
            TGGUILayout.EndIndent();
            _callStack.Remove(objectToDraw);
        }

        void DrawNavigation() {
            TGGUILayout.BeginVertical();
            using (var changeScope = new TGGUILayout.CheckChangeScope()) {
                _navigator.SearchString = TGGUILayout.TextField("Search", _navigator.SearchString, GUILayout.Width(350));
                if (changeScope) {
                    if (string.IsNullOrWhiteSpace(_navigator.SearchString) && _navigator.GetSelected() != null) {
                        _navigator.Frame();
                    }
                }
            }

            Rect treeRect = new Rect(0, 0, 350, 0);
            if (!_inGame) {
                treeRect = GUILayoutUtility.GetRect(new GUIContent(""), GUIStyle.none, GUILayout.Width(350), GUILayout.ExpandHeight(true));
            }
            
            _navigator.Draw(treeRect);
            TGGUILayout.EndVertical();
        }
        
        void DrawSelection() {
            _scroll = TGGUILayout.BeginScrollView(_scroll, true, true, GUILayout.ExpandWidth(true));
            _fullSearchContext = TGGUILayout.TextField("Search member", _fullSearchContext, GUILayout.Width(400));
            var searchContext = string.IsNullOrWhiteSpace(_fullSearchContext) ?
                Array.Empty<string>() :
                _fullSearchContext.Split(SearchSeparators, StringSplitOptions.RemoveEmptyEntries);
            var searchHash = string.IsNullOrWhiteSpace(_fullSearchContext) ?
                -1 :
                _fullSearchContext.GetHashCode();
            
            MembersList selectedObject = _navigator.GetSelected();
            if (_lastSelection != selectedObject) {
                _scroll = Vector2.zero;
                _lastSelection = selectedObject;
            }

            DrawMember(selectedObject, searchContext, searchHash, true);
            TGGUILayout.EndScrollView();
        }

        void DrawMember(MembersList selectedObject, string[] searchContext, int searchHash, bool withMethods) {
            if (selectedObject != null) {
                //_treeView.FrameItem(selectedId);
                GUILayout.Label($"{selectedObject.Name}", GUILayout.ExpandWidth(true));
                foreach (var member in selectedObject.Items) {
                    DrawField(member, selectedObject.RelatedObject, searchContext, searchHash);
                }
                if (!withMethods) {
                    return;
                }
                GUILayout.Space(8);
                GUILayout.Label("Methods:", GUILayout.ExpandWidth(true));
                foreach (var method in selectedObject.Methods) {
                    DrawMethod(method, selectedObject.RelatedObject, searchContext);
                }
            }
        }

        void DrawField(MembersListItem member, object target, string[] searchContext, int searchHash) {
            if (!member.CanObtainValue) {
                GUILayout.Label($"<color=lightblue>{member.Name}</color>: <color=red>Cannot obtain value</color>", MemberListItemInspector.LabelStyle);
                return;
            }
            var memberValue = member.Value(target);
            MemberListItemInspector.GetInspector(member, memberValue, target)
                .Draw(member, memberValue, target, this, searchContext, searchHash);
        }

        void DrawMethod(MethodMemberListItem method, object target, string[] searchContext) {
            MemberListItemInspector.MethodInspector.Draw(method, target, searchContext);
        }
        
        // === Operations
        public void SetSearchId(string modelId) { 
            _navigator.SearchString = modelId;
        }

        public void SetSelectedId(string modelID) {
            if (!_navigator.TrySelectModel(modelID)) {
                SetSearchId(ModelUtils.GetSimplifiedModelId(modelID));
            }
        }

        public partial class DebugListenerOwner : Model {
            public override Domain DefaultDomain => Domain.Globals;
            public sealed override bool IsNotSaved => true;
        }
    }
}