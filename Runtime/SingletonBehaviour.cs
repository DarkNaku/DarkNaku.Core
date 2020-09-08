using UnityEngine;

namespace DarkNaku.Core {
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour {
        private static object _lock = new object();
        private static bool _instantiated = false;
        private static bool _isDestroyed = false;

        private static T _instance = null;
        public static T Instance {
            get {
                lock (_lock) {
                    if (_isDestroyed) return null;

                    if (_instance == null) {
                        _instance = FindObjectOfType<T>();

                        if (_instance == null) {
                            GameObject go = new GameObject();
                            _instance = go.AddComponent<T>();
                            go.name = "[SINGLETON] " + typeof(T).ToString();
                        }
                    }

                    SingletonBehaviour<T> singleton = _instance as SingletonBehaviour<T>;

                    if (_instantiated == false) {
                        singleton.OnInstantiate();
                        _instantiated = true;
                    }

                    return _instance;
                }
            }
        }

        protected void OnDestroy() {
            OnBeforeDestroy();
            _isDestroyed = true;
            _instance = null;
        }

        protected virtual void OnInstantiate() { 
        }

        protected virtual void OnBeforeDestroy() { 
        }
    }
}