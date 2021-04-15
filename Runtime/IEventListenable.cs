public interface IEventListenable<T> {
    IEventRemover<T> AddListener(IEventListener<T> listener);
}