// Disable the warning: "Field 'DependencyCheck.Dependency.name' is never assigned to, and will always have its default value null"
#pragma warning disable 649
using UnityEditor;
using System.Linq;
using UnityEngine;

namespace Pathfinding.Util {
	[InitializeOnLoad]
	static class DependencyCheck {
		struct Dependency {
			public string name;
			public string version;
		}

		static DependencyCheck() {
        }
    }
}
