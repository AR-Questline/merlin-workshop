using System.Collections.Generic;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    [NoPrefab]
    public class VPortal : View<Portal> {
        bool _inUse; //Hero can trigger portal multiple times, it breaks Transition Service.
        protected override bool ShouldDestroyGameObjectOnDiscard => false;
        
        protected override void OnInitialize() {
            Target.ParentModel.OnVisualLoaded(OnVisualLoaded);
        }

        void OnVisualLoaded(Transform parentTransform) {
            foreach (Collider col in parentTransform.GetComponentsInChildren<Collider>(true)) {
                if (col.gameObject == gameObject) {
                    continue;
                }
                if (col.isTrigger) {
                    PortalTrigger portalTrigger = col.AddComponent<PortalTrigger>();
                    portalTrigger.Init(this);
                }
            }
        }

        public void OnTriggerEnter(Collider other) {
            if (!_inUse && Target.IsFrom && Target.ParentModel.Interactable) {
                VHeroController heroController = other.GetComponentInParent<VHeroController>();
                if (heroController != null) {
                    _inUse = true;
                    Target.Execute(heroController.Target);
                    Hero.Current.ListenToLimited(Hero.Events.ArrivedAtPortal, AllowNextTrigger, this);
                }
            }
        }

        void AllowNextTrigger() {
            _inUse = false;
        }
    }
}