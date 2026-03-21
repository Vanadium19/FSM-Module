using System;
using UnityEngine;

namespace FSMModule.Examples
{
    public sealed class PatrolBetweenPointsState : IState
    {
        private const float ReachDistance = 0.05f;

        private readonly Transform _owner;
        private readonly Transform _pointA;
        private readonly Transform _pointB;
        private readonly float _speed;

        private Transform _currentTarget;

        public PatrolBetweenPointsState(FsmContext context, string pointAKey, string pointBKey, float speed)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _owner = context.Transform != null
                ? context.Transform
                : throw new InvalidOperationException("Patrol state requires an owner transform.");

            _pointA = context.Bindings.Get<Transform>(pointAKey);
            _pointB = context.Bindings.Get<Transform>(pointBKey);
            _speed = Mathf.Max(0f, speed);
        }

        public void OnEnter() => _currentTarget = GetClosestTarget();

        public void OnUpdate(float deltaTime)
        {
            if (_currentTarget == null)
                return;

            _owner.position = Vector3.MoveTowards(_owner.position, _currentTarget.position, _speed * deltaTime);

            if ((_owner.position - _currentTarget.position).sqrMagnitude <= ReachDistance * ReachDistance)
                _currentTarget = _currentTarget == _pointA ? _pointB : _pointA;
        }

        public void OnExit() { }

        private Transform GetClosestTarget()
        {
            var distanceToA = (_owner.position - _pointA.position).sqrMagnitude;
            var distanceToB = (_owner.position - _pointB.position).sqrMagnitude;

            return distanceToA <= distanceToB ? _pointA : _pointB;
        }
    }
}
