using System;
using UnityEngine;

namespace Sirenix.OdinInspector
{
    public class GUIColorAttribute : Attribute
    {
        public Color Color;
        public string GetColor;

        public GUIColorAttribute(float r, float g, float b, float a = 1f) { }
        public GUIColorAttribute(string getColor) { }
    }
}