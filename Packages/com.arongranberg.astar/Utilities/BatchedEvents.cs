using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace Pathfinding.Util {
	/// <summary>Helper for batching updates to many objects efficiently</summary>
	[HelpURL("https://arongranberg.com/astar/documentation/stable/batchedevents.html")]
	public class BatchedEvents : VersionedMonoBehaviour {
		const int ArchetypeOffset = 22;
		const int ArchetypeMask = 0xFF << ArchetypeOffset;

		static Archetype[] data = new Archetype[0];
		static BatchedEvents instance;
		static int isIteratingOverTypeIndex = -1;
		static bool isIterating = false;

		[System.Flags]
		public enum Event {
			Update = 1 << 0,
			LateUpdate = 1 << 1,
			FixedUpdate = 1 << 2,
			Custom = 1 << 3,
			None = 0,
		};


		struct Archetype {
			public object[] objects;
			public int objectCount;
			public System.Type type;
			public TransformAccessArray transforms;
			public int variant;
			public int archetypeIndex;
			public Event events;
			public System.Action<object[], int, TransformAccessArray, Event> action;
			public CustomSampler sampler;

			public void Add (Component obj) {
            }

            public void Remove(int index)
            {
            }
        }

#if UNITY_EDITOR
		void DelayedDestroy () {
        }
#endif

        void OnEnable () {
        }

        void OnDisable () {
        }

        static void CreateInstance () {
        }

        public static T Find<T, K>(K key, System.Func<T, K, bool> predicate) where T : class, IEntityIndex {
            return default;
        }

        public static void Remove<T>(T obj) where T : IEntityIndex {
        }

        public static int GetComponents<T>(Event eventTypes, out TransformAccessArray transforms, out T[] components) where T : Component, IEntityIndex {
            transforms = default(TransformAccessArray);
            components = default(T[]);
            return default;
        }

        public static bool Has<T>(T obj) where T : IEntityIndex => obj.EntityIndex != 0;

		public static void Add<T>(T obj, Event eventTypes, System.Action<T[], int> action, int archetypeVariant = 0) where T : Component, IEntityIndex {
        }

        public static void Add<T>(T obj, Event eventTypes, System.Action<T[], int, TransformAccessArray, Event> action, int archetypeVariant = 0) where T : Component, IEntityIndex {
        }

        static void Add<T>(T obj, Event eventTypes, System.Action<T[], int, TransformAccessArray, Event> action1, System.Action<T[], int> action2, int archetypeVariant = 0) where T : Component, IEntityIndex {
        }

        void Process(Event eventType, System.Type typeFilter)
        {
        }

        public static void ProcessEvent<T>(Event eventType)
        {
        }

        void Update()
        {
        }

        void LateUpdate()
        {
        }

        void FixedUpdate()
        {
        }
    }
}
