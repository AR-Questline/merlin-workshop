using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;
using DG.Tweening;

namespace Awaken.TG.Main.Locations.Containers {
    [UsesPrefab("Locations/VContainerItems")]
    public class VContainerTransferItems : View<ContainerTransferItems> {
        [UnityEngine.Scripting.Preserve] public Transform itemParent;
        public TextMeshProUGUI weightCap;
        Sequence _weightCapReachedSequence;

        public override Transform DetermineHost() => Target.ParentModel.View<VTransferItems>().containerParent;

        protected override void OnInitialize() {
            Target.ContainerUI.ListenTo(TransferItems.Events.WeightCapReached, WeightCapReached, this);
            Target.ContainerUI.ListenTo(Model.Events.AfterChanged, RefreshWeightCap, this);
            Target.ListenTo(Model.Events.AfterChanged, RefreshWeightCap, this);
            RefreshWeightCap();
        }

        void RefreshWeightCap() {
            weightCap.text = $"{Target.ContainerUI.CurrentWeight}/{ContainerUI.WeightCap}";
        }

        void WeightCapReached() {
            _weightCapReachedSequence?.Complete(true);
            Color originalColor = weightCap.color;
            _weightCapReachedSequence = DOTween.Sequence().SetUpdate(true);
            _weightCapReachedSequence.Append(DOTween.To(() => weightCap.color, c => weightCap.color = c, Color.red, 0.5f));
            _weightCapReachedSequence.Append(DOTween.To(() => weightCap.color, c => weightCap.color = c, originalColor, 0.5f));
        }
    }
}