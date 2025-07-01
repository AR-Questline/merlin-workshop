using System.Collections.Generic;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Setup {
    public class AttachmentGroup : MonoBehaviour, IAttachmentGroup {
        [SerializeField] bool startEnabled = true;
        
        public string AttachGroupId => gameObject.name;
        public bool StartEnabled => startEnabled;
        
        public IEnumerable<IAttachmentSpec> GetAttachments() {
            PooledList<IAttachmentSpec>.Get(out var results);
            GetComponentsInChildren<IAttachmentSpec>(results);
            return results.value;
        } 
    }
}