using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DarkNaku.Core {
    public class TaskRunner : SingletonBehaviour<TaskRunner> {
        public class Task : CustomYieldInstruction {
            public class CompleteEvent : UnityEvent<bool> { }

            public override bool keepWaiting => !Completed;

            public bool Running { get; private set; }
            public bool Paused { get; private set; }
            public bool Completed { get; private set; }

            private CompleteEvent _onComplete = new CompleteEvent();
            public CompleteEvent OnComplete => _onComplete;

            private bool _stopped = false;
            private Coroutine _context = null;
            private IEnumerator _coroutine = null;

            public Task(IEnumerator coroutine) {
                _coroutine = coroutine;
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

        public static Task Run(IEnumerator coroutine) {
            return new Task(coroutine);
        }

        protected override void OnInstantiate() {
            DontDestroyOnLoad(gameObject);
        }
    }
}
