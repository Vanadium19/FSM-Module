using System;
using UnityEngine;

namespace FSMModule
{
    public abstract class FsmRunner : MonoBehaviour
    {
        protected FsmContext Context { get; private set; }
        protected IState State { get; private set; }

        private bool _isDisposed;

        public FsmContext RuntimeContext => Context;
        public IState RuntimeState => State;
        public bool IsInitialized => Context != null && State != null;

        protected virtual void Awake()
        {
            _isDisposed = false;
            Context = CreateContext();
            State = CreateState(Context);
        }

        protected virtual void OnEnable() => State?.OnEnter();

        protected virtual void Update() => State?.OnUpdate(Time.deltaTime);

        protected virtual void OnDisable() => State?.OnExit();

        protected virtual void OnDestroy()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (State is IDisposable disposableState)
                disposableState.Dispose();
        }

        protected virtual FsmContext CreateContext() => new(gameObject, transform);

        protected abstract IState CreateState(FsmContext context);
    }
}
