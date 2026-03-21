using UnityEngine;

namespace FSMModule.Examples
{
    [AddComponentMenu("FSM Examples/Patrol Attack Example Input")]
    public sealed class PatrolAttackExampleInput : MonoBehaviour
    {
        [SerializeField] private PatrolAttackExampleRunner runner;
        [SerializeField] private KeyCode toggleAttackKey = KeyCode.Space;
        [SerializeField] private KeyCode forcePatrolKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode forceAttackKey = KeyCode.Alpha2;

        private void Reset() => runner = GetComponent<PatrolAttackExampleRunner>();

        private void Update()
        {
            if (runner == null)
                return;

            if (Input.GetKeyDown(toggleAttackKey))
                runner.ToggleAttack();

            if (Input.GetKeyDown(forcePatrolKey))
                runner.SetAttackEnabled(false);

            if (Input.GetKeyDown(forceAttackKey))
                runner.SetAttackEnabled(true);
        }
    }
}
