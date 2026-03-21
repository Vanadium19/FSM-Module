using UnityEngine;

namespace FSMModule.Graph
{
    public abstract class FsmStateBehaviour : ScriptableObject
    {
        protected FsmContext Context { get; private set; }
        protected FsmGraphAsset Graph { get; private set; }
        protected Blackboard Blackboard => Context.Blackboard;
        protected GameObject Owner => Context.Owner;
        protected Transform OwnerTransform => Context.Transform;

        internal void Initialize(FsmContext context, FsmGraphAsset graph)
        {
            Context = context;
            Graph = graph;
            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        public virtual void OnEnter() { }

        public virtual void OnUpdate(float deltaTime) { }

        public virtual void OnExit() { }
    }
}
