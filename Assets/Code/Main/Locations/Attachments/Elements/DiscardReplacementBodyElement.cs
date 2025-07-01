using Awaken.Utility;
using System;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class DiscardReplacementBodyElement : Element<Location> {
        public override ushort TypeForSerialization => SavedModels.DiscardReplacementBodyElement;

        const uint DiscardOverBand = 3;
        const uint MinimumLifetimeInHours = 12;

        [Saved] DateTime _discardDate;
        bool _searchActionCondition;
        bool _timeCondition;
        bool _beingDiscarded;
        
        SearchAction _searchAction;
        SearchAction SearchAction => _searchAction ??= ParentModel.TryGetElement<SearchAction>();

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public DiscardReplacementBodyElement() { }

        protected override void OnInitialize() {
            _discardDate = World.Any<GameRealTime>().WeatherTime.Date.AddHours(MinimumLifetimeInHours);
            ParentModel.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, OnDistanceBandChanged, this);
            OnDistanceBandChanged(ParentModel.GetCurrentBandSafe(0));
        }

        protected override void OnRestore() {
            ParentModel.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, OnDistanceBandChanged, this);
            OnDistanceBandChanged(ParentModel.GetCurrentBandSafe(0));
        }

        void OnDistanceBandChanged(int band) {
            if (band > DiscardOverBand) {
                TryDiscard().Forget();
            }
        }

        bool ImportantItemsCondition() {
            if (_searchActionCondition) {
                return true;
            }
            
            if (SearchAction == null) {
                _searchActionCondition = true;
                return true;
            }

            foreach (var template in SearchAction.AvailableTemplates) {
                if (template.IsImportantItem) {
                    return false;
                }
            }

            _searchActionCondition = true;
            return true;
        }

        bool TimeCondition() {
            if (_timeCondition) {
                return true;
            }
            
            var currentDate = World.Any<GameRealTime>().WeatherTime.Date;
            if (currentDate > _discardDate) {
                _timeCondition = true;
                return true;
            }

            return false;
        }

        async UniTaskVoid TryDiscard() {
            if (_beingDiscarded) return;
            if (ImportantItemsCondition() && TimeCondition()) {
                _beingDiscarded = true;
                // Do not discard during initialization
                if (await AsyncUtil.DelayFrame(this)) {
                    ParentModel.Discard();
                }
            }
        }
    }
}