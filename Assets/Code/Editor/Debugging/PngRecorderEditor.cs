using Awaken.TG.Debugging;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging
{
    [CustomEditor(typeof(PngRecorder))]
    public class PngRecorderEditor : OdinEditor {
        
        PngRecorder Recorder => (PngRecorder) target;
        
        protected override void OnEnable() {
            base.OnEnable();
            Recorder.OnStateChanged += this.Repaint;
        }

        protected override void OnDisable() {
            if (Recorder != null) {
                Recorder.OnStateChanged -= this.Repaint;
            }
            base.OnDisable();
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "frameRate", "targetFolder");
            serializedObject.ApplyModifiedProperties();
            
            PngRecorder rec = (PngRecorder) target;
            // target folder
            EditorGUILayout.BeginHorizontal();
            rec.targetFolder = EditorGUILayout.TextField("Target folder", rec.targetFolder);
            if (GUILayout.Button("...", GUILayout.Width(30))) {
                string selectedFolder = EditorUtility.SaveFolderPanel("Select target folder for screenshots", rec.targetFolder, "Recording");
                if (!string.IsNullOrEmpty(selectedFolder)) {
                    rec.targetFolder = selectedFolder;
                }
            }            
            EditorGUILayout.EndHorizontal();
            // framerate
            rec.frameRate = EditorGUILayout.IntField("Framerate", rec.frameRate);
            
            // recording
            if (rec.IsRecording) {
                EditorGUILayout.HelpBox("Recording...", MessageType.Warning);
                if (GUILayout.Button("Stop")) {
                    rec.FinishRecording();
                }
            } else {
                if (GUILayout.Button("Record")) {
                    rec.StartRecording();
                }
            }
        }
    }
}
