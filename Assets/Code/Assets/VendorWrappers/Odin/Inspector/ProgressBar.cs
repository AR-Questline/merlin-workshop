using System;
using UnityEngine;

namespace Sirenix.OdinInspector
{
    public class ProgressBarAttribute : Attribute
    {
        public double Min;
        public double Max;
        public string MinGetter;
        public string MaxGetter;
        public float R;
        public float G;
        public float B;
        public int Height;
        public string ColorGetter;
        public string BackgroundColorGetter;
        public bool Segmented;
        public string CustomValueStringGetter;
        private bool drawValueLabel;
        private TextAlignment valueLabelAlignment;

        public string MinMember
        {
          get => this.MinGetter;
          set => this.MinGetter = value;
        }

        public string MaxMember
        {
          get => this.MaxGetter;
          set => this.MaxGetter = value;
        }

        public string ColorMember
        {
          get => this.ColorGetter;
          set => this.ColorGetter = value;
        }

        public string BackgroundColorMember
        {
          get => this.BackgroundColorGetter;
          set => this.BackgroundColorGetter = value;
        }

        public string CustomValueStringMember
        {
          get => this.CustomValueStringGetter;
          set => this.CustomValueStringGetter = value;
        }

        public ProgressBarAttribute(double min, double max, float r = 0.15f, float g = 0.47f, float b = 0.74f) { }
        public ProgressBarAttribute(string minGetter, double max, float r = 0.15f, float g = 0.47f, float b = 0.74f) { }
        public ProgressBarAttribute(double min, string maxGetter, float r = 0.15f, float g = 0.47f, float b = 0.74f) { }
        public ProgressBarAttribute(string minGetter, string maxGetter, float r = 0.15f, float g = 0.47f, float b = 0.74f) { }

        public bool DrawValueLabel
        {
          get => this.drawValueLabel;
          set
          {
            this.drawValueLabel = value;
            this.DrawValueLabelHasValue = true;
          }
        }

        public bool DrawValueLabelHasValue { get; private set; }

        public TextAlignment ValueLabelAlignment
        {
          get => this.valueLabelAlignment;
          set
          {
            this.valueLabelAlignment = value;
            this.ValueLabelAlignmentHasValue = true;
          }
        }

        public bool ValueLabelAlignmentHasValue { get; private set; }
    }
}