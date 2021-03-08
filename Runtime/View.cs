using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;


namespace DarkNaku.Core {
    public sealed class View : SingletonBehaviour<View> {
        public ViewHandler CurrentPopup { 
            get { return (_popups.Count > 0) ? _popups.Peek() : null; }
        }

        public ViewHandler MainView { get; private set; }

        private static bool _initialized = false;

        private Dictionary<string, ViewHandler> _viewTable = null;
        private Stack<ViewHandler> _popups = null;
        private System.DateTime _escapePressedTime = default;

        public static Coroutine Change(string viewName, bool atTheSameTime = false) {
            return Instance._Change(viewName, atTheSameTime);
        }

        public static Coroutine Change(string viewName, object param, bool atTheSameTime = false) {
            return Instance._Change(viewName, param, atTheSameTime);
        }

        public static Coroutine ShowPopup(string viewName) {
            return Instance._ShowPopup(viewName);
        }

        public static Coroutine ShowPopup(string viewName, object param) {
            return Instance._ShowPopup(viewName, param);
        }

        public static Coroutine ShowPopup(string viewName, object param, System.Action<object> onHide) {
            return Instance._ShowPopup(viewName, param, onHide);
        }

        public static Coroutine ShowPopup(string viewName, object param, System.Action<object> onWillHide, System.Action<object> onDidHide) {
            return Instance._ShowPopup(viewName, param, onWillHide, onDidHide);
        }

        public static Coroutine HidePopup(ViewHandler popup) {
            return Instance._HidePopup(popup);
        }

        public static Coroutine HidePopup(ViewHandler popup, object result) {
            return Instance._HidePopup(popup, result);
        }

        private void Awake() {
            if (_initialized == false) Initialize();
        }

        private void Update() {
            if (Input.GetKeyUp(KeyCode.Escape)) Escape();
        }

        protected override void OnInstantiate() {
            if (_initialized == false) Initialize();
        }

        protected override void OnBeforeDestroy() {
            Clear();
        }

        private void Initialize() {
            if (_initialized) {
                Debug.LogWarning("[View] Initialize : Already initialized.");
                return;
            }

            _popups = new Stack<ViewHandler>();
            _viewTable = new Dictionary<string, ViewHandler>();
            _escapePressedTime = System.DateTime.Now;

            for (int i = 0; i < transform.childCount; i++) {
                var handler = transform.GetChild(i).GetComponent<ViewHandler>();

                if (handler != null) {
                    handler.gameObject.SetActive(false);
                    _viewTable.Add(handler.name, handler);
                }
            }

            this.name = "View";

            _initialized = true;
        }

        private void Clear() {
            _popups.Clear();
            _viewTable.Clear();
            _initialized = false;
        }

        private void Escape() {
            if (MainView == null) return;
            if (MainView.IsInTransition) return;
            if ((System.DateTime.Now - _escapePressedTime).TotalSeconds < 1) return;

            if (CurrentPopup == null) {
                MainView.OnEscape();
            } else {
                CurrentPopup.OnEscape();
            }

            _escapePressedTime = System.DateTime.Now;
        }

        private Coroutine _Change(string viewName, bool atTheSameTime) {
            return StartCoroutine(CoChange(viewName, null, atTheSameTime));
        }

        private Coroutine _Change(string viewName, object param, bool atTheSameTime) {
            return StartCoroutine(CoChange(viewName, param, atTheSameTime));
        }

        private IEnumerator CoChange(string viewName, object param, bool atTheSameTime) {
            Debug.AssertFormat(_viewTable.ContainsKey(viewName), "[View] CoChange : Not on view table - {0}", viewName);

            if (atTheSameTime) {
                var prev = MainView;
                prev.Hide(null);
                MainView = _viewTable[viewName];
                MainView.ViewCanvas.sortingOrder = 0;
                MainView.ViewCanvas.worldCamera = MainView.ViewCamera;
                MainView.gameObject.SetActive(true);
                MainView.Show(param, null, null);

                while (prev.IsInTransition || MainView.IsInTransition) {
                    if (prev.IsInTransition == false) {
                        prev.gameObject.SetActive(false);
                    }
                    yield return null;
                }
            } else {
                if (MainView != null) {
                    yield return MainView.Hide(null);
                    MainView.gameObject.SetActive(false);
                }

                MainView = _viewTable[viewName];
                MainView.ViewCanvas.sortingOrder = 0;
                MainView.ViewCanvas.worldCamera = MainView.ViewCamera;
                MainView.gameObject.SetActive(true);
                yield return MainView.Show(param, null, null);
            }
        }

        private Coroutine _ShowPopup(string viewName) {
            return StartCoroutine(CoShowPopup(viewName, null, null, null));
        }

        private Coroutine _ShowPopup(string viewName, object param) {
            return StartCoroutine(CoShowPopup(viewName, param, null, null));
        }

        private Coroutine _ShowPopup(string viewName, object param, System.Action<object> onDidHide) {
            return StartCoroutine(CoShowPopup(viewName, param, null, onDidHide));
        }

        private Coroutine _ShowPopup(string viewName, object param, System.Action<object> onWillHide, System.Action<object> onDidHide) {
            return StartCoroutine(CoShowPopup(viewName, param, onWillHide, onDidHide));
        }

        private IEnumerator CoShowPopup(string viewName, object param, System.Action<object> onWillHide, System.Action<object> onDidHide) {
            if (MainView == null) {
                Debug.LogError("[View] CoShowPopup : MainView is null.");
                yield break;
            }

            if (_viewTable.ContainsKey(viewName) == false) {
                Debug.LogErrorFormat("[View] CoShowPopup : Not on view table - {0}", viewName);
                yield break;
            }

            var popup = _viewTable[viewName];

            if (_popups.Contains(popup)) {
                Debug.LogErrorFormat("[View] CoShowPopup : View already shown. - {0}", viewName);
                yield break;
            }

            popup.ViewCameraData.renderType = CameraRenderType.Overlay;
            popup.gameObject.SetActive(true);

            MainView.ViewCameraData.cameraStack.Add(popup.ViewCamera);

            if (_popups.Count > 0) {
                var currentPopup = _popups.Peek();
                currentPopup.ViewCanvasGroup.interactable = false;
                popup.ViewCanvas.sortingOrder = currentPopup.ViewCanvas.sortingOrder + 1;
            } else {
                MainView.ViewCanvasGroup.interactable = false;
                popup.ViewCanvas.sortingOrder = MainView.ViewCanvas.sortingOrder + 1;
            }

            _popups.Push(popup);

            yield return popup.Show(param, onWillHide, onDidHide);
        }

        private Coroutine _HidePopup(ViewHandler popup) {
            return StartCoroutine(CoHidePopup(popup, null));
        }

        private Coroutine _HidePopup(ViewHandler popup, object result) {
            return StartCoroutine(CoHidePopup(popup, result));
        }

        private IEnumerator CoHidePopup(ViewHandler target, object result) {
            if (target == null) {
                Debug.LogError("[View] CoHidePopup : Target is null.");
                yield break;
            }

            if (_popups.Count <= 0) {
                Debug.LogError("[View] CoHidePopup : Popup is not exist.");
                yield break;
            }

            if (target != _popups.Peek()) {
                Debug.LogErrorFormat("[View] CoHidePopup : Target is not equal to last item of popup list. (Target is {0} and Last item is {1})", target.name, _popups.Peek().name);
                yield break;
            }

            var currentPopup = _popups.Pop();

            yield return currentPopup.Hide(result);

            MainView.ViewCameraData.cameraStack.Remove(currentPopup.ViewCamera);
            currentPopup.gameObject.SetActive(false);

            if (_popups.Count > 0) {
                _popups.Peek().ViewCanvasGroup.interactable = true;
            } else {
                MainView.ViewCanvasGroup.interactable = true;
            }
        }
    }
}