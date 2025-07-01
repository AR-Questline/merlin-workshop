using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemRead : Element<Item>, IItemAction, IRefreshedByAttachment<ItemReadSpec> {
        public override ushort TypeForSerialization => SavedModels.ItemRead;

        StoryBookmark _bookmark;
        StoryGraphRuntime _graph;
        
        public ItemActionType Type => ItemActionType.Read;

        public string StoryText {
            get {
                if (_graph.IsCreated == false) {
                    return string.Empty;
                }
                foreach (var chapter in _graph.chapters) {
                    foreach (var step in chapter.steps) {
                        if (step is SText text) {
                            return text.text;
                        }
                    }
                }
                return string.Empty;
            }
        }
        
        public IEnumerable<IRecipe> Recipes {
            get {
                if (_graph.IsCreated == false) {
                    return null;
                }
                var recipes = new HashSet<IRecipe>();
                foreach (var chapter in _graph.chapters) {
                    foreach (var step in chapter.steps) {
                        if (step is SLearnRecipe learnRecipe) {
                            recipes.Add(learnRecipe.recipe.Get<IRecipe>());
                        }
                    }
                }
                return recipes;
            }
        }

        public void InitFromAttachment(ItemReadSpec spec, bool isRestored) {
            if (StoryBookmark.ToInitialChapter(spec.StoryRef, out var bookmark)) {
                _bookmark = bookmark;
            }
        }

        protected override void OnInitialize() {
            if (_bookmark != null && _bookmark.IsValid) {
                var graph = StoryGraphRuntime.Get(_bookmark.GUID);
                if (graph.HasValue) {
                    _graph = graph.Value;
                }
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            _graph.Dispose();
        }

        public void Submit() {
            var config = StoryConfig.Base(_bookmark, typeof(VReadablePopupUI)).WithItem(ParentModel);
            Story.StartStory(config);
        }
        public void AfterPerformed() {}
        public void Perform() {}
        public void Cancel() {}
    }
}