using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkNaku.Core;

public abstract class SceneHandler : MonoBehaviour {
    public virtual IEnumerator CoInAnimation() {
        yield break;
    }

    public virtual IEnumerator CoOutAnimation() {
        yield break;
    }

    public IEnumerator CoInitialize(object param) {
        yield return CoOnInitialize(param);
    }

    public IEnumerator CoUninitialize() {
        yield return CoOnUninitialize();
        GOPool.Clear();
    }

    protected virtual IEnumerator CoOnInitialize(object param) {
        yield break;
    }

    protected virtual IEnumerator CoOnUninitialize() {
        yield break;
    }
}
