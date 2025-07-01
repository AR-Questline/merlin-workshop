namespace Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups {
    public class RegrowableCullingGroup : BaseCullingGroup {
        static readonly float[] DistanceBands = {
            150,
        };

        public RegrowableCullingGroup() : base(DistanceBands, 0) { }
    }
}
