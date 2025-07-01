using Awaken.TG.Code.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Statuses.Attachments {
    public class StatusWithChanceAttachment : MonoBehaviour {
        [SerializeField, Range(0f,1f)] float chanceToApply = 0.5f;
        
        public bool CanBeApplied => RandomUtil.WithProbability(chanceToApply); 
    }
}