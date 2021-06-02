namespace DarkNaku.Core {
    public interface ISceneLoader : ISceneTransition {
        void OnProgress(float progress);
    }
}