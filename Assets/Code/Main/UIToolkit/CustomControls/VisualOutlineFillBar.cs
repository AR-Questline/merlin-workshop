using System;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.CustomControls {
    [UxmlElement("OutlineFillBar")]
    public partial class VisualOutlineFillBar : VisualElement, IVisualPresenter {
        //USS class names in BEM convention https://getbem.com
        public const string USSClass = "outline-progress";
        public const string USSLabelClass = USSClass + "__label";
        const float SegmentValue = 0.125f;

        //These objects allow C# code to access custom USS properties
        static readonly CustomStyleProperty<Color> TrackColor = new("--background-color");
        static readonly CustomStyleProperty<Color> ProgressColor = new("--progress-color");
        Color _backgroundColor = Color.gray;
        Color _progressColor = Color.white;
        
        public VisualElement Content => contentContainer;

        readonly Label _label;
        float _progress;
        bool _showLabel;
        
        float _width;
        float _height;
        float _radius;
        Vector2 _circleCenter;
        Vector2 _squareStart;
        Vector2[] _squareSegments;
        
        [UxmlAttribute, Header(nameof(VisualOutlineFillBar))] public float LineWidth { get; set; } = 2.0f;
        [UxmlAttribute, Range(0, 1)] public float ShapeGap { get; set; }
        [UxmlAttribute] public ArcDirection ArcDirection { get; set; }
        [UxmlAttribute] public Shape ShapeType { get; set; }

        [UxmlAttribute]
        public bool ShowLabel {
            [UnityEngine.Scripting.Preserve] get => _showLabel;
            set {
                _showLabel = value;
                _label.SetActiveOptimized(_showLabel);
            }
        }
        
        [UxmlAttribute, Range(0, 1)]
        public float Progress {
            get => _progress;
            set {
                _progress = Mathf.Clamp(value, 0, 1);
                _label.text = _progress.ToString("P0");
                MarkDirtyRepaint();
            }
        }
        
        public VisualOutlineFillBar() {
            _label = SetupLabel();
            AddToClassList(USSClass);
            
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            generateVisualContent += GenerateRadialBar;
            
            SetCoreValues();
            MarkDirtyRepaint();
        }
        
        void OnGeometryChanged(GeometryChangedEvent evt) { 
            if (evt.oldRect.size == evt.newRect.size) return;
            SetCoreValues();
        }
        
        void SetCoreValues() {
            _width = contentRect.width;
            _height = contentRect.height;
            _radius = _width * 0.5f;
            _circleCenter = new Vector2(_width * 0.5f, _height * 0.5f);
            _squareStart = new Vector2(_radius, 0);
            
            Vector2[] temp = {
                new (_width, 0),
                new (_width, _radius),
                new (_width, _height),
                new (_radius, _height),
                new (0, _height),
                new (0, _radius),
                new (0, 0),
                _squareStart
            };
            
            _squareSegments = temp;
        }
        
        Label SetupLabel() {
            Label label = new ();
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.AddToClassList(USSLabelClass);
            label.SetActiveOptimized(_showLabel);
            Add(label);
            return label;
        }

        static void OnCustomStyleResolved(CustomStyleResolvedEvent evt) {
            VisualOutlineFillBar element = (VisualOutlineFillBar)evt.currentTarget;
            element.UpdateCustomStyles();
        }
        
        void UpdateCustomStyles() {
            //Repaint if USS properties are set
            bool repaint = customStyle.TryGetValue(ProgressColor, out _progressColor);
            repaint |= customStyle.TryGetValue(TrackColor, out _backgroundColor);

            if (repaint) {
                MarkDirtyRepaint();
            }
        }

        void GenerateRadialBar(MeshGenerationContext context) {
            var painter = context.painter2D;
            painter.lineWidth = LineWidth;
            painter.lineCap = LineCap.Round;

            switch (ShapeType) {
                case Shape.Circle:
                    DrawCircle(painter);
                    break;
                case Shape.Square:
                    DrawSquare(painter);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        void DrawCircle(Painter2D painter) {
            float angleGap = ShapeGap * 180;
            float startAngle = -90 + angleGap;
            float endAngle = 270 - angleGap;
            
            float startProgress = ArcDirection == ArcDirection.Clockwise ? startAngle : endAngle;
            float endProgress = 360 - angleGap * 2;
            float currentProgress = ArcDirection == ArcDirection.Clockwise ? endProgress * Progress : endProgress * Progress * -1;
                    
            DrawArc(painter, startAngle, endAngle, _backgroundColor);
            DrawArc(painter, startProgress, currentProgress + startProgress, _progressColor, ArcDirection);
        }
        
        void DrawArc(Painter2D painter, float startAngle, float endAngle, Color color, ArcDirection direction = ArcDirection.Clockwise) {
            painter.strokeColor = color;
            painter.BeginPath();
            painter.Arc(_circleCenter, _radius, startAngle, endAngle, direction);
            painter.Stroke();
        }
        
        void DrawSquare(Painter2D painter) {
            float squareGap = ShapeGap * 0.5f;
            float gap = squareGap % SegmentValue / SegmentValue;

            int startSegment = (int)Mathf.Floor(squareGap / SegmentValue);
            int endSegment = _squareSegments.Length - startSegment;
            Vector2 root = startSegment == 0 ? _squareStart : _squareSegments[startSegment - 1]; 

            //Draw background
            painter.strokeColor = _backgroundColor;
            painter.BeginPath();
            painter.MoveTo(root);
                    
            for (int i = startSegment; i < endSegment; i++) {
                Vector2 point = _squareSegments[i];
                Vector2 prevPoint = i == startSegment ? root : _squareSegments[i - 1];
                        
                if (i == startSegment) {
                    prevPoint = Vector2.Lerp(prevPoint, point, gap);
                    painter.MoveTo(prevPoint);
                } else if (i == endSegment - 1) {
                    point = Vector2.Lerp(prevPoint, point, 1 - gap);
                }
                        
                painter.LineTo(point);
                painter.Stroke();
            }
            
            //Draw progress
            painter.strokeColor = _progressColor;
            painter.BeginPath();
            
            int index = ArcDirection == ArcDirection.Clockwise ? startSegment : endSegment - 1;
            for (float i = 0; i < Progress; i += SegmentValue) {
                Vector2 point = ArcDirection == ArcDirection.Clockwise ? _squareSegments[index] : index == 0 ? _squareStart : _squareSegments[index - 1];
                Vector2 prevPoint = ArcDirection == ArcDirection.Clockwise ? index == 0 ? _squareStart : _squareSegments[index - 1] : _squareSegments[index];
                
                bool isStartSegment = index == startSegment;
                bool isEndSegment = index == endSegment - 1;
                bool isClockwise = ArcDirection == ArcDirection.Clockwise;

                switch (isClockwise) {
                    case true when isStartSegment:
                    case false when isEndSegment:
                        prevPoint = Vector2.Lerp(prevPoint, point, gap);
                        painter.MoveTo(prevPoint);
                        break;
                    case true when isEndSegment:
                    case false when isStartSegment:
                        point = Vector2.Lerp(prevPoint, point, 1 - gap);
                        break;
                }
                
                point = Vector2.Lerp(prevPoint, point, (Progress - i) / SegmentValue);
                painter.LineTo(point);
                painter.Stroke();
                
                index += ArcDirection == ArcDirection.Clockwise ? 1 : -1;
            }
        }

        public enum Shape : byte {
            Circle,
            Square
        }
    }
}