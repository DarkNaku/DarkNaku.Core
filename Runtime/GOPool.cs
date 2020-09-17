using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DarkNaku.Core {
    public interface IGOPoolItem {
        bool IsAvailable { get; }
        void OnInstantiate();
        void OnReady();
        void OnAbandon();
    }

    public class GOPool : SingletonBehaviour<GOPool> {
        private struct LabelHandleData {
            public AsyncOperationHandle Handle { get; private set; }
            public bool IsDontReleaseOnClear { get; private set; }
            public LabelHandleData(AsyncOperationHandle handle, bool isDontReleaseOnClear) {
                Handle = handle;
                IsDontReleaseOnClear = isDontReleaseOnClear;
            }
        }

        private struct MoldData {
            public GameObject Mold { get; private set; }
            public bool IsDontReleaseOnClear { get; private set; }
            public MoldData (GameObject mold, bool isDontReleaseOnClear) {
                Mold = mold;
                IsDontReleaseOnClear = isDontReleaseOnClear;
            }
        }

        private Dictionary<string, LabelHandleData> _labelHandles = new Dictionary<string, LabelHandleData>();
        private Dictionary<string, MoldData> _molds = new Dictionary<string, MoldData>();
        private Dictionary<string, List<GameObject>> _pools = new Dictionary<string, List<GameObject>>();
        private Queue<GameObject> _trashs = new Queue<GameObject>();
        private List<string> _loadings = new List<string>();
        private List<GameObject> _reservedAbandons = new List<GameObject>();
        private bool _isRecycleWorking = false;

        public static Coroutine RegisterLabel(string label, System.Action<float> onProgress, System.Action onComplete, bool isDontReleaseOnClear = false) {
            return Instance._RegisterLabel(label, onProgress, onComplete, isDontReleaseOnClear);
        }

        public static Coroutine RegisterLabels(IList<string> labels, System.Action<float> onProgress, System.Action onComplete, bool isDontReleaseOnClear) {
            return Instance._RegisterLabels(labels, onProgress, onComplete, isDontReleaseOnClear);
        }

        public static bool WarmUp(string key, int count) {
            return Instance._WarmUp(key, count);
        }

        public static Coroutine WarmUpAsync(string key, int count, System.Action<bool> onComplete) {
            return Instance._WarmUpAsync(key, count, onComplete);
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

        public static Coroutine Abandon(GameObject item, float delay, System.Action<bool> onComplete = null) {
            return Instance._Abandon(item, delay, onComplete);
        }

        private Coroutine _RegisterLabel(string label, System.Action<float> onProgress, System.Action onComplete, bool isDontReleaseOnClear) {
            return StartCoroutine(CoRegisterLabels(new List<string>(){ label }, onProgress, onComplete, isDontReleaseOnClear));
        }

        private Coroutine _RegisterLabels(IList<string> labels, System.Action<float> onProgress, System.Action onComplete, bool isDontReleaseOnClear) {
            return StartCoroutine(CoRegisterLabels(labels, onProgress, onComplete, isDontReleaseOnClear));
        }

        private IEnumerator CoRegisterLabels(IList<string> labels, System.Action<float> onProgress, System.Action onComplete, bool isDontReleaseOnClear) {
            Debug.Assert(labels != null, "[GOPool] CoRegisterLables : List is null.");

            if (labels == null) {
                onComplete();
                yield break;
            }

#if UNITY_EDITOR
            if (ConfirmLabels(labels) == false) {
                onComplete();
                yield break;
            }
#endif

            var percentPerLabel = 1f / labels.Count;

            for (int i = 0; i < labels.Count; i++) {
                if (_labelHandles.ContainsKey(labels[i])) {
                    Debug.AssertFormat(labels != null, "[GOPool] CoRegisterLables : Label already registed - {0}", labels[i]);
                    continue;
                }

                var handle = Addressables.LoadAssetsAsync<GameObject>(labels[i], (go) => {
                    if (_molds.ContainsKey(go.name)) {
                        Debug.AssertFormat(labels != null, "[GOPool] CoRegisterLables : Prefab already registed - {0}", go.name);
                    } else {
                        _molds.Add(go.name, new MoldData(go, isDontReleaseOnClear));
                        _pools.Add(go.name, new List<GameObject>());
                    }
                }, true);

                if (handle.IsValid() == false) {
                    Debug.LogFormat("[GOPool] CoRegisterLables : Label load fail - {0}", labels[i]);
                    continue;
                }

                var prevPercent = handle.PercentComplete;

                while (handle.PercentComplete < 1F) {
                    yield return null;

                    if (handle.PercentComplete > prevPercent) {
                        prevPercent = handle.PercentComplete;

                        if (onProgress != null) {
                            onProgress((percentPerLabel * i) + (percentPerLabel * handle.PercentComplete));
                        }
                    }
                }

                yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded) {
                    if (onProgress != null) {
                        onProgress(percentPerLabel * (i + 1));
                    }

                    _labelHandles.Add(labels[i], new LabelHandleData(handle, isDontReleaseOnClear));
                }
            }

            if (onComplete != null) onComplete();
        }

#if UNITY_EDITOR
        private bool ConfirmLabels(IList<string> labels) {
            Debug.Assert(labels != null, "[GOPool] ConfirmLabels : List is null.");

            var comfirmLabels = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.GetLabels();

            for (int i = 0; i < labels.Count; i++) {
                if (comfirmLabels.Contains(labels[i]) == false) {
                    return false;
                }
            }

            return true;
        }
#endif

        private bool _WarmUp(string key, int count) {
            if (_molds.ContainsKey(key) == false) {
                Debug.AssertFormat(_molds.ContainsKey(key), "[GOPool] WarmUp : Not on mold list - {0}", key);
                return false;
            }
            
            if (_pools.ContainsKey(key) == false) {
                Debug.LogWarningFormat("[GOPool] WarmUp : Not on pool list - {0}", key);
                _pools.Add(key, new List<GameObject>());
            }

            var pool = _pools[key];

            while (pool.Count < count) {
                var item = CreateItem(key);
                pool.Add(item);
            }

            return true;
        }

        private Coroutine _WarmUpAsync(string key, int count, System.Action<bool> onComplete) {
            return StartCoroutine(CoWarmUp(key, count, onComplete));
        }

        private IEnumerator CoWarmUp(string key, int count, System.Action<bool> onComplete) {
            if (_molds.ContainsKey(key) == false) {
                Debug.AssertFormat(_molds.ContainsKey(key), "[GOPool] WarmUp : Not on mold list - {0}", key);
                onComplete?.Invoke(false);
            }
            
            if (_pools.ContainsKey(key) == false) {
                Debug.LogWarningFormat("[GOPool] WarmUp : Not on pool list - {0}", key);
                _pools.Add(key, new List<GameObject>());
            }

            var pool = _pools[key];

            while (pool.Count < count) {
                var item = CreateItem(key);
                pool.Add(item);
                yield return null;
            }

            onComplete?.Invoke(true);
        }

        private T _GetItem<T>(string key, Transform parent) where T : Component {
            var item = _GetItem(key, transform);

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

            if (_molds.ContainsKey(key) == false) {
                Debug.LogErrorFormat("[GOPool] GetItem : Not on mold list - {0}", key);
                return null;
            }

            if (_pools.ContainsKey(key) == false) {
                Debug.LogWarningFormat("[GOPool] GetItem : Not on pool list - {0}", key);
                _pools.Add(key, new List<GameObject>());
            }

            GameObject item = null;
            List<GameObject> pool = _pools[key];

            for (int i = 0; i < pool.Count; i++) {
                if (IsAvailable(pool[i])) {
                    item = pool[i];
                    break;
                }
            }

            if (item == null) {
                item = CreateItem(key);
                pool.Add(item);
            }

            if (parent == null) {
                item.transform.SetParent(transform);
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

        private GameObject CreateItem(string key) { 
            if (_molds.ContainsKey(key) == false) {
                Debug.LogErrorFormat("[GOPool] CreateItem : Not on mold list - {0}", key);
                return null;
            }

            var item = Instantiate(_molds[key].Mold) as GameObject;
            item.name = item.name.Replace("(Clone)", "");
            var pi = item.GetComponent(typeof(IGOPoolItem)) as IGOPoolItem;
            if (pi != null) pi.OnInstantiate();

            return item;
        }

        private Coroutine _Abandon(GameObject item, float delay, System.Action<bool> onComplete) {
            return StartCoroutine(CoAbandon(item, delay, onComplete));
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

            item.SetActive(false);
            _trashs.Enqueue(item);

            var pi = item.GetComponent(typeof(IGOPoolItem)) as IGOPoolItem;
            if (pi != null) pi.OnAbandon();

            if (_isRecycleWorking) return;

            StartCoroutine(CoRecycle(item));
        }

        private IEnumerator CoRecycle(GameObject item) {
            Debug.Assert(_isRecycleWorking == false, "[GOPool] CoRecycle : Recycler already working.");

            _isRecycleWorking = true;

            yield return new WaitForEndOfFrame();

            while (_trashs.Count > 0) {
                GameObject trash = _trashs.Dequeue();
                trash.transform.SetParent(transform);
            }

            _isRecycleWorking = false;
        }

        private void _Clear(bool isForce) {
            var clearedKeys = new List<string>();

            foreach (string key in _molds.Keys) {
                var data = _molds[key];

                if ((isForce == false) && data.IsDontReleaseOnClear) continue;

                var pool = _pools[key];

                while (pool.Count > 0) {
                    Destroy(pool[0]);
                    pool.RemoveAt(0);
                }

                _pools.Remove(key);
                Addressables.Release(data.Mold);
                clearedKeys.Add(key);
            }

            foreach (string key in clearedKeys) {
                _molds.Remove(key);
            }

            var clearedLabels = new List<string>();

            foreach (string label in _labelHandles.Keys) {
                var data = _labelHandles[label];

                if ((isForce == false) && data.IsDontReleaseOnClear) continue;

                Addressables.Release(data.Handle);
                clearedLabels.Add(label);
            }

            foreach (string key in clearedKeys) {
                _labelHandles.Remove(key);
            }
        }
    }
}