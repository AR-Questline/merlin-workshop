using UnityEngine;

namespace Pathfinding {
	/// <summary>
	/// Base for all path modifiers.
	/// See: MonoModifier
	/// Modifier
	/// </summary>
	public interface IPathModifier {
		int Order { get; }

		void Apply(Path path);
		void PreProcess(Path path);
	}

	/// <summary>
	/// Base class for path modifiers which are not attached to GameObjects.
	/// See: MonoModifier
	/// </summary>
	[System.Serializable]
	public abstract class PathModifier : IPathModifier {
		[System.NonSerialized]
		public Seeker seeker;

		/// <summary>
		/// Modifiers will be executed from lower order to higher order.
		/// This value is assumed to stay constant.
		/// </summary>
		public abstract int Order { get; }

		public void Awake (Seeker seeker)
        {
        }

        public void OnDestroy(Seeker seeker)
        {
        }

        public virtual void PreProcess(Path path)
        {
        }

        /// <summary>Main Post-Processing function</summary>
        public abstract void Apply(Path path);
    }

    /// <summary>
    /// Base class for path modifiers which can be attached to GameObjects.
    /// See: Menubar -> Component -> Pathfinding -> Modifiers
    /// </summary>
    [System.Serializable]
    public abstract class MonoModifier : VersionedMonoBehaviour, IPathModifier
    {
        [System.NonSerialized]
		public Seeker seeker;

		/// <summary>Alerts the Seeker that this modifier exists</summary>
		protected virtual void OnEnable () {
        }

        protected virtual void OnDisable () {
        }

        /// <summary>
        /// Modifiers will be executed from lower order to higher order.
        /// This value is assumed to stay constant.
        /// </summary>
        public abstract int Order { get; }

		public virtual void PreProcess (Path path) {
        }

        /// <summary>Called for each path that the Seeker calculates after the calculation has finished</summary>
        public abstract void Apply(Path path);
	}
}
