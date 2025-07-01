using System;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Customs {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Adds action that is triggered after paying health cost.")]
    public class HealthCostActionAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, LocStringCategory(Category.Interaction)] LocString customInteractLabel;
        [SerializeField] float duration = 10f;
        [SerializeField] StatCost defaultCost;
        [SerializeField] bool modifyCostOnFlag;
        [SerializeField, ShowIf(nameof(modifyCostOnFlag))] FlagLogic flagLogic;
        [SerializeField, ShowIf(nameof(modifyCostOnFlag))] StatCost modifiedCost;
        [SerializeField] bool useHold;

        public bool UseHold => useHold;
        public string CustomInteractLabel => customInteractLabel;
        public float Duration => duration;
        public float TotalCost(LimitedStat stat) => modifyCostOnFlag && flagLogic.Get(false) ? modifiedCost.TotalCost(stat) : defaultCost.TotalCost(stat);
        
        public Element SpawnElement() {
            return new HealthCostAction();
        }

        public bool IsMine(Element element) {
            return element is HealthCostAction;
        }

        [Serializable]
        struct StatCost {
            [SerializeField] float flatStatCost;
            [SerializeField] float maxStatMultiplierCost;

            public float TotalCost(LimitedStat stat) {
                return flatStatCost + maxStatMultiplierCost * (stat?.UpperLimit ?? 0f);
            }
        }
    }
}