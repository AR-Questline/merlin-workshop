using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Awaken.Utility.Collections;
using Unity.Burst;

namespace Awaken.TG.MVC.Elements {
    public readonly struct ModelsSet<T> where T : class, IModel {
        const int AvgModelsCount = 4;
        public static readonly ModelsSet<T> Empty = new ModelsSet<T>(ListExtensions<IModel>.Empty);

        readonly StructList<List<IModel>> _models;

        public bool IsCreated => _models.IsCreated;
        public bool IsFlat => _models.Count == 1;

        public ModelsSet(StructList<List<IModel>> models) {
            _models = models;
            CheckIntegrity();
        }

        public ModelsSet(params List<IModel>[] models) {
            _models = new StructList<List<IModel>>(models);
            CheckIntegrity();
        }

        public ModelsSet<TOut> As<TOut>() where TOut : class, T {
            return new ModelsSet<TOut>(_models);
        }

        public T At(uint index) {
            return (T)_models[0][(int)index];
        }

        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }

        public IndexedEnumerator GetIndexedEnumerator() {
            return new IndexedEnumerator(this);
        }

        public ManagedEnumerator GetManagedEnumerator() {
            return new ManagedEnumerator(this);
        }

        public ReverseEnumerator Reverse() {
            return new ReverseEnumerator(this);
        }

        [BurstDiscard]
        public WhereEnumerator Where(Func<T, bool> predicate) {
            if (predicate == null) {
                throw new ArgumentNullException(nameof(predicate));
            }
            return new(this, predicate);
        }

        [BurstDiscard]
        public SelectEnumerator<TOut> Select<TOut>(Func<T, TOut> selector) {
            if (selector == null) {
                throw new ArgumentNullException(nameof(selector));
            }
            return new(this, selector);
        }

        [Conditional("DEBUG")]
        void CheckIntegrity() {
            if (!_models.IsCreated) {
                throw new ArgumentNullException($"Models in {nameof(ModelsSet<T>)} is not created");
            }
            if (_models.Count == 0) {
                throw new ArgumentException($"Models in {nameof(ModelsSet<T>)} is empty (outer list must contain at least one inner list)");
            }
        }

        public struct Enumerator {
            List2DEnumerator<IModel> _modelsEnumerator;

            public Enumerator(ModelsSet<T> models) {
                _modelsEnumerator = new List2DEnumerator<IModel>(models._models);
            }

            Enumerator(List2DEnumerator<IModel> enumerator) {
                _modelsEnumerator = enumerator;
            }

            public bool MoveNext() => _modelsEnumerator.MoveNext();

            public T Current => (T)_modelsEnumerator.Current;

            [UnityEngine.Scripting.Preserve]
            public Enumerator GetEnumerator() => this;

            public Enumerator Copy() {
                return new Enumerator(_modelsEnumerator.Copy());
            }
        }

        public struct IndexedEnumerator {
            List2DEnumerator<IModel> _modelsEnumerator;
            int _index;

            public IndexedEnumerator(ModelsSet<T> models) {
                _modelsEnumerator = new List2DEnumerator<IModel>(models._models);
                _index = -1;
            }

            public bool MoveNext() {
                if (_modelsEnumerator.MoveNext()) {
                    ++_index;
                    return true;
                }
                return false;
            }

            public (int, T) Current => (_index, (T)_modelsEnumerator.Current);

            public IndexedEnumerator GetEnumerator() => this;
        }

        public struct ManagedEnumerator : IEnumerator<T>, IEnumerable<T>, IEnumerator {
            List2DEnumerator<IModel> _modelsEnumerator;

            public ManagedEnumerator(ModelsSet<T> models) {
                _modelsEnumerator = new List2DEnumerator<IModel>(models._models);
            }

            public bool MoveNext() => _modelsEnumerator.MoveNext();
            public void Reset() => throw new Exception("Not supported");

            public T Current => (T)_modelsEnumerator.Current;
            object IEnumerator.Current => Current;

            public void Dispose() {}

            [UnityEngine.Scripting.Preserve]
            public ManagedEnumerator GetEnumerator() => this;
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => this;
        }

        public struct WhereEnumerator : IEnumerator<T>, IEnumerable<T> {
            List2DEnumerator<IModel> _modelsEnumerator;
            Func<T, bool> _predicate;

            public WhereEnumerator(ModelsSet<T> models, Func<T, bool> predicate) {
                _modelsEnumerator = new List2DEnumerator<IModel>(models._models);
                _predicate = predicate;
            }

            public bool MoveNext() {
                bool movedNext;
                while ((movedNext = _modelsEnumerator.MoveNext()) && !_predicate(Current)) {}
                return movedNext;
            }
            public void Reset() => throw new Exception("Not supported");

            public WhereEnumerator GetEnumerator() => this;
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => this;

            public T Current => (T)_modelsEnumerator.Current;
            object IEnumerator.Current => Current;

            public void Dispose() {}
        }

        public struct SelectEnumerator<TOut> : IEnumerator<TOut>, IEnumerable<TOut> {
            List2DEnumerator<IModel> _modelsEnumerator;
            Func<T, TOut> _selector;

            public SelectEnumerator(ModelsSet<T> models, Func<T, TOut> selector) {
                _modelsEnumerator = new List2DEnumerator<IModel>(models._models);
                _selector = selector;
            }

            public bool MoveNext() {
                return _modelsEnumerator.MoveNext();
            }
            public void Reset() => throw new Exception("Not supported");

            public SelectEnumerator<TOut> GetEnumerator() => this;
            IEnumerator<TOut> IEnumerable<TOut>.GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => this;

            public TOut Current => _selector((T)_modelsEnumerator.Current);
            object IEnumerator.Current => Current;

            public void Dispose() {}
        }

        public struct ReverseEnumerator {
            List2DReverseEnumerator<IModel> _modelsEnumerator;

            public ReverseEnumerator(ModelsSet<T> models) {
                _modelsEnumerator = new List2DReverseEnumerator<IModel>(models._models);
            }

            public bool MoveNext() => _modelsEnumerator.MoveNext();

            public T Current => (T)_modelsEnumerator.Current;

            public ReverseEnumerator GetEnumerator() => this;
        }

        // LINQ replacements
        public T First() {
            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            if (!modelsEnumerator.MoveNext()) {
                throw new InvalidOperationException("Empty models set");
            }
            return (T)modelsEnumerator.Current;
        }

        public T First(Func<T, bool> predicate) {
            if (predicate == null) {
                throw new ArgumentNullException(nameof(predicate));
            }

            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            while (modelsEnumerator.MoveNext()) {
                var model = (T)modelsEnumerator.Current;
                if (predicate(model)) {
                    return model;
                }
            }
            throw new InvalidOperationException("No element satisfied the condition");
        }

        public T FirstOrDefault(T defaultValue = default) {
            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            if (modelsEnumerator.MoveNext()) {
                return (T)modelsEnumerator.Current;
            }
            return defaultValue;
        }

        public T FirstOrDefault(Func<T, bool> predicate, T defaultValue = default) {
            if (predicate == null) {
                throw new ArgumentNullException(nameof(predicate));
            }

            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            while (modelsEnumerator.MoveNext()) {
                var model = (T)modelsEnumerator.Current;
                if (predicate(model)) {
                    return model;
                }
            }
            return defaultValue;
        }

        [UnityEngine.Scripting.Preserve]
        public T Last() {
            var modelsEnumerator = Reverse();
            if (!modelsEnumerator.MoveNext()) {
                throw new InvalidOperationException("Empty models set");
            }
            return modelsEnumerator.Current;
        }

        [UnityEngine.Scripting.Preserve]
        public T Last(Func<T, bool> predicate) {
            if (predicate == null) {
                throw new ArgumentNullException(nameof(predicate));
            }

            var modelsEnumerator = Reverse();
            while (modelsEnumerator.MoveNext()) {
                var model = modelsEnumerator.Current;
                if (predicate(model)) {
                    return model;
                }
            }
            throw new InvalidOperationException("No element satisfied the condition");
        }

        [UnityEngine.Scripting.Preserve]
        public T LastOrDefault(T defaultValue = default) {
            var modelsEnumerator = Reverse();
            if (modelsEnumerator.MoveNext()) {
                return modelsEnumerator.Current;
            }
            return defaultValue;
        }

        public T LastOrDefault(Func<T, bool> predicate, T defaultValue = default) {
            if (predicate == null) {
                throw new ArgumentNullException(nameof(predicate));
            }

            var modelsEnumerator = Reverse();
            while (modelsEnumerator.MoveNext()) {
                var model = modelsEnumerator.Current;
                if (predicate(model)) {
                    return model;
                }
            }
            return defaultValue;
        }

        public bool Any(Func<T, bool> predicate) {
            if (predicate == null) {
                throw new ArgumentNullException(nameof(predicate));
            }

            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            while (modelsEnumerator.MoveNext()) {
                if (predicate((T)modelsEnumerator.Current)) {
                    return true;
                }
            }
            return false;
        }

        public bool Any() {
            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            return modelsEnumerator.MoveNext();
        }

        public bool All(Func<T, bool> predicate) {
            if (predicate == null) {
                throw new ArgumentNullException(nameof(predicate));
            }

            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            while (modelsEnumerator.MoveNext()) {
                if (!predicate((T)modelsEnumerator.Current)) {
                    return false;
                }
            }

            return true;
        }

        public bool IsEmpty() {
            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            return !modelsEnumerator.MoveNext();
        }

        public bool CountEqualTo(int count) {
            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            while (count > 0 && modelsEnumerator.MoveNext()) {
                count--;
            }
            return count == 0 && !modelsEnumerator.MoveNext();
        }

        public bool CountGreaterThan(int count) {
            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            while (count > 0 && modelsEnumerator.MoveNext()) {
                count--;
            }
            return count == 0 && modelsEnumerator.MoveNext();
        }

        [UnityEngine.Scripting.Preserve]
        public bool CountGreaterOrEqualTo(int count) {
            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            while (count > 0 && modelsEnumerator.MoveNext()) {
                count--;
            }
            return count == 0;
        }

        public bool CountLessThan(int count) {
            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            while (count > 0 && modelsEnumerator.MoveNext()) {
                count--;
            }
            return count > 0;
        }

        public bool CountLessOrEqualTo(int count) {
            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            while (count > 0 && modelsEnumerator.MoveNext()) {
                count--;
            }
            return count > 0 || (count == 0 && !modelsEnumerator.MoveNext());
        }

        public uint Count(Func<T, bool> predicate) {
            if (predicate == null) {
                throw new ArgumentNullException(nameof(predicate));
            }
            var count = 0u;
            foreach (var element in this) {
                if (predicate(element)) {
                    count++;
                }
            }
            return count;
        }

        public uint Count() {
            var count = 0u;
            foreach (var _ in this) {
                count++;
            }
            return count;
        }

        public int Sum(Func<T, int> selector) {
            if (selector == null) {
                throw new ArgumentNullException(nameof(selector));
            }
            int sum = 0;
            foreach (var model in this) {
                sum += selector(model);
            }
            return sum;
        }

        public float Sum(Func<T, float> selector) {
            if (selector == null) {
                throw new ArgumentNullException(nameof(selector));
            }
            float sum = 0;
            foreach (var model in this) {
                sum += selector(model);
            }
            return sum;
        }

        public T MinBy<TKey>(Func<T, TKey> selector) where TKey : IComparable<TKey> {
            if (selector == null) {
                throw new ArgumentNullException(nameof(selector));
            }

            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            if (!modelsEnumerator.MoveNext()) {
                return null;
            }

            var minElement = (T)modelsEnumerator.Current;
            var minValue = selector(minElement);
            while (modelsEnumerator.MoveNext()) {
                var element = (T)modelsEnumerator.Current;
                var value = selector(element);
                if (value.CompareTo(minValue) < 0) {
                    minElement = element;
                    minValue = value;
                }
            }

            return minElement;
        }

        public T MaxBy<TKey>(Func<T, TKey> selector) where TKey : IComparable<TKey> {
            if (selector == null) {
                throw new ArgumentNullException(nameof(selector));
            }

            var modelsEnumerator = new List2DEnumerator<IModel>(_models);
            if (!modelsEnumerator.MoveNext()) {
                return null;
            }

            var maxElement = (T)modelsEnumerator.Current;
            var maxValue = selector(maxElement);
            while (modelsEnumerator.MoveNext()) {
                var element = (T)modelsEnumerator.Current;
                var value = selector(element);
                if (value.CompareTo(maxValue) > 0) {
                    maxElement = element;
                    maxValue = value;
                }
            }

            return maxElement;
        }

        public T[] ToArraySlow() {
            var array = new T[Count()];
            int i = 0;
            foreach (var model in this) {
                array[i++] = model;
            }
            return array;
        }

        public void FillList(List<T> buffer) {
            buffer.Clear();
            var capacity = buffer.Count + _models.Count * AvgModelsCount;
            buffer.EnsureCapacity(capacity);
            foreach (var model in this) {
                buffer.Add(model);
            }
        }

        public int IndexOf<U>(U equatable) where U : IEquatable<T> {
            var flatModels = _models[0];
            for (var i = 0; i < flatModels.Count; i++) {
                if(equatable.Equals((T)flatModels[i])) {
                    return i;
                }
            }
            return -1;
        }
    }
}
