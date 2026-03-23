using System;
using UnityEngine;

namespace FSMModule.Graph
{
    [Serializable]
    public sealed class FsmGraphStateNode
    {
        [SerializeField] private string id;
        [SerializeField] private string name = "State";
        [SerializeField] private Vector2 position;
        [SerializeField] private FsmStateBehaviour state;

        public FsmGraphStateNode(string id, string name, Vector2 position)
        {
            this.id = id;
            this.name = name;
            this.position = position;
        }

        public string Id
        {
            get => id;
            set => id = value;
        }

        public string Name
        {
            get => name;
            set => name = value;
        }

        public Vector2 Position
        {
            get => position;
            set => position = value;
        }

        public FsmStateBehaviour State
        {
            get => state;
            set => state = value;
        }
    }
}
