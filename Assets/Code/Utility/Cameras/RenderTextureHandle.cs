using System.Collections.Generic;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Utility.Cameras {
    public class RenderTextureHandle : ScriptableObject {
        List<RenderTextureAssigner> _assigners = new List<RenderTextureAssigner>();
        RenderTexture _texture;
        int _currentWidth;
        int _currentHeight;

        public void Get(RenderTextureAssigner assigner) {
            _texture ??= new RenderTexture(1, 1, 0);
            _assigners.Add(assigner);
            assigner.Assign(_texture);
        }
        
        public void Set(RenderTexture texture) {
            _texture = texture;
            foreach (var assigner in _assigners.WhereNotNull()) {
                assigner.Assign(_texture);
            }
        }

        public void Check(int targetWidth, int targetHeight) {
            if (targetWidth != _currentWidth || targetHeight != _currentHeight) {
                CreateTexture(targetWidth, targetHeight);
            }
        }

        void CreateTexture(int targetWidth, int targetHeight) {
            RenderTexture texture = new RenderTexture(targetWidth, targetHeight, 16);
            Set(texture);
            _currentWidth = targetWidth;
            _currentHeight = targetHeight;
        }
    }
}