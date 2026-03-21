using UnityEngine;

namespace FSMModule
{
    public sealed class FsmContext
    {
        public FsmContext()
            : this(null, null, null, null) { }

        public FsmContext(GameObject owner)
            : this(owner, owner != null ? owner.transform : null, null, null) { }

        public FsmContext(GameObject owner, Transform transform)
            : this(owner, transform, null, null) { }

        public FsmContext(GameObject owner, Transform transform, Blackboard blackboard, FsmBindings bindings)
        {
            Owner = owner != null ? owner : transform != null ? transform.gameObject : null;
            Transform = transform != null ? transform : Owner != null ? Owner.transform : null;
            Blackboard = blackboard ?? new Blackboard();
            Bindings = bindings ?? new FsmBindings();
        }

        public GameObject Owner { get; }
        public Transform Transform { get; }
        public Blackboard Blackboard { get; }
        public FsmBindings Bindings { get; }
    }
}
