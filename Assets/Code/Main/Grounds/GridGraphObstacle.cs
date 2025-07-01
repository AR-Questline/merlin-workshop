using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Cysharp.Threading.Tasks;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.Grounds {
	public enum ArColliderType {
		Box = 0,
		Sphere = 1,
		Capsule = 2,
	}

	public class GridGraphObstacle : MonoBehaviour, IEventSource {
		const float ColliderHeight = 1000f;
		public ArColliderType colliderType;

		public static class Events {
			public static readonly Event<GridGraphObstacle, GridGraphObstacle> GridObstacleChanged = new(nameof(GridObstacleChanged));
		}
		
		void Awake() {
			SetupObstacle();
		}

		void OnEnable() {
			World.EventSystem?.Trigger(this, Events.GridObstacleChanged, this);
			var navMeshCut = gameObject.GetComponent<NavmeshCut>();
			UniTask.DelayFrame(10).ContinueWith(() => {
				navMeshCut.enabled = false;
				navMeshCut.enabled = true;
			}).Forget();
		}

		void OnDisable() {
			World.EventSystem?.Trigger(this, Events.GridObstacleChanged, this);
		}

		void SetupObstacle() {
			if (!Application.isPlaying) return;

			Collider coll = GetComponent<Collider>();
			var navMeshCut = gameObject.GetComponent<NavmeshCut>();
			if (navMeshCut == null) {
				navMeshCut = gameObject.AddComponent<NavmeshCut>();
			}
			
			if (coll is BoxCollider box) {
				box.size += new Vector3(0, 1000, 0);
				navMeshCut.type = NavmeshCut.MeshType.Rectangle;
				navMeshCut.center = box.center;
				navMeshCut.height = box.size.y;
				navMeshCut.rectangleSize = new Vector2(box.size.x * 1.2f, box.size.z * 1.2f);
			} else if (coll is CapsuleCollider capsule) {
				capsule.height = ColliderHeight;
				navMeshCut.type = NavmeshCut.MeshType.Circle;
				navMeshCut.center = capsule.center;
				navMeshCut.height = capsule.height;
				navMeshCut.circleRadius = capsule.radius * 0.95f;
				navMeshCut.circleResolution = 12;
			} else if (coll is SphereCollider sphere) {
				navMeshCut.type = NavmeshCut.MeshType.Circle;
				navMeshCut.center = sphere.center;
				navMeshCut.height = sphere.radius;
				navMeshCut.circleRadius = sphere.radius * 0.95f;
				navMeshCut.circleResolution = 12;
			}
			
			navMeshCut.useRotationAndScale = true;
			navMeshCut.UpdateDistance = 0.2f;
			UniTask.DelayFrame(10).ContinueWith(() => {
				navMeshCut.enabled = false;
				navMeshCut.enabled = true;
			}).Forget();
		}

		public string ID => name;
	}
}