using Awaken.Utility;
using System;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.MVC.Utils {
    /// <summary>
    /// Interface just to be able to show <see cref="WeakModelRef{T}"/> in ModelsDebug
    /// Because Models Debug can not have inspector for generics
    /// </summary>
    public interface IWeakModelRef {
        string ID { get; }
    }
    /// <summary>
    /// Allows you to refer to a model without introducing a hard dependency.
    /// Useful when you expect the model to disappear from the World at some point.
    /// </summary>
    [Serializable]
    public partial struct WeakModelRef<T> : IWeakModelRef, IEquatable<WeakModelRef<T>> where T : class, IModel {
        public ushort TypeForSerialization => SavedTypes.WeakModelRef;

        // === Properties

        [Saved] public string id;
        public string ID => id;

        public bool IsSet => ID != null; 

        // === Constructors

        public WeakModelRef(string id) {
            this.id = id;
        }

        public WeakModelRef(T model) {
            // we allow null ID references - such a reference always resolves to null
            id = model?.ID;
        }
        
        public static WeakModelRef<T> Empty => new WeakModelRef<T>();

        // === Operation

        /// <summary>
        /// Retrieves the model. Can return null if the model was discarded.
        /// </summary>
        public readonly T Get() {
            if (id == null) return null;
            return World.ByID<T>(this.id);
        }

        /// <summary>
        /// Retrieves the model. Can out null if the model was discarded.
        /// </summary>
        public readonly bool TryGet(out T model) {
            model = Get();
            return model != null;
        }

        public readonly bool Exists() {
            return Get() != null;
        }
        
        public static implicit operator WeakModelRef<T>(T model) {
            return new WeakModelRef<T>(model);
        }

        public static implicit operator T(WeakModelRef<T> modelRef) => modelRef.Get();

        public bool Equals(WeakModelRef<T> other) {
            return id == other.id;
        }

        public override bool Equals(object obj) {
            return obj is WeakModelRef<T> other && Equals(other);
        }

        public override int GetHashCode() {
            return id?.GetHashCode() ?? 0;
        }
    }
}
