using Awaken.TG.MVC.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Tutorials.TutorialPopups {
    [UsesPrefab("UI/Tutorials/" + nameof(VTutorialGraphic))]
    public class VTutorialGraphic : VTutorialMultimedia<TutorialGraphic> {
        [SerializeField] Image image;
        
        protected override void OnInitialize() {
            base.OnInitialize();
            if (Target.SpriteReference is {IsSet: true}) {
                Target.SpriteReference.RegisterAndSetup(this, image);
                loadingIcon.SetActive(false);
            }
        }
    }
}