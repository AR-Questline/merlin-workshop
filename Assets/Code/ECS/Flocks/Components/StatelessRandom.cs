using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    public static class StatelessRandom {
        public static bool IsRandomContinuousStateEnabled(float2 stateTimeMinMax, float stateChanceInv, double currentTime, uint stableHash) {
            var stateTime = GetRandomTime(stateTimeMinMax, stableHash);
            var stateValue = ((uint)(currentTime + stableHash) % (int)(stateTime * stateChanceInv));
            return (stateValue < stateTime);
        }
        
        public static bool IsRandomOneFrameStateEnabledInCurrentFrame(int2 delayInFramesMinMax, int currentFrame, uint stableHash) {
            var stateDelayInFrames = GetRandomFramesCount(delayInFramesMinMax, stableHash);
            return ((stableHash + currentFrame) % stateDelayInFrames) == 0;
        }

        public static float GetRandomTime(float2 timeMinMax, uint hash) {
            const float Inverse100 = 1f / 100;
            var t = (hash % 100) * Inverse100;
            var time = math.lerp(timeMinMax.x, timeMinMax.y, t);
            return math.max(time, 0.01f);
        }
        
        public static int GetRandomFramesCount(int2 framesMinMax, uint hash) {
            const float Inverse100 = 1f / 100;
            var t = (hash % 100) * Inverse100;
            var time = (int)math.lerp(framesMinMax.x, framesMinMax.y, t);
            return math.max(time, 1);
        }
    }
}