using UnityEngine;

namespace FSMModule
{
    public sealed class FsmContext
    {
        public FsmContext()
            : this(null, null, null) { }

        public FsmContext(GameObject owner)
            : this(owner, owner != null ? owner.transform : null, null) { }

        public FsmContext(GameObject owner, Transform transform)
            : this(owner, transform, null) { }

        public FsmContext(GameObject owner, Transform transform, Blackboard blackboard)
        {
            Owner = owner != null ? owner : transform != null ? transform.gameObject : null;
            Transform = transform != null ? transform : Owner != null ? Owner.transform : null;
            Blackboard = blackboard ?? new Blackboard();
        }

        public GameObject Owner { get; }
        public Transform Transform { get; }
        public Blackboard Blackboard { get; }
    }
}
