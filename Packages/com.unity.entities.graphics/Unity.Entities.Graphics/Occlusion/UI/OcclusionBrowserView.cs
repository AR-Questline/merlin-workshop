#if UNITY_EDITOR && ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Rendering.Occlusion.Masked;
using UnityEngine;
using UnityEngine.UIElements;

public class OcclusionBrowserView : VisualElement
{
    public string ViewName
    {
        get => viewName.text;
        set => viewName.text = value;
    }

    public int CurrentSlice { get; set; }

    public Texture2D CurrentSliceTexture { get; set; }

    List<BufferGroup> BufferGroups { get; set; }

    public OcclusionBrowserView()
    {
        VisualTreeAsset template = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.entities.graphics/Unity.Entities.Graphics/Occlusion/UI/OcclusionBrowserView.uxml");
        template.CloneTree(this);

        Initialize();
    }

    private VisualElement container;

    private Label viewName;
    private Label statusBar, statusBar2;
    private Button nextSlice, prevSlice;
    private VisualElement viewData;
    private VisualElement sliceList;

    /// <inheritdoc />
    public override VisualElement contentContainer => this.container;

    public ListView ViewList { get; private set; }
    public Label InfoText { get => statusBar2; }

    public void Reset()
    {
        bool hasSlices = BufferGroups.Count > 1;
        nextSlice.SetEnabled(hasSlices);
        prevSlice.SetEnabled(hasSlices);

        nextSlice.clicked += () => Debug.Log("Next");
        prevSlice.clicked += () => Debug.Log("Prev");
    }

    private void Initialize()
    {
        viewName = this.Q<Label>("view-name");
        statusBar = this.Q<Label>("status-bar");
        statusBar2 = this.Q<Label>("status-bar2");

        nextSlice = this.Q<Button>("next-slice");
        prevSlice = this.Q<Button>("prev-slice");
        viewData = this.Q<VisualElement>("view-data");
        sliceList = this.Q<VisualElement>("slice-list");

        ViewList = this.Q<ListView>("view-list");

        nextSlice.SetEnabled(false);
        prevSlice.SetEnabled(false);

        container = this.Q<VisualElement>("occlusion-data"); ;

        viewData.generateVisualContent = new Action<MeshGenerationContext>(OnGenerateVisualContent);
    }

    /// <inheritdoc cref="UxmlFactory" />
    public new class UxmlFactory : UxmlFactory<OcclusionBrowserView, UxmlTraits>
    {
    }

    /// <inheritdoc cref="UxmlTraits" />
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        private readonly UxmlStringAttributeDescription viewName = new UxmlStringAttributeDescription { name = "view-name", defaultValue = "Unknown" };
        private readonly UxmlIntAttributeDescription currentSlice = new UxmlIntAttributeDescription { name = "current-slice", defaultValue = 1 };

        public override void Init(VisualElement visualElement, IUxmlAttributes bag, CreationContext creationContext)
        {
            base.Init(visualElement, bag, creationContext);

            if (visualElement is OcclusionBrowserView browserView)
            {
                browserView.Initialize();

                browserView.ViewName = viewName.GetValueFromBag(bag, creationContext);
                browserView.CurrentSlice = currentSlice.GetValueFromBag(bag, creationContext);
                browserView.container = browserView.contentContainer;
            }
        }
    }

    internal void SetupGUI()
    {
        nextSlice.visible = prevSlice.visible = false;

        currentSelection = 0;
        sliceList.Clear();

        int index = 0;
        foreach (var bg in BufferGroups)
        {
            var tex = bg.GetVisualizationTexture();

            var background = new StyleBackground(tex);

            var thumbnail = new Button()
            {
                style =
                    {
                        width = 64,
                        height = 64,
                        marginBottom = 0,
                        marginTop = 0,
                        marginLeft = 0,
                        marginRight = 0,

                        paddingBottom = 0,
                        paddingTop = 0,
                        paddingLeft = 0,
                        paddingRight = 0,

                        backgroundImage = background,
                    }
            };

            int sliceIndex = index;
            thumbnail.clicked += () =>
            {
                SelectSlice(sliceIndex);
            };
            sliceList.Add(thumbnail);

            index++;
        }
        viewData.RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
        viewData.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveEvent);

        SelectSlice(currentSelection);
    }

    private void OnMouseLeaveEvent(MouseLeaveEvent evt)
    {
        statusBar.text = "";
    }

    private void OnMouseMoveEvent(MouseMoveEvent evt)
    {
        var bg = BufferGroups[currentSelection];

        var tex = bg.GetVisualizationTexture();
        if (tex == null)
        {
            return;
        }

        float2 pos = evt.localMousePosition;
        pos.x /= viewData.localBound.width;
        pos.y /= viewData.localBound.height;

        var x = (int)(pos.x * tex.width);
        var y = (int)(pos.y * tex.height);
        var color = tex.GetPixel(x, y);

        float depth = color.r;

        statusBar.text = $"<b>Mouse:</b> ({x},{y})  <b>Depth:</b> {depth}";
    }

    int currentSelection = 0;

    private void SelectSlice(int sliceIndex)
    {
        var bg = BufferGroups[sliceIndex];
        CurrentSliceTexture = bg.GetVisualizationTexture();
        currentSelection = sliceIndex;

        viewData.MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Vertex[] vertices = new Vertex[4];
        ushort[] indices = { 0, 1, 2, 2, 3, 0 };

        Rect r = viewData.contentRect;
        if (r.width < 0.01f || r.height < 0.01f)
            return; // Skip rendering when too small.

        float left = 0;
        float right = r.width;
        float top = 0;
        float bottom = r.height;

        vertices[0].position = new Vector3(left, bottom, Vertex.nearZ);
        vertices[1].position = new Vector3(left, top, Vertex.nearZ);
        vertices[2].position = new Vector3(right, top, Vertex.nearZ);
        vertices[3].position = new Vector3(right, bottom, Vertex.nearZ);

        vertices[0].tint =
        vertices[1].tint =
        vertices[2].tint =
        vertices[3].tint = Color.white;

        if (CurrentSliceTexture == null)
        {
			mgc.DrawText("Inactive view", new Vector2(5, 5), 12, Color.white);
            return;
		}

        MeshWriteData mwd = mgc.Allocate(vertices.Length, indices.Length, CurrentSliceTexture);

#if !UNITY_2023_2_OR_NEWER
        // Since the texture may be stored in an atlas, the UV coordinates need to be
        // adjusted. Simply rescale them in the provided uvRegion.
        Rect uvRegion = mwd.uvRegion;
        vertices[0].uv = new Vector2(0, 1) * uvRegion.size + uvRegion.min;
        vertices[1].uv = new Vector2(0, 0) * uvRegion.size + uvRegion.min;
        vertices[2].uv = new Vector2(1, 0) * uvRegion.size + uvRegion.min;
        vertices[3].uv = new Vector2(1, 1) * uvRegion.size + uvRegion.min;
#endif

        mwd.SetAllVertices(vertices);
        mwd.SetAllIndices(indices);
    }

    internal void SetBufferGroups(List<BufferGroup> groups)
    {
        BufferGroups = groups;
    }
}

#endif
