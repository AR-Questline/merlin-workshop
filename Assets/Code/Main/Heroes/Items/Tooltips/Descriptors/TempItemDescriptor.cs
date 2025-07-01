using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors {
    /// <summary>
    /// IDisposable, remember to cleanup!
    /// </summary>
    public class TempItemDescriptor : ExistingItemDescriptor, IDisposable {
        readonly IEventListener _listener;
        
        public TempItemDescriptor(IRecipe recipe, ICrafting crafting, int? overridenLevel = null) : base(World.Add(recipe.Create(crafting, overridenLevel))) {
            _listener = crafting.ListenTo(Model.Events.AfterDiscarded, DiscardItem);
        }

        public TempItemDescriptor(IRecipe recipe, IModel owner) : base(World.Add(new Item(recipe.Outcome, recipe.Quantity))) {
            _listener = owner.ListenTo(Model.Events.AfterDiscarded, DiscardItem);
        }
        
        [UnityEngine.Scripting.Preserve]
        public TempItemDescriptor(ItemTemplate template, IModel owner) : base(World.Add(new Item(template))) {
            _listener = owner.ListenTo(Model.Events.AfterDiscarded, DiscardItem);
        }
        
        public TempItemDescriptor(ItemTemplate template, IModel owner, int quantity, int itemLevel, int weightLevel) : base(World.Add(new Item(template, quantity, itemLevel, weightLevel))) {
            _listener = owner.ListenTo(Model.Events.AfterDiscarded, DiscardItem);
        }

        public void Dispose() {
            World.EventSystem.RemoveListener(_listener);
            Item?.Discard();
        }

        void DiscardItem() {
            Item?.Discard();
        }
    }
}