using System.Collections.Generic;
using UnityEngine;

namespace FSMModule.Graph
{
    [AddComponentMenu("FSM/FSM Graph Runner")]
    public sealed class FsmGraphRunner : FsmRunner
    {
        [SerializeField] private FsmGraphAsset graph;
        [SerializeField] private List<FsmInjectedFieldBinding> injectedBindings = new();

        private FsmGraphRuntime _runtime;

        public FsmGraphAsset Graph => graph;
        public string CurrentStateId => _runtime != null ? _runtime.CurrentStateId : string.Empty;
        public IReadOnlyList<FsmInjectedFieldBinding> InjectedBindings => injectedBindings;
        public void SetBool(string key, bool value) => Context?.Parameters.SetBool(key, value);
        public void SetInt(string key, int value) => Context?.Parameters.SetInt(key, value);
        public void SetFloat(string key, float value) => Context?.Parameters.SetFloat(key, value);
        public bool GetBool(string key) => Context != null && Context.Parameters.GetBool(key);
        public int GetInt(string key) => Context != null ? Context.Parameters.GetInt(key) : default;
        public float GetFloat(string key) => Context != null ? Context.Parameters.GetFloat(key) : default;

        protected override IState CreateState(FsmContext context)
        {
            if (graph == null)
            {
                Debug.LogWarning($"'{nameof(FsmGraphRunner)}' on '{name}' has no graph assigned.", this);
                return new BaseState();
            }

            _runtime = graph.CreateRuntime(context, injectedBindings, this);
            return _runtime;
        }
    }
}
