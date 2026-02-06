using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Video.Subtitles {
    [SpawnsView(typeof(VSubtitles))]
    public partial class CutsceneSubtitles : SubtitlesBase<Cutscene> {
        readonly VCutsceneBase _vCutscene;
        readonly VSimpleSubtitlesHost _vSimpleSubtitlesHost;
        readonly CutsceneAudioAndSubtitles _cutsceneAudioAndSubtitles;
        
        public override Transform SubtitlesHost => _vSimpleSubtitlesHost.subtitlesHost;
        protected override SubtitlesData.Record[] Records => _cutsceneAudioAndSubtitles.CurrentSubtitles?.records;
        protected override float Time => _vCutscene.TimeElapsed;
        
        public CutsceneSubtitles(VCutsceneBase vCutscene, VSimpleSubtitlesHost simpleSubtitlesHost, CutsceneAudioAndSubtitles cutsceneAudioAndSubtitles) {
            _vCutscene = vCutscene;
            _vSimpleSubtitlesHost = simpleSubtitlesHost;
            _cutsceneAudioAndSubtitles = cutsceneAudioAndSubtitles;
        }
    }
}