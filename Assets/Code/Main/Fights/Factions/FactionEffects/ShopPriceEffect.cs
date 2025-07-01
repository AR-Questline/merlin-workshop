using System;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Fights.Factions.FactionEffects {
    
    [Serializable]
    public class ShopPriceEffect : ReputationEffect {
        [Range(-100, 100), Tooltip("Values below zero decrease base price; values above zero increase base price")] 
        public int discountPercentage = 0;
        
        IEventListener _openShopListener;
        IEventListener _closeShopListener;
        StatTweak _priceTweak;

        public override void Init(FactionTemplate factionTemplate) {
            _factionTemplate = factionTemplate;
            _openShopListener = World.EventSystem.ListenTo(EventSelector.AnySource, Shop.Events.ShopOpened, ApplyDiscount);
            _closeShopListener = World.EventSystem.ListenTo(EventSelector.AnySource, Shop.Events.ShopClosed, DiscardDiscount);
        }
        
        void ApplyDiscount(Shop shop) {
            if (_priceTweak != null) {
               Log.Important?.Error("Shop price effect already applied! This shouldn't happen.");
                return;
            }
            
            var npcElement = shop.ParentModel.TryGetElement<NpcElement>();
            bool isProperFaction = npcElement?.Faction.Template == _factionTemplate;
            if (!isProperFaction || discountPercentage == 0) {
                return;
            }
            
            float discountModifier = discountPercentage / 100f;

            float priceModifier = 1 + discountModifier;
            _priceTweak = StatTweak.Multi(shop.MerchantStats.SellModifier, priceModifier, parentModel: shop);
            _priceTweak.MarkedNotSaved = true;
        }

        void DiscardDiscount() {
            _priceTweak?.Discard();
            _priceTweak = null;
        }

        public override void Deinit() {
            _factionTemplate = null;
            
            if (_closeShopListener != null) {
                World.EventSystem.RemoveListener(_closeShopListener);
                _closeShopListener = null;
            }

            if (_openShopListener != null) {
                World.EventSystem.RemoveListener(_openShopListener);
                _openShopListener = null;
            }
            
            DiscardDiscount();
        }
    }
}