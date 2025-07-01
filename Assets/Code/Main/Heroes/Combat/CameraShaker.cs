using System;
using System.Threading;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.MVC;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class CameraShaker : MonoBehaviour {
        CinemachineVirtualCamera _camera;
        Sequence _sequence;
        CancellationTokenSource _cts = new();
        ScreenShakesProactiveSetting _screenShakesSettings;

        void Start() {
            _camera = GetComponent<CinemachineVirtualCamera>() ?? throw new Exception("No cinemachine virtual camera");
            _screenShakesSettings = World.Only<ScreenShakesProactiveSetting>();
        }

        void OnDestroy() {
            _sequence.Kill();
        }

        [Button]
        public async UniTask Shake(float amplitude = 0.5f, float frequency = 0.15f, float time = 0.5f, float pickPercent = 0.1f) {
            amplitude *= _screenShakesSettings.Intensity;
            
            var noise = _camera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            CancelTween();
            await DOTween.Sequence()
                .AppendCallback(() => noise.m_FrequencyGain = frequency)
                .Append(DOTween.To(() => noise.m_AmplitudeGain, value => noise.m_AmplitudeGain = value, amplitude, time * pickPercent))
                .Append(DOTween.To(() => noise.m_AmplitudeGain, value => noise.m_AmplitudeGain = value, 0, time * (1 - pickPercent)))
                .AppendCallback(() => noise.m_FrequencyGain = 0)
                .OnComplete(() => noise.m_FrequencyGain = 0)
                .OnKill(() => noise.m_FrequencyGain = 0)
                //.WithCancellation(_cts.Token)
                ;
        }

        void CancelTween() {
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }
}