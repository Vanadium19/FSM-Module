using UnityEngine;

namespace FSMModule.Examples
{
    public sealed class DebugAttackState : IState
    {
        private readonly string _message;

        public DebugAttackState(string message) => _message = message;

        public void OnEnter() => Debug.Log(_message);

        public void OnUpdate(float deltaTime) { }

        public void OnExit() => Debug.Log("Attack state finished.");
    }
}
