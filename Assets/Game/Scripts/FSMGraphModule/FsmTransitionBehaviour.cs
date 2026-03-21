using UnityEngine;

namespace FSMModule.Graph
{
    public abstract class FsmTransitionBehaviour : ScriptableObject
    {
        protected FsmContext Context { get; private set; }
        protected Blackboard Blackboard => Context.Blackboard;
        protected GameObject Owner => Context.Owner;
        protected Transform OwnerTransform => Context.Transform;

        internal void Initialize(FsmContext context)
        {
            Context = context;
            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        public abstract bool CanTransition();
    }
}
