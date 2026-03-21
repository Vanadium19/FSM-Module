using UnityEngine;
using FSMModule.Graph;

namespace FSMModule.Examples
{
    [AddComponentMenu("FSM Examples/Patrol Attack Example Input")]
    public sealed class PatrolAttackExampleInput : MonoBehaviour
    {
        private const string DefaultAttackKey = "ShouldAttack";

        [SerializeField] private FsmGraphRunner runner;
        [SerializeField] private string attackKey = DefaultAttackKey;
        [SerializeField] private KeyCode toggleAttackKey = KeyCode.Space;
        [SerializeField] private KeyCode forcePatrolKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode forceAttackKey = KeyCode.Alpha2;

        private void Reset() => runner = GetComponent<FsmGraphRunner>();

        private void Update()
        {
            if (runner == null || runner.RuntimeContext == null)
                return;

            if (Input.GetKeyDown(toggleAttackKey))
                SetAttackEnabled(!IsAttackEnabled());

            if (Input.GetKeyDown(forcePatrolKey))
                SetAttackEnabled(false);

            if (Input.GetKeyDown(forceAttackKey))
                SetAttackEnabled(true);
        }

        private bool IsAttackEnabled() =>
            runner.RuntimeContext.Blackboard.TryGetValue(attackKey, out bool value) && value;

        private void SetAttackEnabled(bool value) =>
            runner.RuntimeContext.Blackboard.SetValue(attackKey, value);
    }
}
