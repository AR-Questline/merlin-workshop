namespace Awaken.CommonInterfaces {
    public interface IWithOcclusionCullingTarget {
        static IRevertOcclusion TargetRevertDummy { get; } = new WithOcclusionCullingTargetRevertDummy(); 
        
        IRevertOcclusion EnterOcclusionCulling();
        
        public interface IRevertOcclusion {
            void Revert();
        }
    }

    public class WithOcclusionCullingTargetRevertDummy : IWithOcclusionCullingTarget.IRevertOcclusion {
        public void Revert() {}
    }
}
