using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FSMModule.Graph
{
    [CreateAssetMenu(fileName = "FSMGraph", menuName = "FSM/Graph Asset")]
    public sealed class FsmGraphAsset : ScriptableObject
    {
        [SerializeField] private string initialStateId;
        [SerializeField] private List<FsmBlackboardParameterDefinition> blackboardParameters = new();
        [SerializeField] private List<FsmBindingDefinition> bindingDefinitions = new();
        [SerializeField] private List<FsmGraphStateNode> states = new();
        [SerializeField] private List<FsmGraphTransition> transitions = new();

        public string InitialStateId
        {
            get => initialStateId;
            set => initialStateId = value;
        }

        public List<FsmBlackboardParameterDefinition> BlackboardParameters => blackboardParameters;
        public List<FsmBindingDefinition> BindingDefinitions => bindingDefinitions;
        public List<FsmGraphStateNode> States => states;
        public List<FsmGraphTransition> Transitions => transitions;

        public FsmGraphRuntime CreateRuntime(
            FsmContext context,
            IReadOnlyList<FsmInjectedFieldBinding> injectedBindings = null,
            Object logContext = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            ApplyBlackboardDefaults(context.Blackboard);

            var runtimeObjects = new List<Object>();
            var runtimeStates = new Dictionary<string, IState>();

            foreach (var stateNode in states)
            {
                if (stateNode == null || string.IsNullOrWhiteSpace(stateNode.Id) || stateNode.State == null)
                    continue;

                var runtimeState = Instantiate(stateNode.State);
                runtimeState.name = $"{stateNode.Name} Runtime";
                runtimeState.hideFlags = HideFlags.HideAndDontSave;
                FsmBehaviourInjectionUtility.ApplyInjectedFields(
                    runtimeState,
                    FsmGraphBehaviourOwnerKind.State,
                    stateNode.Id,
                    injectedBindings,
                    logContext);
                runtimeState.Initialize(context);

                runtimeObjects.Add(runtimeState);
                runtimeStates[stateNode.Id] = new RuntimeStateAdapter(runtimeState);
            }

            var runtimeTransitions = new List<IStateTransition<string>>();

            foreach (var transition in transitions)
            {
                if (transition == null || transition.Transition == null)
                    continue;

                if (!runtimeStates.ContainsKey(transition.FromStateId) || !runtimeStates.ContainsKey(transition.ToStateId))
                    continue;

                var runtimeTransition = Instantiate(transition.Transition);
                runtimeTransition.name = $"{transition.FromStateId} -> {transition.ToStateId} Runtime";
                runtimeTransition.hideFlags = HideFlags.HideAndDontSave;
                FsmBehaviourInjectionUtility.ApplyInjectedFields(
                    runtimeTransition,
                    FsmGraphBehaviourOwnerKind.Transition,
                    transition.Id,
                    injectedBindings,
                    logContext);
                runtimeTransition.Initialize(context);

                runtimeObjects.Add(runtimeTransition);
                runtimeTransitions.Add(new StateTransition<string>(
                    transition.FromStateId,
                    transition.ToStateId,
                    runtimeTransition.CanTransition));
            }

            var initialState = ResolveInitialStateId(runtimeStates);
            var stateMachine = new AutoStateMachine<string>(initialState, runtimeStates, runtimeTransitions);
            return new FsmGraphRuntime(stateMachine, runtimeObjects);
        }

        public void ApplyBlackboardDefaults(Blackboard blackboard)
        {
            if (blackboard == null)
                return;

            foreach (var parameter in blackboardParameters)
                parameter?.ApplyTo(blackboard);
        }

        public FsmGraphStateNode FindState(string stateId) =>
            states.Find(state => state != null && state.Id == stateId);

        public FsmGraphTransition FindTransition(string transitionId) =>
            transitions.Find(transition => transition != null && transition.Id == transitionId);

        private string ResolveInitialStateId(IReadOnlyDictionary<string, IState> runtimeStates)
        {
            if (!string.IsNullOrWhiteSpace(initialStateId) && runtimeStates.ContainsKey(initialStateId))
                return initialStateId;

            foreach (var pair in runtimeStates)
                return pair.Key;

            throw new InvalidOperationException($"Graph '{name}' has no valid runtime states.");
        }

        private sealed class RuntimeStateAdapter : IState
        {
            private readonly FsmStateBehaviour _state;

            public RuntimeStateAdapter(FsmStateBehaviour state) => _state = state;

            public void OnEnter() => _state.OnEnter();

            public void OnUpdate(float deltaTime) => _state.OnUpdate(deltaTime);

            public void OnExit() => _state.OnExit();
        }
    }
}
