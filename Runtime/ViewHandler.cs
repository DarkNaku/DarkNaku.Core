using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public abstract class ViewHandler : MonoBehaviour {
    [SerializeField] private Camera _viewCamera = null;
    public Camera ViewCamera { get { return _viewCamera; } }

    [SerializeField] private Canvas _viewCanvas = null;
    public Canvas ViewCanvas { get { return _viewCanvas; } }

    [SerializeField] private CanvasGroup _viewCanvasGroup = null;
    public CanvasGroup ViewCanvasGroup { get { return _viewCanvasGroup; } }

    [SerializeField] private IViewTransition _viewTransition = null;
    protected IViewTransition ViewTransition { 
        get {
            if (_viewTransition == null) {
                _viewTransition = GetComponent(typeof(IViewTransition)) as IViewTransition;
            }

            return _viewTransition; 
        } 
    }

    public bool IsInTransition { get; private set; }

    private System.Action<object> _onWillHide = null;
    private System.Action<object> _onDidHide = null;

    public Coroutine Show(object param, System.Action<object> onWillHide, System.Action<object> onDidHide) {
        return StartCoroutine(CoShow(param, onWillHide, onDidHide));
    }

    public Coroutine Hide(object result) {
        return StartCoroutine(CoHide(result));
    }

    protected virtual void OnWillEnter(object param) { }
    protected virtual void OnDidEnter(object param) { }
    protected virtual void OnWillLeave() { }
    protected virtual void OnDidLeave() { }
    public virtual void OnEscape() { }

    protected IEnumerator CoShow(object param, System.Action<object> onWillHide, System.Action<object> onDidHide) {
        IsInTransition = true;
        _onWillHide = onWillHide;
        _onDidHide = onDidHide;

        OnWillEnter(param);

        if (ViewTransition != null) {
            yield return StartCoroutine(ViewTransition.CoTransitionIn(this));
        }

        OnDidEnter(param);

        ViewCanvasGroup.interactable = true;
        IsInTransition = false;
    }

    protected IEnumerator CoHide(object result) {
        IsInTransition  = true;
        ViewCanvasGroup.interactable = false;

        _onWillHide?.Invoke(result);
        _onWillHide = null;

        OnWillLeave();

        if (ViewTransition != null) {
            yield return StartCoroutine(ViewTransition.CoTransitionOut(this));
        }

        OnDidLeave();

        _onDidHide?.Invoke(result);
        _onDidHide = null;
        IsInTransition = false;
    }
}