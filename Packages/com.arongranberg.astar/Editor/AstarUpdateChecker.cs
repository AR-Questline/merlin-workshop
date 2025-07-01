using UnityEngine;
using UnityEditor;
using Pathfinding.Util;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;

namespace Pathfinding {
	/// <summary>Handles update checking for the A* Pathfinding Project</summary>
	[InitializeOnLoad]
	public static class AstarUpdateChecker {
		/// <summary>Used for downloading new version information</summary>
		static UnityWebRequest updateCheckDownload;

		static System.DateTime _lastUpdateCheck;
		static bool _lastUpdateCheckRead;

		static System.Version _latestVersion;

		static System.Version _latestBetaVersion;

		/// <summary>Description of the latest update of the A* Pathfinding Project</summary>
		static string _latestVersionDescription;

		static bool hasParsedServerMessage;

		/// <summary>Number of days between update checks</summary>
		const double updateCheckRate = 1F;

		/// <summary>URL to the version file containing the latest version number.</summary>
		const string updateURL = "https://www.arongranberg.com/astar/version.php";

		/// <summary>Last time an update check was made</summary>
		public static System.DateTime lastUpdateCheck {
			get {
				try {
					// Reading from EditorPrefs is relatively slow, avoid it
					if (_lastUpdateCheckRead) return _lastUpdateCheck;

					_lastUpdateCheck = System.DateTime.Parse(EditorPrefs.GetString("AstarLastUpdateCheck", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
					_lastUpdateCheckRead = true;
				}
				catch (System.FormatException) {
					lastUpdateCheck = System.DateTime.UtcNow;
					Debug.LogWarning("Invalid DateTime string encountered when loading from preferences");
				}
				return _lastUpdateCheck;
			}
			private set {
				_lastUpdateCheck = value;
				EditorPrefs.SetString("AstarLastUpdateCheck", _lastUpdateCheck.ToString(System.Globalization.CultureInfo.InvariantCulture));
			}
		}

		/// <summary>Latest version of the A* Pathfinding Project</summary>
		public static System.Version latestVersion {
			get {
				RefreshServerMessage();
				return _latestVersion ?? AstarPath.Version;
			}
			private set {
				_latestVersion = value;
			}
		}

		/// <summary>Latest beta version of the A* Pathfinding Project</summary>
		public static System.Version latestBetaVersion {
			get {
				RefreshServerMessage();
				return _latestBetaVersion ?? AstarPath.Version;
			}
			private set {
				_latestBetaVersion = value;
			}
		}

		/// <summary>Summary of the latest update</summary>
		public static string latestVersionDescription {
			get {
				RefreshServerMessage();
				return _latestVersionDescription ?? "";
			}
			private set {
				_latestVersionDescription = value;
			}
		}

		/// <summary>
		/// Holds various URLs and text for the editor.
		/// This info can be updated when a check for new versions is done to ensure that there are no invalid links.
		/// </summary>
		static Dictionary<string, string> astarServerData = new Dictionary<string, string> {
			{ "URL:modifiers", "https://www.arongranberg.com/astar/docs/modifiers.html" },
			{ "URL:astarpro", "https://arongranberg.com/unity/a-pathfinding/astarpro/" },
			{ "URL:documentation", "https://arongranberg.com/astar/docs/" },
			{ "URL:findoutmore", "https://arongranberg.com/astar" },
			{ "URL:download", "https://arongranberg.com/unity/a-pathfinding/download" },
			{ "URL:changelog", "https://arongranberg.com/astar/docs/changelog.html" },
			{ "URL:tags", "https://arongranberg.com/astar/docs/tags.html" },
			{ "URL:homepage", "https://arongranberg.com/astar/" }
		};

		static AstarUpdateChecker() {
        }

        static void RefreshServerMessage () {
        }

        public static string GetURL (string tag) {
            return default;
        }

        /// <summary>Initiate a check for updates now, regardless of when the last check was done</summary>
        public static void CheckForUpdatesNow () {
        }

        /// <summary>
        /// Checking for updates...
        /// Should be called from EditorApplication.update
        /// </summary>
        static void UpdateCheckLoop () {
        }

        /// <summary>
        /// Checks for updates if there was some time since last check.
        /// It must be called repeatedly to ensure that the result is processed.
        /// Returns: True if an update check is progressing (WWW request)
        /// </summary>
        static bool CheckForUpdates () {
            return default;
        }

        static void DownloadVersionInfo()
        {
        }

        /// <summary>Handles the data from the update page</summary>
        static void UpdateCheckCompleted(string result)
        {
        }

        static void ParseServerMessage(string result)
        {
        }

        static void ShowUpdateWindowIfRelevant()
        {
        }
    }
}
