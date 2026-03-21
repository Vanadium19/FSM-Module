using UnityEngine;

namespace FSMModule.Graph
{
    public abstract class FsmStateBehaviour : ScriptableObject
    {
        protected FsmContext Context { get; private set; }
        protected FsmGraphAsset Graph { get; private set; }
        protected Blackboard Blackboard => Context.Blackboard;
        protected FsmParameters Parameters => Context.Parameters;
        protected GameObject Owner => Context.Owner;
        protected Transform OwnerTransform => Context.Transform;

        protected void SetBool(string key, bool value) => Parameters.SetBool(key, value);
        protected void SetInt(string key, int value) => Parameters.SetInt(key, value);
        protected void SetFloat(string key, float value) => Parameters.SetFloat(key, value);
        protected bool GetBool(string key) => Parameters.GetBool(key);
        protected int GetInt(string key) => Parameters.GetInt(key);
        protected float GetFloat(string key) => Parameters.GetFloat(key);

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
