using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DarkNaku.Core {
    public sealed class Sound : SingletonBehaviour<Sound> {
        private struct LabelHandleData {
            public AsyncOperationHandle Handle { get; private set; }
            public bool IsDontReleaseOnClear { get; private set; }
            public LabelHandleData(AsyncOperationHandle handle, bool isDontReleaseOnClear) {
                Handle = handle;
                IsDontReleaseOnClear = isDontReleaseOnClear;
            }
        }

        private struct ClipData {
            public AudioClip Clip { get; private set; }
            public bool IsDontReleaseOnClear { get; private set; }
            public ClipData (AudioClip clip, bool isDontReleaseOnClear) {
                Clip = clip;
                IsDontReleaseOnClear = isDontReleaseOnClear;
            }
        }

        public static bool EnabledSFX {
            get { return PlayerPrefs.GetInt("SFX_SOUND_ENABLED", 1) == 1; }
            set { PlayerPrefs.SetInt("SFX_SOUND_ENABLED", value ? 1 : 0); }
        }

        public static bool EnabledBGM {
            get { return PlayerPrefs.GetInt("BGM_SOUND_ENABLED", 1) == 1; }
            set { PlayerPrefs.SetInt("BGM_SOUND_ENABLED", value ? 1 : 0); }
        }

        public static float VolumeSFX {
            get { return PlayerPrefs.GetFloat("SFX_SOUND_VOLUME", 1f); }
            set { PlayerPrefs.SetFloat("SFX_SOUND_VOLUME", Mathf.Clamp01(value)); }
        }

        public static float VolumeBGM {
            get { return PlayerPrefs.GetFloat("BGM_SOUND_VOLUME", 1f); }
            set { PlayerPrefs.SetFloat("BGM_SOUND_VOLUME", Mathf.Clamp01(value)); }
        }

        private Dictionary<string, LabelHandleData> _labelHandles = new Dictionary<string, LabelHandleData>();
        private Dictionary<int, AudioSource> _playersForBGM = new Dictionary<int, AudioSource>();
        private Dictionary<string, ClipData> _clips = new Dictionary<string, ClipData>();
        private List<string> _playedClipNamesInThisFrame = new List<string>();
        private List<AudioSource> _playersForSFX = new List<AudioSource>();
        private bool _isRunningDefender = false;

        public static Coroutine RegisterSound(string label, System.Action<float> onProgress, System.Action onComplete, bool isDontReleaseOnClear = false) {
            return Instance._RegisterSound(label, onProgress, onComplete, isDontReleaseOnClear);
        }

        public static Coroutine RegisterSounds(IList<string> labels, System.Action<float> onProgress, System.Action onComplete, bool isDontReleaseOnClear = false) {
            return Instance._RegisterSounds(labels, onProgress, onComplete, isDontReleaseOnClear);
        }

        public static void PlaySFX(string clipName) {
            Instance._PlaySFX(clipName);
        }

        public static void PlayBGM(string clipName, int channal = 0) {
            Instance._PlayBGM(clipName, channal);
        }

        public static void StopSFX(string clipName) {
            Instance._StopSFX(clipName);
        }

        public static void StopBGM(int channal) {
            Instance._StopBGM(channal);
        }

        public static void Clear(bool isForce) {
            Instance._Clear(isForce);
        }

        private void Awake() {
            DontDestroyOnLoad(gameObject);
        }

        protected override void OnInstantiate() {
            DontDestroyOnLoad(gameObject);
        }

        private Coroutine _RegisterSound(string label, System.Action<float> onProgress, System.Action onComplete, bool isDontReleaseOnClear) {
            return StartCoroutine(CoRegisterSounds(new List<string>(){ label }, onProgress, onComplete, isDontReleaseOnClear));
        }

        private Coroutine _RegisterSounds(IList<string> labels, System.Action<float> onProgress, System.Action onComplete, bool isDontReleaseOnClear) {
            return StartCoroutine(CoRegisterSounds(labels, onProgress, onComplete, isDontReleaseOnClear));
        }

        private IEnumerator CoRegisterSounds(IList<string> labels, System.Action<float> onProgress, System.Action onComplete, bool isDontReleaseOnClear) {
            if (labels == null) {
                Debug.LogError("[Sound] CoRegisterSounds : List is null.");
                onComplete();
                yield break;
            }

#if UNITY_EDITOR
            if (ConfirmLabels(labels) == false) {
                Debug.LogError("[Sound] CoRegisterSounds : Label list is null.");
                onComplete();
                yield break;
            }
#endif

            var percentPerLabel = 1f / labels.Count;

            for (int i = 0; i < labels.Count; i++) {
                if (_labelHandles.ContainsKey(labels[i])) {
                    Debug.AssertFormat(labels != null, "[Sound] CoRegisterSounds : Label already registed - {0}", labels[i]);
                    continue;
                }

                var handle = Addressables.LoadAssetsAsync<AudioClip>(labels[i], (clip) => {
                    if (_clips.ContainsKey(clip.name)) {
                        Debug.LogWarningFormat("[Sound] CoRegisterSounds : AudioClip already registed ({0} - {1})", labels[i], clip.name);
                    } else {
                        _clips.Add(clip.name, new ClipData(clip, isDontReleaseOnClear));
                    }
                }, true);

                if (handle.IsValid() == false) {
                    Debug.LogFormat("[Sound] CoRegisterSounds : Label load fail - {0}", labels[i]);
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
            if (labels == null) {
                Debug.LogError("[Sound] ConfirmLabels : Label list is null.");
                return false;
            }

            var comfirmLabels = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.GetLabels();

            for (int i = 0; i < labels.Count; i++) {
                if (comfirmLabels.Contains(labels[i]) == false) {
                    return false;
                }
            }

            return true;
        }
#endif

        private void _PlaySFX(string clipName) {
            if (EnabledSFX == false) return;

            /*
            if (_clips.ContainsKey(clipName) == false) {
                Debug.LogErrorFormat("[Sound] PlaySFX : Not on clip table - {0}", clipName);
                return;
            }
            */

            if (_playedClipNamesInThisFrame.Contains(clipName)) {
                Debug.LogWarningFormat("[Sound] PlaySFX : Defend to play the same frame - {0}", clipName);
                return;
            }

            var player = GetPlayer();
            player.volume = VolumeSFX;
            player.clip = GetClip(clipName);
            player.name = string.Format("SFX Player - {0}", clipName);
            player.loop = false;
            player.Play();

            _playedClipNamesInThisFrame.Add(clipName);
            StartCoroutine(CoDefendPlayAtTheSameTime());
        }

        private void _PlayBGM(string clipName, int channal) {
            if (EnabledBGM == false) return;

            /*
            if (_clips.ContainsKey(clipName) == false) {
                Debug.LogErrorFormat("[Sound] PlayBGM : Not on clip table - {0}", clipName);
                return;
            }
            */

            if (_playedClipNamesInThisFrame.Contains(clipName)) {
                Debug.LogWarningFormat("[Sound] PlayBGM : Defend to play the same frame - {0}", clipName);
                return;
            }

            AudioSource player = null;
            
            if (_playersForBGM.ContainsKey(channal)) {
                player = _playersForBGM[channal];
            } else {
                player = CreatePlayer();
                _playersForBGM.Add(channal, player);
            }

            player.volume = VolumeBGM;
            player.clip = GetClip(clipName);
            player.name = string.Format("BGM {0} Player - {1}", channal, clipName);
            player.loop = true;
            player.Play();

            _playedClipNamesInThisFrame.Add(clipName);
            StartCoroutine(CoDefendPlayAtTheSameTime());
        }

        private AudioClip GetClip(string clipName) {
            if (_clips.ContainsKey(clipName)) {
                return _clips[clipName].Clip;
            } else {
                AudioClip clip = Resources.Load(clipName, typeof(AudioClip)) as AudioClip;

                if (clip == null) {
                    return null;
                } else {
                    _clips.Add(clipName, new ClipData(clip, false));
                    return clip;
                }
            }
        }

        private AudioSource GetPlayer() {
            Debug.Assert(_playersForSFX != null, "[Sound] GetPlayer : SFX Player list is null.");
            
            for (int i = 0; i < _playersForSFX.Count; i++) {
                if (_playersForSFX[i].isPlaying) continue;
                return _playersForSFX[i];
            }

            var player = CreatePlayer();
            _playersForSFX.Add(player);

            return player;
        }

        private AudioSource CreatePlayer() {
            var player = new GameObject("Sound Player").AddComponent<AudioSource>();
            player.transform.parent = transform;
            player.playOnAwake = false;
            DontDestroyOnLoad(player.gameObject);

            return player;
        }

        private IEnumerator CoDefendPlayAtTheSameTime() {
            if (_isRunningDefender) yield break;
            _isRunningDefender = true;
            yield return new WaitForEndOfFrame();
            _playedClipNamesInThisFrame.Clear();
            _isRunningDefender = false;
        }

        private void _StopSFX(string clipName) {
            for (int i = 0; i < _playersForSFX.Count; i++) {
                if (_playersForSFX[i].isPlaying == false) continue;

                if (string.Equals(_playersForSFX[i].clip.name, clipName)) {
                    _playersForSFX[i].Stop();
                }
            }
        }

        private void _StopBGM(int channal) {
            if (_playersForBGM.ContainsKey(channal) == false) {
                Debug.LogWarningFormat("[Sound] StopBGM : Channal not exist - {0}", channal);
                return;
            }

            var player = _playersForBGM[channal];
            player.Stop();
        }

        private void _Clear(bool isForce) {
            for (int i = 0; i < _playersForSFX.Count; i++) {
                if (_playersForSFX[i].isPlaying) {
                    if (isForce) {
                        _playersForSFX[i].Stop();
                        _playersForSFX[i].clip = null;
                    }
                } else {
                    _playersForSFX[i].clip = null;
                }
            }

            foreach (var channal in _playersForBGM.Keys) {
                if (_playersForBGM[channal].isPlaying) {
                    if (isForce) {
                        _playersForBGM[channal].Stop();
                        _playersForBGM[channal].clip = null;
                    }
                } else {
                    _playersForBGM[channal].clip = null;
                }
            }

            var clearedKeys = new List<string>();

            foreach (string key in _clips.Keys) {
                var data = _clips[key];

                if (isForce || (data.IsDontReleaseOnClear == false)) {
                    Addressables.Release(data.Clip);
                    clearedKeys.Add(key);
                }
            }

            foreach (string key in clearedKeys) {
                _clips.Remove(key);
            }

            var clearedLabels = new List<string>();

            foreach (string label in _labelHandles.Keys) {
                var data = _labelHandles[label];

                if (isForce || (data.IsDontReleaseOnClear == false)) {
                    Addressables.Release(data.Handle);
                    clearedLabels.Add(label);
                }
            }

            foreach (string key in clearedKeys) {
                _labelHandles.Remove(key);
            }
        }
    }
}