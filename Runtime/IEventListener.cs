public interface IEventListener<T> {
    T Event { get; }
    void OnComplete();
}