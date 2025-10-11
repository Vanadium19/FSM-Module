using System;
using System.Collections.Generic;

namespace FSMModule
{
    public interface IStateMachine<TKey> : IState
    {
        public event Action<TKey> OnStateAdded;
        public event Action<TKey> OnStateChanged;
        public event Action<TKey> OnStateRemoved;

        public int StateCount { get; }
        public TKey CurrentState { get; }
        public IReadOnlyCollection<TKey> States { get; }

        public bool AddState(TKey key, IState state);
        public bool RemoveState(TKey key);
        public bool ContainsState(TKey key);

        public bool TryChangeState(TKey key);
        public void ChangeState(TKey key);
    }
}