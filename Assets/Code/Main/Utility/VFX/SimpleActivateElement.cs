using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Utility.VFX {
    public partial class SimpleActivateElement : Element<Location>, IRefreshedByAttachment<SimpleActivateAttachment> {
        public override ushort TypeForSerialization => SavedModels.SimpleActivateElement;

        [Saved] bool _activated;
        GameObject[] _targetGameObjects;

        public void InitFromAttachment(SimpleActivateAttachment spec, bool isRestored) {
            _targetGameObjects = spec.targetGameObjects;
        }

        public void SetActive(bool active) {
            _activated = active;
            _targetGameObjects.ForEach(x => x.SetActive(active));
        }

        protected override void OnInitialize() {
            if (_targetGameObjects == null) {
                Log.Important?.Error($"Simple Activate Attachment {ParentModel.ViewParent.name} has no target gameObjects linked");
                Discard();
                return;
            }

            SetActive(_activated);
        }
    }
}