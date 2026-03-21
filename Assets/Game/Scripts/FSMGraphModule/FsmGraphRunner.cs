using UnityEngine;

namespace FSMModule.Graph
{
    [AddComponentMenu("FSM/FSM Graph Runner")]
    public sealed class FsmGraphRunner : FsmRunner
    {
        [SerializeField] private FsmGraphAsset graph;

        private FsmGraphRuntime _runtime;

        public FsmGraphAsset Graph => graph;
        public string CurrentStateId => _runtime != null ? _runtime.CurrentStateId : string.Empty;

        protected override IState CreateState(FsmContext context)
        {
            if (graph == null)
            {
                Debug.LogWarning($"'{nameof(FsmGraphRunner)}' on '{name}' has no graph assigned.", this);
                return new BaseState();
            }

            _runtime = graph.CreateRuntime(context);
            return _runtime;
        }
    }
}
