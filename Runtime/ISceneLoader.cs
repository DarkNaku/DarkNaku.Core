namespace DarkNaku.Core {
    public interface ISceneLoader : ISceneTransition {
        bool IsVisible { get; set; }
        void OnProgress(float progress);
    }
}