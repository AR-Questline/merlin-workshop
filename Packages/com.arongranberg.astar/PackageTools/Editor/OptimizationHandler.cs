using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Pathfinding {
	/// <summary>
	/// Helper for enabling or disabling compiler directives.
	/// Used only in the editor.
	/// </summary>
	public static class OptimizationHandler {
		public class DefineDefinition {
			public string name;
			public string description;
			public bool enabled;
			public bool consistent;
		}

		/// <summary>
		/// Various build targets that Unity have deprecated.
		/// There is apparently no way to figure out which these are without hard coding them.
		/// </summary>
		static readonly BuildTargetGroup[] deprecatedBuildTargets = new BuildTargetGroup[] {
			BuildTargetGroup.Unknown,
#if UNITY_5_4_OR_NEWER
			(BuildTargetGroup)16, /* BlackBerry */
#endif
#if UNITY_5_5_OR_NEWER
			(BuildTargetGroup)5, /* PS3 */
			(BuildTargetGroup)6, /* XBox360 */
			(BuildTargetGroup)15, /* WP8 */
#endif
#if UNITY_2017_4_OR_NEWER
			(BuildTargetGroup)2, /* WebPlayer */
			(BuildTargetGroup)20, /* PSM */
#endif
#if UNITY_2018_1_OR_NEWER
			(BuildTargetGroup)22, /* SamsungTV */
			(BuildTargetGroup)24, /* WiiU */
#endif
#if UNITY_2018_2_OR_NEWER
			(BuildTargetGroup)17, /* Tizen */
#endif
#if UNITY_2018_3_OR_NEWER
			(BuildTargetGroup)18, /* PSP2 */
			(BuildTargetGroup)23, /* Nintendo3DS */
#endif
		};

		static string GetPackageRootDirectory () {
            return default;
        }

        static Dictionary<BuildTargetGroup, List<string> > GetDefineSymbols () {
            return default;
        }

        static void SetDefineSymbols(Dictionary<BuildTargetGroup, List<string>> symbols)
        {
        }

        public static void EnableDefine(string name)
        {
        }

        public static void DisableDefine(string name)
        {
        }

        public static void IsDefineEnabled(string name, out bool enabled, out bool consistent)
        {
            enabled = default(bool);
            consistent = default(bool);
        }

        public static List<DefineDefinition> FindDefines()
        {
            return default;
        }

        public static void ApplyDefines(List<DefineDefinition> defines)
        {
        }
    }
}
