//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEngine.UI;

namespace ChocDino.UIFX
{
	/// <summary>
	/// Allows a Camera to render directly to UGUI more gracefully than using doing it manually with a RenderTexture.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(CanvasRenderer)), DisallowMultipleComponent]
	[HelpURL("https://www.chocdino.com/products/unity-assets/")]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Sources/UIFX - Camera Source")]
	public class CameraSource : MaskableGraphic
	{
		[SerializeField] Camera _camera = null;

		public Camera Camera { get => _camera; set { _camera = value; ForceUpdate(); } }

		public RenderTexture Texture { get => _renderTexture; }

		private RenderTexture _renderTexture;
		private Camera _renderCamera;

		public override Texture mainTexture => _renderTexture;

		protected override void Awake()
        {
        }

        protected override void OnEnable()
        {
        }

        protected override void OnDisable()
        {
        }

        /// <summary>
        /// OnCanvasHierarchyChanged() is called when the Canvas is enabled/disabled
        /// </summary>
        protected override void OnCanvasHierarchyChanged()
        {
        }

        /// <summary>
        /// OnTransformParentChanged() is called when a parent is changed, in which case we may need to get a new Canvas
        /// </summary>
        protected override void OnTransformParentChanged()
        {
        }

        /// <summary>
        /// Forces the filter to update.  Usually this happens automatically, but in some cases you may want to force an update.
        /// </summary>
        public void ForceUpdate(bool force = false)
        {
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
        }

        protected override void OnValidate()
        {
        }
#endif

        protected virtual void Update()
        {
        }

        bool CreateTexture()
        {
            return default;
        }

        void ReleaseTexture()
        {
        }
    }
}