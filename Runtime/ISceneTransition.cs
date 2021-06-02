using System.Collections;

namespace DarkNaku.Core {
    public interface ISceneTransition {
        IEnumerator CoInAnimation(string prevScene);
        IEnumerator CoOutAnimation(string nextScene);
    }
}