using Awaken.TG.Main.Heroes.Stats.Controls;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using UnityEngine;

namespace Awaken.TG.Main.Scenes.SceneConstructors.MapAttachments {
    public class TutorialMapAttachment : MonoBehaviour, IMapAttachment {
        public void Init() {
            ModelUtils.GetSingletonModel(() => new ProficiencyGainBlockerModel());
            
            World.Add(new SaveBlocker(nameof(TutorialMapAttachment)));
            
            var loadBlocker = World.Add(new LoadBlocker());
            loadBlocker.MoveToDomain(Domain.CurrentScene());
        }
    }
}