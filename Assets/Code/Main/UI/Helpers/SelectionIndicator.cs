using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Selections;
using UnityEngine;

namespace Awaken.TG.Main.UI.Helpers
{
    public class SelectionIndicator : ViewComponent<Model> {

        // === Inspector properties

        [Range(0f, 120f)] public float rotationSpeed = 30f;

        // === Initialization

        protected override void OnAttach() {
            Target.ListenTo(Selection.Events.SelectionChanged, UpdateSelection, this);
            gameObject.SetActive(false);
        }

        // === Changing state

        public void UpdateSelection(SelectionChange e) => gameObject.SetActive(e.Selected);

        // === Unity lifecycle

        void Update() {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }
}