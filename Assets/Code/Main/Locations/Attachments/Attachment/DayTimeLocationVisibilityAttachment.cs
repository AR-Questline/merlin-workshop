using System;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Times;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Changes location visibility based on time of day.")]
    public class DayTimeLocationVisibilityAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] FlagLogic visibleFlag;
        [SerializeField, TableList] ARTimeOfDayInterval[] visibleTimes = Array.Empty<ARTimeOfDayInterval>();

        public ref readonly FlagLogic VisibleFlag => ref visibleFlag;
        public ARTimeOfDayInterval[] VisibleTimes => visibleTimes;
        
        public Element SpawnElement() => new DayTimeLocationVisibility();
        public bool IsMine(Element element) => element is DayTimeLocationVisibility;
    }
}