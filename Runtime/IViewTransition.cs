using System.Collections;

public interface IViewTransition {
    IEnumerator CoTransitionIn(ViewHandler handler);
    IEnumerator CoTransitionOut(ViewHandler handler);
}