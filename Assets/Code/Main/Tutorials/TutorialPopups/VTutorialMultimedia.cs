using UnityEngine;

namespace Awaken.TG.Main.Tutorials.TutorialPopups {
    public abstract class VTutorialMultimedia<T> : VTutorialText<T> where T : TutorialText {
        [SerializeField] protected GameObject layoutHolder;
        [SerializeField] protected GameObject loadingIcon;
        
        protected override void OnInitialize() {
            base.OnInitialize();
            layoutHolder.SetActive(true);
            loadingIcon.SetActive(true);
        }
    }
}