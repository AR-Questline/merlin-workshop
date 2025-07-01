using System;
using System.Collections.Generic;
using CrazyMinnow.SALSA;
using UnityEngine;

namespace Awaken.VendorWrappers.Salsa {
    public class Salsa : MonoBehaviour {
        public AudioSource audioSrc;
        public bool waitForAudioSource;
        public bool useExternalAnalysis;
        public GameObject audioSourceWaitTarget;
        public QueueProcessor queueProcessor;
        public Emoter emoter;
        [Range(0.0f, 1f)]
        public float emphasizerTrigger;
        public float analysisValue;
        private float cachedAnalysisValue;
        private int prevAudioFrequency;
        public int sampleSize = 512;
        private float[] audioSamples;
        public float audioUpdateDelay = 0.08f;
        private float updateTimeCheck;
        [Range(-2048f, 9048f)]
        public int playheadBias = 2800;
        public bool autoAdjustAnalysis = true;
        public bool autoAdjustMicrophone;
        public int microphoneRecordHeadPointer;
        public bool scaleExternalAnalysis;
        public int silencePulseThreshold = 3;
        private int silencePulseCount;
        private bool isSalsaingState;
        public float loCutoff = 0.03f;
        public float hiCutoff = 0.75f;
        public bool useAdvDyn;
        [Range(0.0f, 1f)]
        public float advDynPrimaryBias = 0.5f;
        public bool useAdvDynJitter;
        [Range(0.0f, 1f)]
        public float advDynJitterAmount = 0.25f;
        [Range(0.0f, 1f)]
        public float advDynJitterProb = 0.25f;
        public bool useAdvDynSecondaryMix;
        [Range(0.0f, 1f)]
        public float advDynSecondaryMix;
        public bool useAdvDynRollback;
        [Range(0.0f, 1f)]
        public float advDynRollback = 0.3f;
        [Range(0.0f, 1f)]
        public float globalFrac = 1f;
        public bool usePersistence;
        public bool useTimingsOverride;
        public float globalDurON = 0.08f;
        public float globalDurOFF = 0.06f;
        public float globalDurOffBalance;
        public float originalUpdateDelay;
        public float globalNuanceBalance;
        public bool useEasingOverride;
        public LerpEasings.EasingType globalEasing = LerpEasings.EasingType.CubicOut;
        public List<LipsyncExpression> visemes = new List<LipsyncExpression>();
        private int prevTriggeredIndex = -1;
        private int secondaryMixTrigger = -1;
        private bool isSalsaProcessing;
        public bool configReady = true;
        public bool configNotReadyNotified;
        public bool warnOnNullRefs = true;
        private bool isAppQuitting;
        [SerializeField]
        private bool isDebug;
        [NonSerialized]
        public bool delegatesWired = true;
        public bool useAudioAnalysis = true;
        public bool needAudioSource = true;
        private Salsa.SalsaNotificationArgs _eventNotificationArgs;
        public bool inspStateFoldoutProcessing;
        public bool inspStateFoldoutSettings;
        public bool inspStateFoldoutVisemes;
        public bool inspCollectionDisplayMode;
        public bool inspAnalysisDisplay;
        public bool inspOverrideSecondaryMix;
        public bool inspDetailedTimingAdjustments;
        private bool isPulseInterrupt;
        public bool doEditorConfigPreview;
        public string inspGoOriginalName;
        
        public class SalsaNotificationArgs : EventArgs
        {
            public Salsa salsaInstance;
            public int visemeTrigger;
        }
    }
}