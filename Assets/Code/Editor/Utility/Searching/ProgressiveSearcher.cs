using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Utility.Searching {
    public class ProgressiveSearcher : OdinEditorWindow {
        static TemplatesProvider s_provider;
        static TemplatesProvider Provider => s_provider ??= GetTemplateProvider(); 
        static TemplatesProvider GetTemplateProvider() {
            var provider = new TemplatesProvider();
            provider.StartLoading();
            return provider;
        }

        [SerializeField, TableList(ShowPaging = true, NumberOfItemsPerPage = 20)] List<Result> results = new();

        bool _templatesDone;
        IEnumerator<SceneResources> _sceneEnumerator;

        protected virtual ITemplateQuery[] TemplateQueries { get; }
        protected virtual ISceneQuery[] SceneQueries { get; }

        [Button(ButtonSizes.Large)]
        void NextStep() {
            if (_templatesDone) {
                RunOnSceneProgressive(SceneQueries);
            } else {
                RunOnTemplates(TemplateQueries);
                _templatesDone = true;
            }
        }

        [ShowIf(nameof(_templatesDone)), Button(ButtonSizes.Large)]
        void RerunStep() {
            if (_sceneEnumerator == null) {
                RunOnTemplates(TemplateQueries);
            } else {
                RunOnCurrentScene(SceneQueries);
            }
        }

        [Button(ButtonSizes.Large)]
        void Reset() {
            _templatesDone = false;
            
            _sceneEnumerator?.Dispose();
            _sceneEnumerator = null;
            
            results.Clear();
        }
        
        void RunOnTemplates(ITemplateQuery[] queries) {
            results.Clear();
            foreach (var template in Provider.AllTemplates) {
                if (template is Object obj && obj == null) continue;
                foreach (var query in queries) {
                    query.Run(template, results);
                }
            }
        }

        void RunOnSceneProgressive(ISceneQuery[] queries) {
            _sceneEnumerator ??= SceneEnumerator();

            if (_sceneEnumerator.MoveNext()) {
                try {
                    RunOnCurrentScene(queries);
                } catch (Exception e) {
                    Console.WriteLine(e);
                } finally {
                    _sceneEnumerator.Current?.Dispose();
                }
            } else {
                EditorUtility.DisplayDialog("Search end", "You have searched through all templates and scenes", "ok");
                Reset();
            }
        }
        IEnumerator<SceneResources> SceneEnumerator() => BuildTools.GetAllScenes().Select(path => new SceneResources(path, false)).GetEnumerator();

        void RunOnCurrentScene(ISceneQuery[] queries) {
            results.Clear();
            foreach (var query in queries) {
                query.Run(results);
            }
        }

        public class FilterQuery<TObject> {
            TryGetResult _tryGetResult;
            
            public FilterQuery(TryGetResult tryGetResult) {
                _tryGetResult = tryGetResult;
            }

            public void TryAddResult(TObject obj, List<Result> results) {
                if (_tryGetResult(obj, out var result)) {
                    results.Add(result);
                }
            }
            
            public delegate bool TryGetResult(TObject template, out Result result);
        }
        
        public interface ITemplateQuery {
            void Run(ITemplate template, List<Result> results);
        }

        public class TemplateQuery<TTemplate> : FilterQuery<TTemplate>, ITemplateQuery where TTemplate : ITemplate {
            public TemplateQuery(TryGetResult tryGetResult) : base(tryGetResult) { }
            public void Run(ITemplate template, List<Result> results) {
                if (template is TTemplate t) {
                    TryAddResult(t, results);
                }
            }
        }

        public class AttachmentQuery<TAttachment> : FilterQuery<TAttachment>, ITemplateQuery {
            public AttachmentQuery(TryGetResult tryGetResult) : base(tryGetResult) { }
            public void Run(ITemplate template, List<Result> results) {
                if (template is Component component) {
                    foreach(var attachment in component.GetComponentsInChildren<TAttachment>(true)) {
                        TryAddResult(attachment, results);
                    }
                }
            }
        }

        public interface ISceneQuery {
            void Run(List<Result> results);
        }
        
        public class SceneQuery<TObject> : FilterQuery<TObject>, ISceneQuery where TObject : Object {
            public SceneQuery(TryGetResult tryGetResult) : base(tryGetResult) { }
            public void Run(List<Result> results) {
                foreach (var obj in FindObjectsByType<TObject>(FindObjectsSortMode.None)) {
                    TryAddResult(obj, results);
                }
            }
        }

        [Serializable]
        public struct Result {
            [SerializeField, HideLabel, VerticalGroup(nameof(context))] Object context;
            [SerializeField, HideLabel, VerticalGroup(nameof(note))] string note;

            public Result(Object context, string note) {
                this.context = context;
                this.note = note;
            }
        }
    }
}