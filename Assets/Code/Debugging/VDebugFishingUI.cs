using System.Collections.Generic;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Fishing;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Debugging {
    [UsesPrefab("CharacterSheet/Overview/" + nameof(VDebugFishingUI))]
    public class VDebugFishingUI : View<DebugFishingUI> {
        [SerializeField] TextMeshProUGUI info;

        public override Transform DetermineHost() => Hero.Current.View<VHeroHUD>().CenterBars;

        protected override void OnInitialize() {
            Hero.Current.ListenTo(DebugFishing.Events.OnDebugFishDataShow, DebugShowAvailableFish, this);
            Hero.Current.ListenTo(DebugFishing.Events.OnDebugFishDataHide, DebugHideAvailableFish, this);
        }

        void DebugShowAvailableFish(IEnumerable<IFishVolume> volumes) {
            bool containsGenericFishVolume = false;
            info.SetActiveAndText(true, string.Empty);
            
            foreach (var volume in volumes) {
                if (volume == null) {
                    continue;
                }
                
                if (volume is FishVolume fishVolume) {
                    var fishTable = fishVolume.AllFish;
                        
                    foreach (var fish in fishTable.entries) {
                        info.text += $"{fish.data.name} - {fish.occurrence.ToString()}\n";
                    }
                } else if (volume is GenericFishVolume) {
                    containsGenericFishVolume = true;
                }
            }

            if (containsGenericFishVolume) {
                info.text += "Generic Fish Volume\n";
            }
        }

        void DebugHideAvailableFish() {
            info.SetActiveAndText(false, string.Empty);
        }
    }
}
