using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DarkNaku.Core;

public class Director : SingletonBehaviour<Director> {
    private const string LOADING_SCENE = "Loading";

    [SerializeField] private Canvas _loadingCanvas = null;
    [SerializeField] private Image _loadingBar = null;
    [SerializeField] private string _sceneName = null;
    [SerializeField] private string _viewName = null;

    public static bool NowLoading => Instance._nowLoading;

    public static void Load(string sceneName, object param = null) {
        TaskRunner.Run(Instance.CoLoad(sceneName,  param));
    }

    private bool _nowLoading = false;

    protected override void OnInstantiate() {
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator CoLoad(string sceneName, object param) {
        if (_nowLoading) yield break;

        _nowLoading = true;

        AsyncOperation ao = null;

        var currentScene = SceneManager.GetActiveScene();
		var currentSceneHandler = FindHandler<SceneHandler>(currentScene);
        var eventSystem = GetEventSystemInScene(currentScene);

        if (eventSystem != null) eventSystem.enabled = false;

        LoadingHandler loadingHandler = null;
        var loadingScene = SceneManager.GetSceneByName(LOADING_SCENE);

        if (loadingScene != null) {
            ao = SceneManager.LoadSceneAsync(LOADING_SCENE);
            ao.allowSceneActivation = false;
        }

        yield return currentSceneHandler?.CoOutAnimation();
        yield return currentSceneHandler?.CoUninitialize();

        if (loadingScene != null) {
            while (ao.progress < 0.9F) yield return null;
            ao.allowSceneActivation = true;

            loadingScene = SceneManager.GetSceneByName(LOADING_SCENE);

            while (loadingScene.isLoaded == false) yield return null;

		    loadingHandler = FindHandler<LoadingHandler>(loadingScene);
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
