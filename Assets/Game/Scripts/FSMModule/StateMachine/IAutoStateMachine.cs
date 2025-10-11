using System;
using System.Collections.Generic;

namespace FSMModule
{
    public interface IAutoStateMachine<TKey> : IStateMachine<TKey>
    {
        public event Action<IStateTransition<TKey>> OnTransitionAdded;
        public event Action<IStateTransition<TKey>> OnTransitionRemoved;

        public int TransitionCount { get; }
        public IEnumerable<(TKey, TKey)> Transitions { get; }

        public bool AddTransition(IStateTransition<TKey> transition);
        public bool AddTransition(TKey from, TKey to, Func<bool> condition);

        public bool RemoveTransition(IStateTransition<TKey> transition);
        public bool RemoveTransition(TKey from, TKey to);

        public bool ContainsTransition(IStateTransition<TKey> transition);
        public bool ContainsTransition(TKey from, TKey to);
    }
}