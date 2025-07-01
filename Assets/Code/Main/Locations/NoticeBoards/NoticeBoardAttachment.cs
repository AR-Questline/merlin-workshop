using System;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Locations.NoticeBoards {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Notice Board management in the location.")]
    public class NoticeBoardAttachment : MonoBehaviour, IAttachmentSpec {
        public NoticeBoard.NoticeQueue[] notices = Array.Empty<NoticeBoard.NoticeQueue>();
        
        public Element SpawnElement() => new NoticeBoard();
        public bool IsMine(Element element) => element is NoticeBoard;

        void OnDrawGizmos() {
            var previousMatrix = Gizmos.matrix;
            foreach (ref var notice in notices.RefIterator()) {
                if (notice.place) {
                    Gizmos.matrix = notice.place.localToWorldMatrix;
                    Gizmos.DrawCube(Vector3.zero, new Vector3(0.5f, 0.1f, 0.3f));
                }
            }
            Gizmos.matrix = previousMatrix;
        }
    }
}