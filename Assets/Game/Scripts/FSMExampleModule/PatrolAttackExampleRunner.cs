using System.Collections.Generic;
using UnityEngine;

namespace FSMModule.Examples
{
    [AddComponentMenu("FSM Examples/Patrol Attack Example Runner")]
    public sealed class PatrolAttackExampleRunner : FsmRunner
    {
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private bool startInAttack;
        [SerializeField] private string attackMessage = "Attack state entered.";

        public bool IsAttackEnabled => Context != null ? ReadAttackFlag(Context) : startInAttack;

        protected override FsmContext CreateContext()
        {
            var context = base.CreateContext();
            context.Blackboard.SetValue(PatrolAttackExampleKeys.ShouldAttack, startInAttack);
            return context;
        }

        protected override IState CreateState(FsmContext context)
        {
            var states = new Dictionary<PatrolAttackStateId, IState>
            {
                [PatrolAttackStateId.Patrol] = new PatrolBetweenPointsState(
                    context,
                    PatrolAttackExampleKeys.PatrolPointA,
                    PatrolAttackExampleKeys.PatrolPointB,
                    patrolSpeed),
                [PatrolAttackStateId.Attack] = new DebugAttackState(attackMessage),
            };

            var transitions = new IStateTransition<PatrolAttackStateId>[]
            {
                new StateTransition<PatrolAttackStateId>(
                    PatrolAttackStateId.Patrol,
                    PatrolAttackStateId.Attack,
                    () => ReadAttackFlag(context)),
                new StateTransition<PatrolAttackStateId>(
                    PatrolAttackStateId.Attack,
                    PatrolAttackStateId.Patrol,
                    () => !ReadAttackFlag(context)),
            };

            return new AutoStateMachine<PatrolAttackStateId>(PatrolAttackStateId.Patrol, states, transitions);
        }

        public void SetAttackEnabled(bool value)
        {
            startInAttack = value;
            Context?.Blackboard.SetValue(PatrolAttackExampleKeys.ShouldAttack, value);
        }

        public void ToggleAttack() => SetAttackEnabled(!IsAttackEnabled);

        private static bool ReadAttackFlag(FsmContext context) =>
            context.Blackboard.TryGetValue(PatrolAttackExampleKeys.ShouldAttack, out bool shouldAttack) && shouldAttack;
    }
}
