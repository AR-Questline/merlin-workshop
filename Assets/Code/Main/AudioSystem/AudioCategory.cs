using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Audio;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem {
    [System.Serializable]
    public class AudioCategory {
        [SerializeReference, ListDrawerSettings(DraggableItems = false, ShowIndexLabels = false, IsReadOnly = true), LabelWidth(150)]
        List<IAudioSource> _sourcesByAddedOrder = new();
        List<IAudioSource> _notCopyrightedSources = new();
        List<IAudioSource> _copyrightedSources = new();

        InfluencerMode _influencerMode;
        InfluencerMode InfluencerMode => _influencerMode ??= World.Only<InfluencerMode>();
        public bool AllowCopyrightedMusic => !InfluencerMode.Enabled;

        public void Add(IAudioSource newSource) {
            _sourcesByAddedOrder.Add(newSource);
            if (!newSource.IsCopyrighted) {
                _notCopyrightedSources.Add(newSource);
            } else {
                _copyrightedSources.Add(newSource);
            }
        }

        public void Remove(IAudioSource oldSource) {
            _sourcesByAddedOrder.Remove(oldSource);
            if (!oldSource.IsCopyrighted) {
                _notCopyrightedSources.Remove(oldSource);
            } else {
                _copyrightedSources.Remove(oldSource);
            }
        }

        public bool Contains(IAudioSource sourceToFind) => _sourcesByAddedOrder.Contains(sourceToFind);
        public bool IsEmpty() => _sourcesByAddedOrder.Count == 0 || _sourcesByAddedOrder.All(s => s?.EventReference().IsNull ?? true);

        public IAudioSource Newest() {
            if (AllowCopyrightedMusic) {
                return _copyrightedSources.LastOrDefault() ?? _notCopyrightedSources.LastOrDefault();
            }
            return _notCopyrightedSources.LastOrDefault();
        }

        public IAudioSource Oldest() {
            if (AllowCopyrightedMusic) {
                return _copyrightedSources.FirstOrDefault() ?? _notCopyrightedSources.FirstOrDefault();
            }
            return _notCopyrightedSources.FirstOrDefault();
        }

        public IAudioSource Random() {
            if (AllowCopyrightedMusic && _copyrightedSources.Count > 0) {
                return RandomUtil.UniformSelect(_copyrightedSources);
            }
            return _notCopyrightedSources.Count > 0 ? RandomUtil.UniformSelect(_notCopyrightedSources) : null;
        }

        public void Clear() {
            _sourcesByAddedOrder.Clear();
            _copyrightedSources.Clear();
            _notCopyrightedSources.Clear();
        }
    }
}