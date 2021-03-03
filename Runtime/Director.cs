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
        [SerializeField] private string _loadingSceneName = "Loading";

        public static bool NowLoading => Instance._nowLoading;

        private bool _nowLoading = false;

#if UNITY_EDITOR
        [MenuItem("DarkNaku/Director Settings")]
        public static void Edit() {
            Selection.activeObject = Instance;
        }
#endif

        public static void Load(string sceneName, object param = null) {
            TaskRunner.Run(Instance.CoLoad(sceneName, param));
        }

        private IEnumerator CoLoad(string sceneName, object param) {
            if (_nowLoading) yield break;

            _nowLoading = true;

            AsyncOperation ao = null;

            var currentScene = SceneManager.GetActiveScene();
            var currentSceneHandler = FindHandler<SceneHandler>(currentScene);
            var eventSystem = GetEventSystemInScene(currentScene);

            if (eventSystem != null) eventSystem.enabled = false;

            SceneHandler loadingHandler = null;
            var loadingScene = SceneManager.GetSceneByName(_loadingSceneName);

            if (loadingScene != null) {
                ao = SceneManager.LoadSceneAsync(_loadingSceneName);
                ao.allowSceneActivation = false;
            }

            yield return currentSceneHandler?.CoOutAnimation();
            yield return currentSceneHandler?.CoUninitialize();

            if (loadingScene != null) {
                while (ao.progress < 0.9F) yield return null;
                ao.allowSceneActivation = true;

                loadingScene = SceneManager.GetSceneByName(_loadingSceneName);

                while (loadingScene.isLoaded == false) yield return null;

                loadingHandler = FindHandler<SceneHandler>(loadingScene);
                yield return loadingHandler?.CoInAnimation();
            }

            ao = SceneManager.LoadSceneAsync(sceneName);
            ao.allowSceneActivation = false;

            while (ao.progress < 0.9F) {
                loadingHandler?.OnProgress(ao.progress);
                yield return null;
            }

            loadingHandler?.OnProgress(1f);

            yield return loadingHandler?.CoOutAnimation();

            ao.allowSceneActivation = true;

            currentScene = SceneManager.GetSceneByName(sceneName);

            while (currentScene.isLoaded == false) yield return null;

            currentSceneHandler = FindHandler<SceneHandler>(currentScene);

            yield return currentSceneHandler?.CoInitialize(param);
            yield return currentSceneHandler?.CoInAnimation();

            eventSystem = GetEventSystemInScene(currentScene);
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

        private T FindHandler<T>(Scene scene) where T : MonoBehaviour {
            GameObject[] goes = scene.GetRootGameObjects();

            for (int i = 0; i < goes.Length; i++) {
                T handler = goes[i].GetComponent<T>();
                if (handler != null) return handler;
            }

            return null;
        }
    }
}
