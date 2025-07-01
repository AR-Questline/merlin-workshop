using System.Threading;

/// <summary>Synchronization primitives</summary>
namespace Pathfinding.Sync {
	/// <summary>
	/// Spin lock which can be used in Burst.
	/// Good when the lock is generally uncontested.
	/// Very inefficient when the lock is contested.
	/// </summary>
	internal struct SpinLock {
		private volatile int locked;

		public void Lock () {
        }

        public void Unlock () {
        }
    }
}
