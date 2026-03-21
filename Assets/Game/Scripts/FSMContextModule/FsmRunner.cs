using System;
using UnityEngine;

namespace FSMModule
{
    public abstract class FsmRunner : MonoBehaviour
    {
        [SerializeField] private ComponentBinding[] bindings = Array.Empty<ComponentBinding>();

        protected FsmContext Context { get; private set; }
        protected IState State { get; private set; }

        protected virtual void Awake()
        {
            Context = CreateContext();
            State = CreateState(Context);
        }

        protected virtual void OnEnable() => State?.OnEnter();

        protected virtual void Update() => State?.OnUpdate(Time.deltaTime);

        protected virtual void OnDisable() => State?.OnExit();

        protected virtual FsmContext CreateContext()
        {
            var context = new FsmContext(gameObject, transform);

            foreach (var binding in bindings)
            {
                if (binding == null || !binding.IsValid)
                    continue;

                context.Bindings.SetObject(binding.Key, binding.Target);
            }

            return context;
        }

        protected abstract IState CreateState(FsmContext context);
    }
}
