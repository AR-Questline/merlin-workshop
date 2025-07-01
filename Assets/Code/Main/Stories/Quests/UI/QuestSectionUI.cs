using System;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.UI {
    namespace Awaken.TG.Main.Stories.Quests.UI {
        [Serializable]
        public struct QuestSectionUI {
            [SerializeField] Transform sectionTransform;
            [SerializeField] QuestType questType;

            public Transform SectionTransform => sectionTransform;
            public QuestType QuestType => questType;
        }
    }
}