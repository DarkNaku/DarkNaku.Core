using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkNaku.Core;

namespace DarkNaku.Core {
    public class TaskRunner : SingletonBehaviour<TaskRunner> {
        public static Task Run(IEnumerator coroutine, System.Action<bool> onComplete = null) {
            return new Task(coroutine, onComplete);
        }

        public class Task : CustomYieldInstruction {
            public override bool keepWaiting => !Completed;

            public bool Running { get; private set; }
            public bool Paused { get; private set; }
            public bool Completed { get; private set; }

            private bool _stopped = false;
            private Coroutine _context = null;
            private IEnumerator _coroutine = null;
            private System.Action<bool> _onComplete = null;

            public Task(IEnumerator coroutine, System.Action<bool> onComplete) {
                _coroutine = coroutine;
                _onComplete = onComplete;
                Start();
            }

            public void Start() {
                if (Running) {
                    Debug.LogError("[Task] Start : Task is already running.");
                } else {
                    _context = Instance.StartCoroutine(CoWrapper());
                }
            }

            public void Stop() {
                Running = false;
                _stopped = true;

                if (Running) {
                    Running = false;
                } else {
                    Debug.LogError("[Task] Stop : Task is not running.");
                }
            }

            public void Pause() {
                if (Running) {
                    Paused = true;
                } else {
                    Debug.LogError("[Task] Pause : Task is not running.");
                }
            }

            public void Resume() {
                if (Running) {
                    Paused = false;
                } else {
                    Debug.LogError("[Task] Resume : Task is not running.");
                }
            }

            private IEnumerator CoWrapper() {
                if (Running) yield break;

                Running = true;
                Paused = false;
                Completed = false;

                while (Running) {
                    if (Paused) {
                        yield return null;
                    } else {
                        if (_coroutine == null) break;

                        if (_coroutine.MoveNext()) {
                            yield return _coroutine.Current;
                        } else {
                            Completed = true;
                            break;
                        }
                    }
                }

                _onComplete?.Invoke(Completed);
                Running = false;
            }
        }
    }
}
