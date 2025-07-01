using System;
using System.Collections;
using Awaken.TG.Debugging.AssetViewer.AssetGroup;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Debugging.AssetViewer {
    public class PreviewAssetsSpawner : MonoBehaviour {
        [SerializeField] MapScene mapScene;
        
        IEnumerator Start() {
            while (!mapScene.IsInitialized) {
                yield return null;
            }
            
            SpawnLocationTemplates();
        }

        void SpawnLocationTemplates() {
            PreviewAssetsGroupComponent[] templatesGroups = GetComponentsInChildren<PreviewAssetsGroupComponent>();

            foreach (PreviewAssetsGroupComponent templatesGroup in templatesGroups) {
                templatesGroup.SpawnTemplates();
            }
            SetNoon();
        }

        void SetNoon() {
            var gameTime = World.Any<GameRealTime>();
            DateTime currentTime = (DateTime)gameTime.WeatherTime;
            DateTime nextNoon = new(currentTime.Year, currentTime.Month, currentTime.Day, 12, 00, 00);
            nextNoon = nextNoon.AddDays(1);
            double secondsSpan = (nextNoon - currentTime).TotalSeconds;
            gameTime.WeatherIncrementSeconds((float)secondsSpan);
        }
    }
}