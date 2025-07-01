using System.Collections.Generic;
using CrazyMinnow.SALSA;
using UnityEngine;

namespace Awaken.VendorWrappers.Salsa {
    public class QueueProcessor : MonoBehaviour {
        public List<QueueProcessor.QueueOoO> queueOrderOfOperation = new List<QueueProcessor.QueueOoO>();
        private bool isQueueOrderOfOperationInitialized;
        private Dictionary<int, int> lastWriteHierarchyDictionary = new Dictionary<int, int>();
        public List<QueueData> baseQueue = new List<QueueData>();
        public List<QueueData> priorityQueueHeads = new List<QueueData>();
        public List<QueueData> priorityQueueEyes = new List<QueueData>();
        public List<QueueData> priorityQueueLids = new List<QueueData>();
        public List<QueueData> priorityQueueEmotes = new List<QueueData>();
        public List<QueueData> priorityQueueLipsync = new List<QueueData>();
        public List<QueueData> priorityQueueBlinks = new List<QueueData>();
        public bool useMergeWithInfluencer = true;
        public bool ignoreScaledTime;
        private QueueProcessor.QueueProcessorNotificationArgs _eventNotificationArgs;
        [Range(100f, 500f)]
        public float inspHeight = 150f;
        public bool inspShowHeads = true;
        public bool inspShowEmote = true;
        public bool inspShowEyes = true;
        public bool inspShowLids = true;
        public bool inspShowLipsync = true;
        public bool inspShowBlinks = true;
        
        public class QueueOoO
        {
            public string name;
            public int hierarchyLevel;
            public List<QueueData> queue;
            public Queue<int> trailingMaxCount = new Queue<int>();
            public int maxQueueCount;
            public int minQueueCount;
        }

        public class QueueProcessorNotificationArgs
        {
            public QueueProcessor queueProcesor;
        }
    }
}