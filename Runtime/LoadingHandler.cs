using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LoadingHandler : MonoBehaviour {
    public virtual IEnumerator CoInAnimation() {
        yield break;
    }

    public virtual IEnumerator CoOutAnimation() {
        yield break;
    }

    public virtual void OnProgress(float progress) {
    }
}
