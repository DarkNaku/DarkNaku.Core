using System.Collections;
using UnityEngine;

namespace DarkNaku.Core {
    public abstract class SceneHandler : MonoBehaviour {
        public IEnumerator CoInitialize(object param) {
            yield return CoOnInitialize(param);
        }

        public IEnumerator CoUninitialize() {
            yield return CoOnUninitialize();
        }

        protected virtual IEnumerator CoOnInitialize(object param) {
            yield break;
        }

        protected virtual IEnumerator CoOnUninitialize() {
            yield break;
        }

        public virtual IEnumerator CoInAnimation(string prevScene) {
            yield break;
        }

        public virtual IEnumerator CoOutAnimation(string nextScene) {
            yield break;
        }

        public virtual void OnProgress(float progress) {
        }
    }
}