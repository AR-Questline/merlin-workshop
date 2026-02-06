using System.IO;
using Awaken.TG.Utility;
using Awaken.Utility.Archives;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor.Helpers;
using Unity.Content;
using Unity.IO.Archive;
using UnityEditor;
using UnityEngine;

namespace Awaken.Utility.Editor.Debugging {
    public class ArchiveExplorerWindow : EditorWindow {
        Vector2 _scrollPosition;

        bool _hasData;
        bool _isStreamed;
        CompressionType _compressionType;
        ArchiveStatus _status;
        string _archivePath;
        string _mountPath;
        ArchiveFileInfo[] _fileInfos;

        protected void OnGUI() {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Select archive to explore");
            if (GUILayout.Button("Select archive")) {
                _hasData = false;
                var path = EditorUtility.OpenFilePanel("Select archive", Application.streamingAssetsPath, "arch");
                if (!string.IsNullOrEmpty(path)) {
                    ExploreArchive(path);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_hasData == false) {
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Archive selection button
            EditorGUILayout.LabelField($"Archive path: {_archivePath}");
            EditorGUILayout.LabelField($"Mount path: {_mountPath}");
            EditorGUILayout.LabelField($"Meta data: IsStreamed({_isStreamed}); Compression({_compressionType}); Status({_status})");
            EditorGUILayout.LabelField($"File infos: {_fileInfos.Length}");
            EditorGUILayout.Space();
            for (var i = 0; i < _fileInfos.Length; i++) {
                ArchiveFileInfo fileInfo = _fileInfos[i];
                EditorGUILayout.LabelField($"{i}. {fileInfo.Filename} - {M.HumanReadableBytes(fileInfo.FileSize)}");
            }

            EditorGUILayout.EndScrollView();
        }

        void ExploreArchive(string path) {
            if (!File.Exists(path)) {
                Log.Critical?.Error($"Archive file [{path}] does not exist");
                return;
            }

            _hasData = true;
            _archivePath = path;

            var contentNamespace = ContentNamespace.GetOrCreateNamespace("ExploreArchive");
            var contentHandle = ArchiveFileInterface.MountAsync(contentNamespace, path, string.Empty);
            contentHandle.JobHandle.Complete();
            _status = contentHandle.Status;
            if (contentHandle.Status != ArchiveStatus.Complete) {
                Log.Critical?.Error($"Archive mount at path [{path}] failed with status {contentHandle.Status}");
                contentHandle.Unmount().Complete();
                contentNamespace.Delete();
                return;
            }

            _isStreamed = contentHandle.IsStreamed;
            _compressionType = contentHandle.Compression;
            _fileInfos = contentHandle.GetFileInfo();
            _mountPath = contentHandle.GetMountPath();

            contentHandle.Unmount().Complete();
            contentNamespace.Delete();
        }

        [MenuItem("TG/Debug/Archive Explorer")]
        static void ShowWindow() {
            var window = GetWindow<ArchiveExplorerWindow>();
            window.titleContent = new GUIContent("Archive Explorer");
            window.Show();
        }
    }
}
