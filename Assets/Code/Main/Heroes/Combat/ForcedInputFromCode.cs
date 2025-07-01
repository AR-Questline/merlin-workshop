using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class ForcedInputFromCode : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        public Vector2 InputAcceleration { get; private set; }
        HeroControllerData Data => ParentModel.Template.heroControllerData;
        Vector2 _firstVector, _secondVector, _thirdVector;
        bool _initialized;
        float _shakeMagnitude;
        
        protected override void OnInitialize() {
            this.GetOrCreateTimeDependent().WithUpdate(ProcessUpdate);
            ShakeCamera();
        }
        
        void ShakeCamera() {
            _firstVector = Vector3.one;
            _firstVector = _firstVector.normalized * Data.firstVectorSize.RandomPick();
            _secondVector = _firstVector.normalized * Data.secondVectorSize.RandomPick();
            _thirdVector = _firstVector.normalized * Data.thirdVectorSize.RandomPick();
            InputAcceleration = Vector3.zero;
            _shakeMagnitude = 0;
            _initialized = true;
        }

        void ProcessUpdate(float deltaTime) {
            if (!_initialized) {
                return;
            }
            
            _firstVector = _firstVector.Rotate(deltaTime * Data.firstVectorRotationSpeed);
            _secondVector = _secondVector.Rotate(deltaTime * Data.secondVectorRotationSpeed);
            _thirdVector = _thirdVector.Rotate(deltaTime * Data.thirdVectorRotationSpeed);
            InputAcceleration = (_firstVector + _secondVector + _thirdVector) * _shakeMagnitude;

#if UNITY_EDITOR
            var center = ParentModel.MainView.transform.position + ParentModel.MainView.transform.forward;
            Debug.DrawRay(center, _firstVector, Color.red);
            center = new Vector3(center.x + _firstVector.x, center.y + _firstVector.y, center.z);
            Debug.DrawRay(center, _secondVector, Color.green);
            center = new Vector3(center.x + _secondVector.x, center.y + _secondVector.y, center.z);
            Debug.DrawRay(center, _thirdVector, Color.cyan);
#endif
        }
        
        // === Public API
        public void UpdateInputMagnitude(float value) {
            _shakeMagnitude = value;
        }
    }
}
