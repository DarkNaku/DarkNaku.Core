using System.Collections;

namespace DarkNaku.Core {
    public interface IViewTransition {
        IEnumerator CoTransitionIn(ViewHandler handler);
        IEnumerator CoTransitionOut(ViewHandler handler);
    }
}