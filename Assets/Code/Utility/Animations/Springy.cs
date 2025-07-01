using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Awaken.Utility.Animations {
    public class SpringyFloat {
        // ========================= Fields

        float _springiness, _damping;

        float _current = 0f;
        float _velocity = 0f;
        float _acceleration = 0f;

        // ========================= Constructors

        public SpringyFloat(float springiness, float damping, float initial = 0f) {
            _springiness = springiness;
            _damping = damping;
            _current = initial;
        }

        // ========================= Updating

        [UnityEngine.Scripting.Preserve]
        public float Update(float targetValue, float dt) {
            dt = Mathf.Min(dt, 0.02f);
            float dtSq = dt * dt;
            _acceleration += _springiness * (targetValue - _current) * dt;
            _velocity += _acceleration * dt;
            _velocity -= _velocity * _damping * dt;
            _current += _velocity * dt + _acceleration * dtSq / 2;
            _acceleration = 0f;
            return _current;
        }

        [UnityEngine.Scripting.Preserve]
        public void Pull(float acceleration) { 
            _acceleration += acceleration;
        }

        [UnityEngine.Scripting.Preserve]
        public void Yank(float impulse) {
            _velocity += impulse;
        }

        [UnityEngine.Scripting.Preserve]
        public void Set(float value) {
            _current = value;
            _velocity = _acceleration = 0f;
        }

        [UnityEngine.Scripting.Preserve]
        public float Get() {
            return _current;
        }
        public static implicit operator float(SpringyFloat sf) => sf._current;
    }

    public class SpringyVector {
        // ========================= Fields

        float _springiness, _damping;

        Vector3 _current = Vector3.zero;
        Vector3 _velocity = Vector3.zero;
        Vector3 _acceleration = Vector3.zero;

        // ========================= Constructors

        public SpringyVector(float springiness, float damping) : this(springiness, damping, Vector3.zero) { }
        public SpringyVector(float springiness, float damping, Vector3 initial) {
            _springiness = springiness;
            _damping = damping;
            _current = initial;
        }

        // ========================= Updating

        [UnityEngine.Scripting.Preserve]
        public Vector3 Update(Vector3 targetValue, float dt) {
            dt = Mathf.Min(dt, 0.02f);
            float dtSq = dt * dt;
            _acceleration += _springiness * (targetValue - _current) * dt;
            _velocity += _acceleration * dt;
            _velocity -= _velocity * _damping * dt;
            _current += _velocity * dt + _acceleration * dtSq / 2;
            _acceleration = Vector3.zero;
            return _current;
        }

        [UnityEngine.Scripting.Preserve]
        public void Pull(Vector3 acceleration) {
            _acceleration += acceleration;
        }
        
        [UnityEngine.Scripting.Preserve]
        public void Yank(Vector3 impulse) {
            _velocity += impulse;
        }

        [UnityEngine.Scripting.Preserve]
        public void Move(Vector3 displacement) {
            _current += displacement;
        }

        [UnityEngine.Scripting.Preserve]
        public void Set(Vector3 value) {
            _current = value;
            _velocity = _acceleration = Vector3.zero;
        }
    }
}
