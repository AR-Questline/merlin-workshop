namespace Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups {
    public class LargeLocationCullingGroup : LocationCullingGroup {
        const float LargeLocationMultiplier = 2;
        
        public static readonly float[] LargeLocationDistanceBands = {
            // 0
            4 * LargeLocationMultiplier, 
            // 1
            15 * LargeLocationMultiplier, 
            // 2
            50 * LargeLocationMultiplier, 
            // 3
            80 * LargeLocationMultiplier,
            // 4
            100 * LargeLocationMultiplier, 
            // 5
        };
        public LargeLocationCullingGroup() : base(LargeLocationDistanceBands, 0.05f) { }
    }
}