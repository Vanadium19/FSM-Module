using UnityEngine;
using FSMModule.Graph;

namespace FSMModule.Examples
{
    public sealed class BlackboardBoolTransitionBehaviour : FsmTransitionBehaviour
    {
        [SerializeField] private string key = PatrolAttackExampleKeys.ShouldAttack;
        [SerializeField] private bool expectedValue = true;

        public override bool CanTransition() =>
            Blackboard.TryGetValue(key, out bool value) && value == expectedValue;
    }
}
