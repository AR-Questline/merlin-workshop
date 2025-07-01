using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.MVC.Domains {
    public class MultiSceneLoadOperation : ISceneLoadOperation {
        IEnumerable<ISceneLoadOperation> _enumerable;
        int _count;

        float _progress;
        int _done;
        string _name;
        string _mainSceneName;

        public string Name => _name;
        public bool IsDone => _done == _count;
        public float Progress => (_done + _progress) / _count;

        public IEnumerable<string> MainScenesNames => _mainSceneName.Yield();

        public MultiSceneLoadOperation(IEnumerable<ISceneLoadOperation> operations, string mainSceneName, int count, bool parallel) {
            _enumerable = operations;
            _count = count;
            _name = string.Empty;
            _mainSceneName = mainSceneName;

            if (parallel) {
                ParallelLoadControl().Forget();
            } else {
                SynchronousLoadControl().Forget();
            }

        }

        async UniTaskVoid SynchronousLoadControl() {
            foreach (var current in _enumerable.WhereNotNull()) {
                _name += $", {current.Name}";
                while (!current.IsDone) {
                    _progress = current.Progress;
                    await UniTask.NextFrame();
                }
                _done++;
            }
        }

        async UniTaskVoid ParallelLoadControl() {
            List<ISceneLoadOperation> operations = _enumerable.ToList();
            _name = string.Join(", ", operations.Select(op => op?.Name));
            
            while (operations.Any()) {
                _done += operations.RemoveAll(op => op.IsDone);
                _progress = operations.Sum(op => op.Progress);
                await UniTask.NextFrame();
            }
        }
    }
}