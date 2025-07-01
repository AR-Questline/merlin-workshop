using System;

namespace Awaken.TG.Main.AudioSystem.Biomes {
    public class CombatPriorityManager : PriorityManager {
        int HighestTierAlerted => Services?.TryGet<AudioCore>()?.HighestTierAlerted ?? 0;
        int _currentlyPlayedTier = -1;

        public void Reset() {
            _currentlyPlayedTier = -1;
        }

        protected override void FindNewPriorityAudioSource() {
            ValidateCurrentlyPlayingAudioSource();
            if (priorityList.Count <= 0) {
                return;
            }

            int tier = Math.Min(priorityList.Count - 1, HighestTierAlerted);
            IAudioSource newAudioSource;
            do {
                newAudioSource = priorityList[tier].Random();
                if (newAudioSource != null) {
                    break;
                }
                tier--;
            } while (tier >= 0);

            if (newAudioSource == null) {
                return;
            }

            if (_currentlyPlayedTier == tier && AudioPlaying != null && !AudioPlaying.ShouldBeReplacedBy(newAudioSource)) {
                return;
            }
            
            _currentlyPlayedTier = tier;
            // If same sound track only update reference
            if (AudioPlaying != null && AudioPlaying.EventReference().Guid == newAudioSource.EventReference().Guid) {
                AudioPlaying = newAudioSource;
                return;
            }

            // Run new sound
            AudioPlaying = newAudioSource;
            bool isPlaying = false;//Emitter.IsPlaying();
            if (isPlaying) {
                SendAudioEvent();
            } else {
                ChangeAudioEvent();
            }
        }
    }
}