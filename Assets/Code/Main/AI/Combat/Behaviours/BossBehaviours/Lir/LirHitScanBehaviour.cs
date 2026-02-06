using System;
using Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG {
    [Serializable]
    public class LirHitScanBehaviour : HitScanBehaviour {
        [SerializeField] int maxCastsInRow = 4;
        
        int _castsCounter;

        public override bool CanBeUsed => false;
        public override bool CanBeInterrupted => true;
        
        protected override bool StartBehaviour() {
            _castsCounter = 0;
            return base.StartBehaviour();
        }

        protected override UniTask CastSpell(bool returnFireballInHandAfterSpawned = true) {
            var castSpellResult = base.CastSpell(returnFireballInHandAfterSpawned);
            _castsCounter++;
            if (_castsCounter >= maxCastsInRow) {
                ParentModel.StopCurrentBehaviour(true);
            }
            return castSpellResult;
        }
    }
}