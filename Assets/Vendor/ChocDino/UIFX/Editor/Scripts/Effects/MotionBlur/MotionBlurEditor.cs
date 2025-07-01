//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(MotionBlurSimple), true)]
	[CanEditMultipleObjects]
	internal class MotionBlurSimpleEditor : MotionBlurEditor
	{
		private SerializedProperty _propMode;
		private SerializedProperty _propSampleCount;
		private SerializedProperty _propBlendStrength;
		private SerializedProperty _propLerpUV;
		private SerializedProperty _propFrameRateIndependent;
		private SerializedProperty _propStrength;

		void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
        }
    }

    [CustomEditor(typeof(MotionBlurReal), true)]
    [CanEditMultipleObjects]
    internal class MotionBlurRealEditor : MotionBlurEditor
    {
        private SerializedProperty _propMode;
        private SerializedProperty _propSampleCount;
        private SerializedProperty _propLerpUV;
        private SerializedProperty _propFrameRateIndependent;
        private SerializedProperty _propStrength;
        private SerializedProperty _propShaderAdd;
        private SerializedProperty _propShaderResolve;

        void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
        }
    }

	internal abstract class MotionBlurEditor : BaseEditor
	{
		protected static readonly AboutInfo s_aboutInfo
				= new AboutInfo(s_aboutHelp, "UIFX - Motion Blur\n© Chocolate Dinosaur Ltd", "uifx-logo-motion-blur")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Documentation")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/motion-blur/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/motion-blur/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/motion-blur/components/motion-blur-real/"),
								new AboutButton("API Reference", "https://www.chocdino.com/products/unity-assets/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Review <b>UIFX - Motion Blur</b>", "https://assetstore.unity.com/packages/slug/260687?aid=1100lSvNe#reviews"),
								new AboutButton("Review <b>UIFX Bundle</b>", AssetStoreBundleReviewUrl),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", DiscordUrl),
								new AboutButton("Post to Unity Forum Thread", "https://discussions.unity.com/t/released-uifx-motion-blur/930437"),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};
		
		protected static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeToBundle, s_aboutInfo } );

		public override void OnInspectorGUI()
        {
        }
    }
}