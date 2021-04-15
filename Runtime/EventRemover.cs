using System.Collections.Generic;
using UnityEngine;

public class EventRemover<T> : IEventRemover<T> {
    private List<IEventListener<T>> _listeners = null;
    private IEventListener<T> _listener = null;

    public EventRemover(List<IEventListener<T>> listeners, IEventListener<T> listener) {
        _listeners = listeners;
        _listener = listener;
    }

    public void RemoveListener() {
        if ((_listeners == null) || (_listener == null)) return;

        if (_listeners.Contains(_listener)) {
            _listeners.Remove(_listener);
            _listener = null;
            _listeners = null;
        } else {
            Debug.LogWarning("[EventRemover] RemoveListener : Listener is not registed.");
        }
    }
}