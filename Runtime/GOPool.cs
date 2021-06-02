using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DarkNaku.Core {
    public interface IGOPoolItem {
        bool IsAvailable { get; }
        void OnInstantiate();
        void OnReady();
        void OnAbandon();
    }

    public sealed class GOPool : SingletonScriptable<GOPool> {
        public class PoolData {
            private GameObject _mold = null;
            public GameObject Mold => _mold;

            private List<GameObject> _pool = null;
            public List<GameObject> Pool => _pool;

            public PoolData(GameObject mold) {
                _mold = mold;
                _pool = new List<GameObject>();
            }
        }

        [Header("Prefabs Path (Path under Resources)")]
        [SerializeField] string _prefabPath = "Prefabs";

        private Dictionary<string, PoolData> _pools = new Dictionary<string, PoolData>();
        private Queue<GameObject> _trashs = new Queue<GameObject>();
        private List<GameObject> _reservedAbandons = new List<GameObject>();
        private bool _isRecycleWorking = false;
        private Transform _root = null;

#if UNITY_EDITOR
        [MenuItem("DarkNaku/GameObject Pool Settings")]
        public static void SelectGOPool() {
            Selection.activeObject = Instance;
        }
#endif

        public static bool WarmUp(string key, int count) {
            return Instance._WarmUp(key, count);
        }

        public static TaskRunner.Task WarmUpAsync(string key, int count, System.Action<bool> onComplete = null) {
            return TaskRunner.Run(Instance.CoWarmUp(key, count, onComplete));
        }

        public static T GetItem<T>(string key, Transform parent) where T : Component {
            return Instance._GetItem<T>(key, parent);
        }

        public static GameObject GetItem(string key, Transform parent){
            return Instance._GetItem(key, parent);
        }

        public static void Abandon(GameObject item) {
            Instance._Abandon(item);
        }

        public static TaskRunner.Task Abandon(GameObject item, float delay, System.Action<bool> onComplete = null) {
            return TaskRunner.Run(Instance.CoAbandon(item, delay, onComplete));
        }

        protected override void OnLoaded() {
            _root = new GameObject("GOPool").transform;
            DontDestroyOnLoad(_root.gameObject);
        }

        private bool _WarmUp(string key, int count) {
            if (_pools.ContainsKey(key) == false) {
                if (CreateMold(key) == false) return false;
            }

            var data = _pools[key];

            while (data.Pool.Count < count) {
                CreateItem(data);
            }

            return true;
        }

        private IEnumerator CoWarmUp(string key, int count, System.Action<bool> onComplete) {
            if (_pools.ContainsKey(key) == false) {
                if (CreateMold(key) == false) {
                    onComplete?.Invoke(false);
                    yield break;
                }
            }

            var data = _pools[key];

            while (data.Pool.Count < count) {
                CreateItem(data);
                yield return null;
            }

            onComplete?.Invoke(true);
        }

        private T _GetItem<T>(string key, Transform parent) where T : Component {
            var item = _GetItem(key, parent);

            if (item == null) {
                return null;
            } else {
                return item.GetComponent<T>();
            }
        }

        private GameObject _GetItem(string key, Transform parent) {
            if (string.IsNullOrEmpty(key)) {
                Debug.LogError("[GOPool] GetItem : Key is wrong.");
                return null;
            }

            if (_pools.ContainsKey(key) == false) {
                if (CreateMold(key) == false) return null;
            }

            GameObject item = null;
            var pool = _pools[key].Pool;

            for (int i = 0; i < pool.Count; i++) {
                if (IsAvailable(pool[i])) {
                    item = pool[i];
                    break;
                }
            }

            if (item == null) {
                item = CreateItem(_pools[key]);
            }

            if (parent == null) {
                item.transform.SetParent(_root);
            } else {
                item.transform.SetParent(parent);
            }

            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one;

            var pi = item.GetComponent(typeof(IGOPoolItem)) as IGOPoolItem;

            if (pi == null) {
                item.SetActive(true);
            } else {
                pi.OnReady();
            }

            return item;
        }

        private bool CreateMold(string key) {
            GameObject mold = Resources.Load<GameObject>(_prefabPath + "/" + key);

            if (mold == null) {
                return false;
            } else {
                _pools.Add(key, new PoolData(mold));
                return true;
            }
        }

        private GameObject CreateItem(PoolData data) { 
            var item = Instantiate(data.Mold) as GameObject;
            item.name = item.name.Replace("(Clone)", "");

            var pi = item.GetComponent(typeof(IGOPoolItem)) as IGOPoolItem;

            if (pi != null) {
                pi.OnInstantiate();
            }

            data.Pool.Add(item);

            return item;
        }

        private bool IsAvailable(GameObject go) {
            Debug.Assert(go != null, "[GOPool] IsAvailable : GameObject is null.");
            
            if (go == null) return false;
            if (_trashs.Contains(go)) return false;

            var item = go.GetComponent(typeof(IGOPoolItem)) as IGOPoolItem;

            if (item == null) {
                return go.activeSelf == false;
            } else {
                return item.IsAvailable;
            }
        }

        private IEnumerator CoAbandon(GameObject item, float delay, System.Action<bool> onComplete) {
            if (_reservedAbandons.Contains(item)) yield break;

            _reservedAbandons.Add(item);

            var elapsed = 0f;

            while (elapsed < delay) {
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (_reservedAbandons.Contains(item)) {
                _Abandon(item);
                _reservedAbandons.Remove(item);
                onComplete?.Invoke(true);
            } else {
                onComplete?.Invoke(false);
            }
        }

        private void _Abandon(GameObject item) {
            Debug.Assert(item != null, "[GOPool] Abandon : 'item' is null.");

            if (item == null) return;
            if (_trashs.Contains(item)) return;

            _trashs.Enqueue(item);

            var pi = item.GetComponent(typeof(IGOPoolItem)) as IGOPoolItem;

            if (pi == null) {
                item.SetActive(false);
            } else {
                pi.OnAbandon();
            }

            if (_isRecycleWorking) return;

            TaskRunner.Run(CoRecycle(item));
        }

        private IEnumerator CoRecycle(GameObject item) {
            Debug.Assert(_isRecycleWorking == false, "[GOPool] CoRecycle : Recycler already working.");

            if (_isRecycleWorking) yield break;

            _isRecycleWorking = true;

            yield return new WaitForEndOfFrame();

            while (_trashs.Count > 0) {
                GameObject trash = _trashs.Dequeue();
                trash.transform.SetParent(_root);
            }

            _isRecycleWorking = false;
        }

        private void _Clear() {
            foreach (var data in _pools.Values) {
                while (data.Pool.Count > 0) {
                    var item = data.Pool[0];
                    Destroy(item);
                    data.Pool.Remove(item);
                }
            }

            _pools.Clear();
        }
    }
}