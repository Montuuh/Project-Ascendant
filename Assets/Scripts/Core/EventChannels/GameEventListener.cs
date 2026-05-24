using UnityEngine;
using UnityEngine.Events;

namespace ProjectAscendant.Core
{
    // Per §9.4.1.1 — MonoBehaviour bridge between a GameEventSO<T> channel and a UnityEvent<T>.
    // Subclass for each event type to make it Inspector-addable as a component.
    // Note: UnityEvent<T> Inspector wiring requires T to be a Unity-serializable type.
    //       Custom struct payloads work at runtime; Inspector dropdown not supported.
    public abstract class GameEventListener<TChannel, TPayload> : MonoBehaviour
        where TChannel : GameEventSO<TPayload>
    {
        [SerializeField] protected TChannel _channel;
        [SerializeField] protected UnityEvent<TPayload> _response;

        protected virtual void OnEnable()
        {
            if (_channel != null)
                _channel.Register(HandleEvent);
        }

        protected virtual void OnDisable()
        {
            if (_channel != null)
                _channel.Unregister(HandleEvent);
        }

        private void HandleEvent(TPayload payload) => _response?.Invoke(payload);
    }
}
