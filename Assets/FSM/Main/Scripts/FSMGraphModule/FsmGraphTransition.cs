using System;
using UnityEngine;

namespace FSMModule.Graph
{
    [Serializable]
    public sealed class FsmGraphTransition
    {
        [SerializeField] private string id;
        [SerializeField] private string fromStateId;
        [SerializeField] private string toStateId;
        [SerializeField] private FsmTransitionBehaviour transition;

        public FsmGraphTransition(string id, string fromStateId, string toStateId)
        {
            this.id = id;
            this.fromStateId = fromStateId;
            this.toStateId = toStateId;
        }

        public string Id
        {
            get => id;
            set => id = value;
        }

        public string FromStateId
        {
            get => fromStateId;
            set => fromStateId = value;
        }

        public string ToStateId
        {
            get => toStateId;
            set => toStateId = value;
        }

        public FsmTransitionBehaviour Transition
        {
            get => transition;
            set => transition = value;
        }
    }
}
