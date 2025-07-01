using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.AI.Barks {
    public class BarkSystem : IService {
        const float GlobalCriticalCooldown = 2f;
        const float GlobalImportantCooldown = 4f;
        const float GlobalNotImportantCooldown = 10f;
        const float GlobalIdleCooldown = 1;
        const float IdleDecayTime = 10f;
        const int MaxIdlesCount = 5;

        float _lastCriticalBark = float.MinValue;
        float _lastImportantBark = float.MinValue;
        float _lastNotImportantBark = float.MinValue;
        float _lastIdleBark = float.MinValue;
        readonly SinkingBuffer<float> _lastIdleTime = new (MaxIdlesCount);

        public float CurrentTime => Time.time;

        public bool CheckCooldown(BarkElement.BarkType barkType, float currentTime) {
            // check other barks/story
            if (World.HasAny<StoryOnTop>()) {
                return false;
            }

            // check cooldowns
            switch (barkType) {
                case BarkElement.BarkType.Critical when currentTime < _lastCriticalBark + GlobalImportantCooldown:
                case BarkElement.BarkType.Important when currentTime < _lastImportantBark + GlobalImportantCooldown:
                case BarkElement.BarkType.NotImportant when currentTime < _lastNotImportantBark + GlobalNotImportantCooldown:
                case BarkElement.BarkType.Idle when currentTime < _lastIdleBark + GlobalIdleCooldown:
                    return false;
            }

            if (barkType == BarkElement.BarkType.Idle) {
                if (currentTime < _lastIdleTime.Bottom + IdleDecayTime) {
                    return false;
                }
            }

            return true;
        }

        public void UpdateCooldown(BarkElement.BarkType type, float currentTime) {
            switch (type) {
                case BarkElement.BarkType.Critical:
                    _lastCriticalBark = currentTime;
                    break;
                case BarkElement.BarkType.Important:
                    _lastImportantBark = currentTime;
                    break;
                case BarkElement.BarkType.NotImportant:
                    _lastNotImportantBark = currentTime;
                    break;
                case BarkElement.BarkType.Idle:
                    _lastIdleBark = currentTime;
                    break;
            }
            if (type == BarkElement.BarkType.Idle) {
                _lastIdleTime.Push(currentTime);
            }
        }
    }
}