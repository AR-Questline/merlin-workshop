using System;
using System.IO;
using Awaken.Utility.Graphics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Utility.Editor.Graphics {
    public class SmallObjectsShadowsWindow : Sirenix.OdinInspector.Editor.OdinEditorWindow {
        [ShowInInspector, ListDrawerSettings(CustomAddFunction = nameof(Add))]
        SmallObjectsShadows.ShadowVolumeDatum[] _shadowVolumeData = Array.Empty<SmallObjectsShadows.ShadowVolumeDatum>();

        [UnityEditor.MenuItem("TG/Graphics/Small Objects Shadows")]
        static void ShowWindow() {
            var window = GetWindow<SmallObjectsShadowsWindow>();
            window.titleContent = new GUIContent("Small Objects Shadows");
            window.Show();
        }

        protected override void Initialize() {
            base.Initialize();
            _shadowVolumeData = SmallObjectsShadows.LoadData().ToManagedArray();
        }

        [Button]
        unsafe void Save() {
            Array.Sort(_shadowVolumeData, static (a, b) => a.maxVolume.CompareTo(b.maxVolume));

            FileStream file = new FileStream(SmallObjectsShadows.ConfigPath, FileMode.Create);

            fixed (SmallObjectsShadows.ShadowVolumeDatum* volumeDataPtr = _shadowVolumeData) {
                var bytesBuffer = new ReadOnlySpan<byte>(volumeDataPtr, sizeof(SmallObjectsShadows.ShadowVolumeDatum) * _shadowVolumeData.Length);
                file.Write(bytesBuffer);
                file.Flush();
            }

            file.Close();
            SmallObjectsShadows.ChangedShadowsBias(SmallObjectsShadows.ShadowsBias);
        }

        SmallObjectsShadows.ShadowVolumeDatum Add() {
            if (_shadowVolumeData.Length != 0) {
                return new SmallObjectsShadows.ShadowVolumeDatum() {
                    maxVolume = _shadowVolumeData[_shadowVolumeData.Length - 1].maxVolume * 1.1f,
                    distance = _shadowVolumeData[_shadowVolumeData.Length - 1].distance * 1.1f
                };
            } else {
                return new SmallObjectsShadows.ShadowVolumeDatum() {
                    maxVolume = 1,
                    distance = 4
                };
            }
        }
    }
}