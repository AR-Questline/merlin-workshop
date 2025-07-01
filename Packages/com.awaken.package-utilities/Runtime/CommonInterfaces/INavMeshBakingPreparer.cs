using JetBrains.Annotations;

namespace Awaken.CommonInterfaces {
    public interface INavMeshBakingPreparer {
        [CanBeNull] IReversible Prepare();
        
        public interface IReversible {
            void Revert();
        }
    }
}