using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Runtime {
    public class RuntimeTreeView {
        public const int MaxSearchItems = 150;
        public static readonly Type TemplateType = typeof(ITemplate);
        public static readonly Type[] TemplateTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(t => TemplateType.IsAssignableFrom(t))
            .ToArray();
        
        Vector2 _scroll;

        Dictionary<string, MembersList> _itemById = new Dictionary<string, MembersList>();
        Dictionary<MembersList, string> _idByItem = new Dictionary<MembersList, string>();
        string _selectedID;
        RuntimeTreeItem _root;
        string _searchString;
        float _nextSearchUpdate;
        string _delayedSearchString;

        public string SearchString {
            get => _searchString;
            set {
                if (_searchString != value) {
                    _searchString = value;
                    _nextSearchUpdate = Time.unscaledTime + 1.5f;
                }
            }
        }

        public MembersList SelectedMember {
            get {
                if (string.IsNullOrWhiteSpace(_selectedID)) {
                    return null;
                }
                return _itemById.TryGetValue(_selectedID, out var member) ? member : null;
            }
            set {
                if (value == null || !_idByItem.TryGetValue(value, out _selectedID)) {
                    _selectedID = string.Empty;
                }
            }
        }

        public int SearchDisplayedItems { get; set; }

        public RuntimeTreeView() {
            Reload();
        }

        public void Reload() {
            // Add main root 
            _root = new RuntimeTreeItem(this) {displayName = "Root", isRoot = true};

            // Add models part
            BuildModels();

            RuntimeTreeItem templatesRoot = GetTemplateTypesTree();
            _root.AddChild(templatesRoot);
            
            // Add services root
            RuntimeTreeItem servicesRoot = new RuntimeTreeItem(this) {displayName = "Services"};
            _root.AddChild(servicesRoot);
            
            // Add services
            BuildServices(servicesRoot);
        }
        
        public void Draw(Rect rect) {
            TryUpdateSearchText();
            _root.FocusSelected();
            
            _scroll = TGGUILayout.BeginScrollView(_scroll, false, false, GUILayout.Width(rect.width), GUILayout.ExpandHeight(true));
            SearchDisplayedItems = 0;
            _root.Draw(_delayedSearchString);
            TGGUILayout.EndScrollView();
        }

        async void ScrollToMemberList(MembersList membersList) {
            await UniTask.DelayFrame(1);
            _scroll.y = membersList.ScrollY;
        }

        void TryUpdateSearchText() {
            if (!(Time.unscaledTime >= _nextSearchUpdate)) {
                return;
            }
            _delayedSearchString = _searchString;
            _nextSearchUpdate = float.MaxValue;
        }

        // === Build tree
        void BuildModels() {
            _itemById.Clear();
            _idByItem.Clear();

            // Collect root models
            var rootModels = World
                .AllInOrder()
                .Where(m => !(m is Element) && m.IsInitialized)
                .OrderBy(m => m.GetType().Name).ToArray();

            var uniqueRootsTypes = rootModels
                .GroupBy(m => m.GetType())
                .Where(g => g.Count() == 1)
                .Select(g => g.Key).ToHashSet();
            
            Dictionary<Type, RuntimeTreeItem> type2Item = new Dictionary<Type, RuntimeTreeItem>();

            foreach (Model model in rootModels) {
                var type = model.GetType();
                if (uniqueRootsTypes.Contains(type)) {
                    var rootModelItem = new RuntimeTreeItem(this, new MembersList(model));
                    _itemById.Add(model.ID, rootModelItem.MembersList);
                    _idByItem.Add(rootModelItem.MembersList, model.ID);
                    _root.AddChild(rootModelItem);
                    // Setup all model children
                    AddElements(rootModelItem, model);
                    continue;
                }
                
                // Add model type as root
                if (!type2Item.TryGetValue(type, out var typeRoot)) {
                    typeRoot = new RuntimeTreeItem(this) {displayName = type.Name};
                    _root.AddChild(typeRoot);
                    type2Item.Add(type, typeRoot);
                }

                // Setup model as type root child
                var modelItem = new RuntimeTreeItem(this, new MembersList(model));
                _itemById.Add(model.ID, modelItem.MembersList);
                _idByItem.Add(modelItem.MembersList, model.ID);
                typeRoot.AddChild(modelItem);
                // Setup all model children
                AddElements(modelItem, model);
            }
        }

        void AddElements(RuntimeTreeItem modelItem, Model model) {
            foreach (Element element in model.AllElements().OrderBy(e => e.GetType().Name)) {
                if (!element.IsInitialized) {
                    continue;
                }
                // Add child to parent model
                var elementItem = new RuntimeTreeItem(this, new MembersList(element));
                _itemById.Add(element.ID, elementItem.MembersList);
                _idByItem.Add(elementItem.MembersList, element.ID);
                modelItem.AddChild(elementItem);
                // Setup all model children
                AddElements(elementItem, element);
            }
        }

        void BuildServices(RuntimeTreeItem servicesRoot) {
            var services = World.Services.All();
            foreach (object service in services) {
                var serviceItem = new RuntimeTreeItem(this, new MembersList(service));
                servicesRoot.AddChild(serviceItem);
                _itemById.Add(serviceItem.displayName, serviceItem.MembersList);
                _idByItem.Add(serviceItem.MembersList, serviceItem.displayName);
            }
        }

        public bool TrySelect(string modelID) {
            if (_itemById.TryGetValue(modelID, out var newSelection)) {
                SelectedMember = newSelection;
                ScrollToMemberList(newSelection);
                return true;
            }
            return false;
        }

        RuntimeTreeItem GetTemplateTypesTree() {
            var root = new RuntimeTreeItem(this) {displayName = "Templates"};
            Dictionary<Type, RuntimeTreeItem> typeTreeItems = new() {
                {TemplateType, root}
            };
            foreach (Type templateType in TemplateTypes) {
                GetOrCreateTemplateTreeItem(templateType, typeTreeItems, root);
            }

            var templatesProvider = World.Services.Get<TemplatesProvider>();
            foreach (KeyValuePair<Type,RuntimeTreeItem> typeItemPair in typeTreeItems) {
                foreach (ITemplate template in templatesProvider.GetAllOfType(typeItemPair.Key, TemplateTypeFlag.All)) {
                    var templateItem = new RuntimeTreeItem(this, new MembersList(template));
                    typeItemPair.Value.AddChild(templateItem);
                    _itemById.Add(templateItem.displayName, templateItem.MembersList);
                    _idByItem.Add(templateItem.MembersList, templateItem.displayName);
                }
            }

            return root;
        }

        RuntimeTreeItem GetOrCreateTemplateTreeItem(Type templateType, Dictionary<Type, RuntimeTreeItem> typeTreeItems, RuntimeTreeItem root) {
            if (!typeTreeItems.TryGetValue(templateType, out RuntimeTreeItem treeItem)) {
                treeItem = new RuntimeTreeItem(this) { displayName = $"[{templateType.Name}]"};
                RuntimeTreeItem parent;
                if (TemplateType.IsAssignableFrom(templateType.BaseType)) {
                    parent = GetOrCreateTemplateTreeItem(templateType.BaseType, typeTreeItems, root);
                } else {
                    parent = root;
                }
                parent.AddChild(treeItem);
                typeTreeItems.Add(templateType, treeItem);
            }

            return treeItem;
        }
    }
}