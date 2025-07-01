using UnityEngine;
using UnityEditor;

namespace TAO.VertexAnimation.Editor
{
	[CustomEditor(typeof(VA_ModelBaker))]
	public class VA_ModelBakerEditor : UnityEditor.Editor
	{
		private VA_ModelBaker modelBaker = null;

		void OnEnable()
		{
			modelBaker = target as VA_ModelBaker;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			InputGUI();
			EditorGUILayoutUtils.HorizontalLine(color: Color.gray);
			BakeGUI();

			serializedObject.ApplyModifiedProperties();
		}

		private void InputGUI()
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.model)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.animationClips)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.fps)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.targetTextureWidth)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.maxTextureDimensions)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.applyRootMotion)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.includeInactive)));
		}

		private void BakeGUI()
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.lodSettings)).FindPropertyRelative(nameof(VA_ModelBaker.LODSettings.lodSettings)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.applyAnimationBounds)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.generateAnimationBook)));

			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.generatePrefab)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.useNormalA)), new GUIContent("Use Normal (Animation)"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.useInterpolation)));
			EditorGUILayout.Space(5);
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.materialShader)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VA_ModelBaker.noVAMaterialShader)));

			if (GUILayout.Button("Bake", GUILayout.Height(32)))
			{
				modelBaker.Bake();
			}
			
			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Delete Unused Animations", EditorStyles.miniButtonLeft))
				{
					if (EditorUtility.DisplayDialog("Delete Unused Animations", "Deleting assets will loose references within the project.", "Ok", "Cancel"))
					{
						modelBaker.DeleteUnusedAnimations();
					}
				}

				if (GUILayout.Button("Delete", EditorStyles.miniButtonRight))
				{
					if (EditorUtility.DisplayDialog("Delete Assets", "Deleting assets will loose references within the project.", "Ok", "Cancel"))
					{
						modelBaker.DeleteSavedAssets();
					}
				}
			}
		}
	}
}