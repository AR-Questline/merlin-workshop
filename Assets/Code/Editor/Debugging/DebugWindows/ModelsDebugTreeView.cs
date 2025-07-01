using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.ModelsDebugs;
using Awaken.TG.Debugging.ModelsDebugs.Runtime;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEditor.IMGUI.Controls;

namespace Awaken.TG.Editor.Debugging.DebugWindows {
    public class ModelsDebugTreeView : TreeView {
        // === Fields
        int _id;
        Dictionary<object, int> _indexByItem = new Dictionary<object, int>();
        Dictionary<string, int> _indexByID = new Dictionary<string, int>();

        // === Construction
        public ModelsDebugTreeView(TreeViewState state) : base(state) {
            Reload();
        }

        protected override TreeViewItem BuildRoot() {
            _id = 0;

            // Add main root 
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};

            // Add models part
            BuildModels(root);

            TreeViewItem templatesRoot = GetTemplateTypesTree();
            root.AddChild(templatesRoot);

            // Add services root
            var servicesRoot = new TreeViewItem {id = ++_id, displayName = "Services"};
            root.AddChild(servicesRoot);
            
            // Add services
            BuildServices(servicesRoot);

            SetupDepthsFromParentsAndChildren(root);

            return root;
        }

        // === Find objects
        public MembersList FindObject(int treeId) {
            var item = FindItem(treeId, rootItem);
            if (item is ModelsDebugTreeViewItem modelTreeViewItem ) {
                return modelTreeViewItem.MembersList;
            }
            return null;
        }
        
        public int FindIndex(object entity) {
            return _indexByItem.TryGetValue(entity, out int index) ? index : -1;
        }
        
        public int FindIndex(string id) {
            return _indexByID.TryGetValue(id, out int index) ? index : -1;
        }

        // === Build tree
        void BuildModels(TreeViewItem root) {
            _indexByItem.Clear();
            _indexByID.Clear();
            
            // Collect root models
            var rootModels = World
                .AllInOrder()
                .Where(m => m is not Element && m.IsInitialized)
                .ToArray();

            Array.Sort(rootModels, NameComparision);

            var uniqueRootsTypes = rootModels
                .GroupBy(m => m.GetType())
                .Where(g => g.Count() == 1)
                .Select(g => g.Key).ToHashSet();
            
            Dictionary<Type, TreeViewItem> type2Item = new Dictionary<Type, TreeViewItem>();

            foreach (Model model in rootModels) {
                var type = model.GetType();
                if (uniqueRootsTypes.Contains(type)) {
                    var rootModelItem = new ModelsDebugTreeViewItem(++_id, new MembersList(model));
                    _indexByItem.Add(model, _id);
                    _indexByID.Add(model.ID, _id);
                    root.AddChild(rootModelItem);
                    // Setup all model children
                    AddElements(rootModelItem, model);
                    continue;
                }
                
                // Add model type as root
                if (!type2Item.TryGetValue(type, out var typeRoot)) {
                    typeRoot = new TreeViewItem {id = ++_id, displayName = type.Name};
                    root.AddChild(typeRoot);
                    type2Item.Add(type, typeRoot);
                }

                // Setup model as type root child
                var modelItem = new ModelsDebugTreeViewItem(++_id, new MembersList(model));
                _indexByItem.Add(model, _id);
                _indexByID.Add(model.ID, _id);
                typeRoot.AddChild(modelItem);
                typeRoot.displayName = $"{type.Name} ({typeRoot.children.Count})";
                // Setup all model children
                AddElements(modelItem, model);
            }
        }

        void AddElements(TreeViewItem modelItem, Model model) {
            var elements = model.AllElements().ToArray();
            Array.Sort(elements, NameComparision);
            foreach (Element element in elements) {
                if (!element.IsInitialized) {
                    continue;
                }
                // Add child to parent model
                var elementItem = new ModelsDebugTreeViewItem(++_id, new MembersList(element));
                _indexByItem.Add(element, _id);
                _indexByID.Add(element.ID, _id);
                modelItem.AddChild(elementItem);
                if (modelItem is ModelsDebugTreeViewItem modelsTreeViewItem) {
                    modelsTreeViewItem.Count = modelItem.children.Count;
                }
                // Setup all model children
                AddElements(elementItem, element);
            }
        }

        void BuildServices(TreeViewItem servicesRoot) {
            var services = World.Services.All().Distinct();
            foreach (object service in services) {
                servicesRoot.AddChild(new ModelsDebugTreeViewItem(++_id, new MembersList(service)));
                _indexByItem.Add(service, _id);
            }

            servicesRoot.displayName += $" ({servicesRoot.children.Count})";
        }
        
        TreeViewItem GetTemplateTypesTree() {
            var root = new TreeViewItem() { id=++_id, displayName = "Templates"};
            Dictionary<Type, TreeViewItem> typeTreeItems = new() {
                {RuntimeTreeView.TemplateType, root}
            };
            foreach (Type templateType in RuntimeTreeView.TemplateTypes) {
                GetOrCreateTemplateTreeItem(templateType, typeTreeItems, root);
            }

            var templatesOfType = new List<ITemplate>(32);
            foreach (KeyValuePair<Type, TreeViewItem> typeItemPair in typeTreeItems) {
                templatesOfType.Clear();
                TemplatesSearcher.FindAllOfType(typeItemPair.Key, templatesOfType, true);
                foreach (ITemplate template in templatesOfType) {
                    var templateItem = new ModelsDebugTreeViewItem(++_id, new MembersList(template));
                    typeItemPair.Value.AddChild(templateItem);
                    _indexByItem.Add(template, _id);
                }
                string count = typeItemPair.Value.children != null ? $" ({typeItemPair.Value.children.Count})" : "";
                typeItemPair.Value.displayName = $"[{typeItemPair.Key.Name}]{count}";
            }

            return root;
        }

        TreeViewItem GetOrCreateTemplateTreeItem(Type templateType, Dictionary<Type, TreeViewItem> typeTreeItems, TreeViewItem root) {
            if (!typeTreeItems.TryGetValue(templateType, out TreeViewItem treeItem)) {
                treeItem = new TreeViewItem() { id = ++_id, displayName = $"[{templateType.Name}]"};
                TreeViewItem parent;
                if (RuntimeTreeView.TemplateType.IsAssignableFrom(templateType.BaseType)) {
                    parent = GetOrCreateTemplateTreeItem(templateType.BaseType, typeTreeItems, root);
                } else {
                    parent = root;
                }
                parent.AddChild(treeItem);
                typeTreeItems.Add(templateType, treeItem);
            }

            return treeItem;
        }

        static int NameComparision(Model left, Model right) {
            int typeCompare = string.Compare(left.GetType().Name, right.GetType().Name, StringComparison.Ordinal);
            if (typeCompare != 0) {
                return typeCompare;
            }

            int leftIdLength = left.ID.Length;
            int rightIdLength = right.ID.Length;

            if (leftIdLength < rightIdLength) {
                return -1;
            } else if (leftIdLength > rightIdLength) {
                return 1;
            } else {
                return string.Compare(left.ID, right.ID, StringComparison.Ordinal);
            }
        }
    }
}