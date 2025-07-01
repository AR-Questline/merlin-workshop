using System;

namespace Awaken.TG.Main.Heroes.Combat {
    [Serializable]
    public class AimAssistData {
        public float minAssistSpeed = 0f;
        public float maxAssistSpeed = 2f;
        public float distanceUntilSpeedDrops = 40f;
        public float assistLongRange = 40f;
        public float assistShortRange = 15f;
        public float softAssistOnEnemyMultiplier = 0.5f;
        public float maxAngleThatSlowsAssist = 10f;
        public float narrowAngleMinMultiplier = 0.1f;
        public float angleToStopTrackingRadians = 0.25f;
    }
}
