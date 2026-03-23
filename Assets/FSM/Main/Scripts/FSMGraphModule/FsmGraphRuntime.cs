using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FSMModule.Graph
{
    public sealed class FsmGraphRuntime : IState, IDisposable
    {
        private readonly IAutoStateMachine<string> _stateMachine;
        private readonly List<Object> _runtimeObjects;
        private bool _disposed;

        public FsmGraphRuntime(IAutoStateMachine<string> stateMachine, List<Object> runtimeObjects)
        {
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _runtimeObjects = runtimeObjects ?? new List<Object>();
        }

        public string CurrentStateId => _stateMachine.CurrentState;
        public IReadOnlyCollection<string> States => _stateMachine.States;
        public IEnumerable<(string, string)> Transitions => _stateMachine.Transitions;

        public void OnEnter() => _stateMachine.OnEnter();

        public void OnUpdate(float deltaTime) => _stateMachine.OnUpdate(deltaTime);

        public void OnExit() => _stateMachine.OnExit();

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (var runtimeObject in _runtimeObjects)
            {
                if (runtimeObject == null)
                    continue;

                if (Application.isPlaying)
                    Object.Destroy(runtimeObject);
                else
                    Object.DestroyImmediate(runtimeObject);
            }

            _runtimeObjects.Clear();
        }
    }
}
