using UnityEngine;
using FSMModule.Graph;

namespace FSMModule.Examples
{
    public sealed class PatrolBetweenBindingsStateBehaviour : FsmStateBehaviour
    {
        [SerializeField] private string pointAKey = PatrolAttackExampleKeys.PatrolPointA;
        [SerializeField] private string pointBKey = PatrolAttackExampleKeys.PatrolPointB;
        [SerializeField] private float speed = 2f;
        [SerializeField] private float reachDistance = 0.05f;

        private Transform _pointA;
        private Transform _pointB;
        private Transform _currentTarget;

        protected override void OnInitialize()
        {
            _pointA = Bindings.Get<Transform>(pointAKey);
            _pointB = Bindings.Get<Transform>(pointBKey);
        }

        public override void OnEnter() => _currentTarget = GetClosestTarget();

        public override void OnUpdate(float deltaTime)
        {
            if (OwnerTransform == null || _currentTarget == null)
                return;

            OwnerTransform.position = Vector3.MoveTowards(OwnerTransform.position, _currentTarget.position, speed * deltaTime);

            if ((OwnerTransform.position - _currentTarget.position).sqrMagnitude <= reachDistance * reachDistance)
                _currentTarget = _currentTarget == _pointA ? _pointB : _pointA;
        }

        private Transform GetClosestTarget()
        {
            var distanceToA = (OwnerTransform.position - _pointA.position).sqrMagnitude;
            var distanceToB = (OwnerTransform.position - _pointB.position).sqrMagnitude;
            return distanceToA <= distanceToB ? _pointA : _pointB;
        }
    }
}
