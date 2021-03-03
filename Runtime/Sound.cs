using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DarkNaku.Core {
    public sealed class Sound : SingletonScriptable<Sound> {
        [Header("AudioClip Path (Path under Resources)")]
        [SerializeField] string clipPath = "Sounds";

        public static bool EnabledSFX {
            get { return PlayerPrefs.GetInt("SFX_SOUND_ENABLED", 1) == 1; }
            set { 
                PlayerPrefs.SetInt("SFX_SOUND_ENABLED", value ? 1 : 0);
                if (value == false) StopSFX();
            }
        }

        public static bool EnabledBGM {
            get { return PlayerPrefs.GetInt("BGM_SOUND_ENABLED", 1) == 1; }
            set { 
                PlayerPrefs.SetInt("BGM_SOUND_ENABLED", value ? 1 : 0); 
                if (value == false) StopBGM();
            }
        }

        public static float VolumeSFX {
            get { return PlayerPrefs.GetFloat("SFX_SOUND_VOLUME", 1f); }
            set {
                var v = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat("SFX_SOUND_VOLUME", v);
                Instance._ChangeVolumeSFX(v);
            }
        }

        public static float VolumeBGM {
            get { return PlayerPrefs.GetFloat("BGM_SOUND_VOLUME", 1f); }
            set { 
                var v = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat("BGM_SOUND_VOLUME", v);
                Instance._ChangeVolumeBGM(v);
            }
        }

        private Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private List<string> _playedClipNamesInThisFrame = new List<string>();
        private List<AudioSource> _playersForBGM = new List<AudioSource>();
        private List<AudioSource> _playersForSFX = new List<AudioSource>();
        private bool _isRunningDefender = false;
        private Transform _root = null;

#if UNITY_EDITOR
        [MenuItem("DarkNaku/Sound Setting")]
        public static void Edit() {
            Selection.activeObject = Instance;
        }
#endif

        public static void PlaySFX(string clipName) {
            Instance._PlaySFX(clipName);
        }

        public static int PlayBGM(string clipName) {
            return Instance._PlayBGM(clipName);
        }

        public static void StopSFX() {
            Instance._StopSFX();
        }

        public static void StopSFX(string clipName) {
            Instance._StopSFX(clipName);
        }

        public static void StopBGM() {
            Instance._StopBGM();
        }

        public static void StopBGM(int channal) {
            Instance._StopBGM(channal);
        }

        public static void StopBGM(string clipName) {
            Instance._StopBGM(clipName);
        }

        protected override void OnLoad() {
            _root = new GameObject("Sound Players").transform;
            DontDestroyOnLoad(_root.gameObject);
            TaskRunner.Run(CoDefendPlayAtTheSameTime());
        }

        private IEnumerator CoDefendPlayAtTheSameTime() {
            if (_isRunningDefender) yield break;
            _isRunningDefender = true;
            yield return new WaitForEndOfFrame();
            _playedClipNamesInThisFrame.Clear();
            _isRunningDefender = false;
        }

        private void _PlaySFX(string clipName) {
            if (EnabledSFX == false) return;
            if (_playedClipNamesInThisFrame.Contains(clipName)) return;

            if (_clips.ContainsKey(clipName) == false) {
                Debug.LogErrorFormat("[Sound] PlaySFX : Could't found clip - {0}", clipName);
                return;
            }

            var player = GetPlayer(ref _playersForSFX);
            player.volume = VolumeSFX;
            player.clip = GetClip(clipName);
            player.name = string.Format("SFX Player - {0}", clipName);
            player.loop = false;
            player.Play();

            _playedClipNamesInThisFrame.Add(clipName);
        }

        private int _PlayBGM(string clipName) {
            if (EnabledBGM == false) return -1;
            if (_playedClipNamesInThisFrame.Contains(clipName)) return -1;

            if (_clips.ContainsKey(clipName) == false) {
                Debug.LogErrorFormat("[Sound] PlayBGM : Could't found clip - {0}", clipName);
                return -1;
            }

            var player = GetPlayer(ref _playersForBGM);
            player.volume = VolumeBGM;
            player.clip = GetClip(clipName);
            player.name = string.Format("BGM {0} Player - {1}", _playersForBGM.IndexOf(player), clipName);
            player.loop = true;
            player.Play();

            _playedClipNamesInThisFrame.Add(clipName);

            return _playersForBGM.IndexOf(player);
        }

        private AudioSource GetPlayer(ref List<AudioSource> players) {
            Debug.Assert(players != null, "[Sound] GetPlayer : Player list is null.");

            if (players == null) return null;
            
            for (int i = 0; i < players.Count; i++) {
                if (players[i].isPlaying) continue;
                return players[i];
            }

            var player = CreatePlayer();
            players.Add(player);

            return player;
        }

        private AudioSource CreatePlayer() {
            var player = new GameObject().AddComponent<AudioSource>();
            player.transform.parent = _root;
            player.playOnAwake = false;
            return player;
        }

        private AudioClip GetClip(string clipName) {
            if (_clips.ContainsKey(clipName)) {
                return _clips[clipName];
            } else {
                AudioClip clip = Resources.Load(clipPath + "/" + clipName, typeof(AudioClip)) as AudioClip;

                if (clip == null) {
                    return null;
                } else {
                    _clips.Add(clipName, clip);
                    return clip;
                }
            }
        }

        private void _StopSFX() {
            for (int i = 0; i < _playersForSFX.Count; i++) {
                _playersForSFX[i].Stop();
            }
        }

        private void _StopSFX(string clipName) {
            StopPlayer(ref _playersForSFX, clipName);
        }

        private void _StopBGM() {
            for (int i = 0; i < _playersForBGM.Count; i++) {
                _playersForBGM[i].Stop();
            }
        }

        private void _StopBGM(string clipName) {
            StopPlayer(ref _playersForBGM, clipName);
        }

        private void _StopBGM(int channal) {
            if ((channal >= 0) && (channal < _playersForBGM.Count)) {
                _playersForBGM[channal].Stop();
            }
        }

        private void StopPlayer(ref List<AudioSource> players, string clipName) {
            for (int i = 0; i < players.Count; i++) {
                if (players[i].isPlaying == false) continue;

                if (string.Equals(players[i].clip.name, clipName)) {
                    players[i].Stop();
                }
            }
        }

        private void _ChangeVolumeSFX(float volume) {
            ChangeVolume(ref _playersForSFX, volume);
        }

        private void _ChangeVolumeBGM(float volume) {
            ChangeVolume(ref _playersForBGM, volume);
        }

        private void ChangeVolume(ref List<AudioSource> players, float volume) {
            for (int i = 0; i < players.Count; i++) {
                players[i].volume = Mathf.Clamp01(volume);
            }
        }
    }
}