using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Content {
    [UsesPrefab("CharacterSheet/Journal/Content/" + nameof(VJournalTutorialGraphicContent))]
    public class VJournalTutorialGraphicContent : View<JournalTutorialContent> {
        [SerializeField] Image image;
        [SerializeField] TMP_Text description;
        
        protected override void OnInitialize() {
            description.SetText(Target.Text);

            if (Target.Graphic is {IsSet: true}) {
                Target.Graphic.RegisterAndSetup(this, image);
            }
        }
    }
}
