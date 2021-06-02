using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DarkNaku.Core {
    public class Director : SingletonScriptable<Director> {
        [Header("Scene's name for loading.")]
        [SerializeField] private GameObject _loadingPrefab = null;

        public static bool NowLoading => Instance._nowLoading;

        [NonSerialized] private bool _nowLoading = false;
        [NonSerialized] private ISceneLoader _loader = null;

#if UNITY_EDITOR
        [MenuItem("DarkNaku/Director Settings")]
        public static void SelectDirector() {
            Selection.activeObject = Instance;
        }
#endif

        public static void Change(string sceneName, object param = null) {
            TaskRunner.Run(Instance.CoChange(sceneName, param));
        }

        protected override void OnLoaded() {
            _nowLoading = false;

            if (_loadingPrefab != null) {
                var loader = Instantiate(_loadingPrefab);

                _loader = loader.GetComponent(typeof(ISceneLoader)) as ISceneLoader;

                if (_loader != null) {
                    _loader.IsVisible = false;
                    DontDestroyOnLoad(loader);
                }
            }
        }

        private IEnumerator CoChange(string nextSceneName, object param) {
            if (_nowLoading) yield break;

            _nowLoading = true;

            var prevScene = SceneManager.GetActiveScene();
            var eventSystem = GetEventSystemInScene(prevScene);
            var prevSceneHandler = FindHandler<ISceneHandler>(prevScene);
            var prevSceneTransition = FindHandler<ISceneTransition>(prevScene);
            var prevSceneName = (prevScene == null) ? null : prevScene.name;

            if (eventSystem != null) eventSystem.enabled = false;

            if (prevSceneTransition != null) {
                yield return prevSceneTransition.CoOutAnimation(nextSceneName);
            }

            if (_loader != null) {
                _loader.IsVisible = true;
                yield return _loader.CoInAnimation(prevSceneName);
            }

            if (prevSceneHandler != null) {
                yield return prevSceneHandler.CoUninitialize();
                prevSceneHandler.OnLeave();
            }

            var ao = SceneManager.LoadSceneAsync(nextSceneName);

            while (ao.isDone == false) {
                _loader?.OnProgress(ao.progress * 0.5f);
                yield return null;
            }

            _loader?.OnProgress(0.5f);

            var nextScene = SceneManager.GetSceneByName(nextSceneName);
            var nextSceneHandler = FindHandler<ISceneHandler>(nextScene);
            var nextSceneTransition = FindHandler<ISceneTransition>(nextScene);

            if (nextSceneHandler != null) {
                yield return nextSceneHandler.CoInitialize(param, (progress) => {
                    _loader?.OnProgress(0.5f + (progress * 0.49f));
                });
            }

            if (_loader != null) {
                _loader.OnProgress(1f);
                yield return _loader.CoOutAnimation(nextSceneName);
                _loader.IsVisible = false;
            }

            nextSceneHandler?.OnEnter(param);

            if (nextSceneTransition != null) {
                yield return nextSceneTransition.CoInAnimation(prevSceneName);
            }

            eventSystem = GetEventSystemInScene(nextScene);

            if (eventSystem != null) eventSystem.enabled = true;

            _nowLoading = false;
        }

        private EventSystem GetEventSystemInScene(Scene scene) {
            EventSystem[] ess = EventSystem.FindObjectsOfType<EventSystem>();

            for (int i = 0; i < ess.Length; i++) {
                if (ess[i].gameObject.scene == scene) return ess[i];
            }

            return null;
        }

        private T FindHandler<T>(Scene scene) where T : class {
            GameObject[] goes = scene.GetRootGameObjects();

            for (int i = 0; i < goes.Length; i++) {
                T handler = goes[i].GetComponent<T>();
                if (handler != null) return handler;
            }

            return null;
        }
    }
}
