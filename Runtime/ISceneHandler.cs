using System;
using System.Collections;

namespace DarkNaku.Core {
    public interface ISceneHandler {
        IEnumerator CoInitialize(object param, Action<float> onProgress);
        IEnumerator CoUninitialize();
        void OnEnter(object param);
        void OnLeave();
    }
}