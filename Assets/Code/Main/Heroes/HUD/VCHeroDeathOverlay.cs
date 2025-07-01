using Awaken.TG.MVC;
using System;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroDeathOverlay : ViewComponent<Hero> {
        [SerializeField] GameObject[] playerDiedOverlays = Array.Empty<GameObject>();
        
        protected override void OnAttach() {
            Target.ListenTo(Hero.Events.Died, Show, this);
            Target.ListenTo(Hero.Events.Revived, Hide, this);
            Hide();
        }

        void Show() {
            foreach (var image in playerDiedOverlays) {
                image.SetActive(true);
            }
        }

        void Hide() {
            foreach (var image in playerDiedOverlays) {
                image.SetActive(false);
            }
        }
    }
}
