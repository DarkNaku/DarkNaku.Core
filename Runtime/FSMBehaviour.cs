﻿using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace DarkNaku.Core {
    public abstract class FSMBehaviour<S, M> : MonoBehaviour
                                where S : struct, IConvertible, IComparable
                                where M : FSMBehaviour<S, M> {

        [System.Serializable]
        public class TransitionEvent : UnityEvent<S, S> {
        }

        private Dictionary<S, FSMState<S, M>> _states = new Dictionary<S, FSMState<S, M>>();

        private TransitionEvent _onTransition = null;
        public TransitionEvent OnTransition {
            get {
                if (_onTransition == null) {
                    _onTransition = new TransitionEvent();
                }

                return _onTransition;
            }
        }

        private S _state = default;
        public S State {
            get { return _state; }
            protected set {
                Debug.AssertFormat(_states.ContainsKey(value),
                    "[{0}] SetState : {1} is not on the list of states.", typeof(M).ToString(), value.ToString());

                if (_states.ContainsKey(value) == false) return;
                if (value.CompareTo(_state) == 0) return;

                var prev = _state;
                _states[_state].OnLeave();
                _state = value;
                var state = _states[_state].OnEnter();
                OnTransition?.Invoke(prev, _state);
                State = state;
            }
        }

        public bool Paused { get; set; }

        private bool _initialized = false;

        public void TriggerEvent<T>(T t) {
            _states[_state].OnTriggerEvent(t);
        }

        public void TriggerEvent<T1, T2>(T1 t1, T2 t2) {
            _states[_state].OnTriggerEvent(t1, t2);
        }

        public void TriggerEvent<T1, T2, T3>(T1 t1, T2 t2, T3 t3) {
            _states[_state].OnTriggerEvent(t1, t2, t3);
        }

        protected void Initialize(S state) {
            if (_states.ContainsKey(state) == false) {
                Debug.LogFormat("[{0}] Initialize : {1} is not on the list of states.", typeof(M).ToString(), state.ToString());
                return;
            }

            if (_initialized) return;

            _state = state;
            _initialized = true;
            OnInitialized();
        }

        protected void FixedUpdate() {
            if (_initialized == false) return;
            if (Paused) return;

            State = _states[State].FixedUpdate();
            OnFixedUpdate();
        }

        protected void Update() {
            if (_initialized == false) return;
            if (Paused) return;

            State = _states[State].Update();
            OnUpdate();
        }

        protected void LateUpdate() {
            if (_initialized == false) return;
            if (Paused) return;

            State = _states[State].LateUpdate();
            OnLateUpdate();
        }

        protected virtual void OnInitialized() { }
        protected virtual void OnFixedUpdate() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnLateUpdate() { }

        protected void AddState(FSMState<S, M> state) {
            Debug.AssertFormat(state != null,
                "[{0}] AddState : State is null.", typeof(M).ToString());

            if (state == null) return;

            state.Initialize(this as M);
            _states.Add(state.State, state);
        }

        protected void RemoveState(S state) {
            Debug.AssertFormat(_states.ContainsKey(state),
                "[{0}] RemoveState : {1} is not on the list of states.", typeof(M).ToString(), state.ToString());

            if (_states.ContainsKey(state)) _states.Remove(state);
        }
    }
}