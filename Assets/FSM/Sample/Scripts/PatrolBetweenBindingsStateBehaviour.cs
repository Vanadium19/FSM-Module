using UnityEngine;
using FSMModule.Graph;

namespace FSMModule.Examples
{
    public sealed class PatrolBetweenBindingsStateBehaviour : FsmStateBehaviour
    {
        [FsmInject] private Transform pointA;
        [FsmInject] private Transform pointB;

        [SerializeField] private float speed = 2f;
        [SerializeField] private float reachDistance = 0.05f;

        private Transform _currentTarget;

        protected override void OnInitialize() => _currentTarget = null;

        public override void OnEnter() => _currentTarget = GetClosestTarget();

        public override void OnUpdate(float deltaTime)
        {
            if (OwnerTransform == null || pointA == null || pointB == null || _currentTarget == null)
                return;

            OwnerTransform.position = Vector3.MoveTowards(OwnerTransform.position, _currentTarget.position, speed * deltaTime);

            if ((OwnerTransform.position - _currentTarget.position).sqrMagnitude <= reachDistance * reachDistance)
                _currentTarget = _currentTarget == pointA ? pointB : pointA;
        }

        private Transform GetClosestTarget()
        {
            if (pointA == null || pointB == null || OwnerTransform == null)
                return null;

            var distanceToA = (OwnerTransform.position - pointA.position).sqrMagnitude;
            var distanceToB = (OwnerTransform.position - pointB.position).sqrMagnitude;
            return distanceToA <= distanceToB ? pointA : pointB;
        }
    }
}