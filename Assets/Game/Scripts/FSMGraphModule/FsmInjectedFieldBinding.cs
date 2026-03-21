using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FSMModule.Graph
{
    [Serializable]
    public sealed class FsmInjectedFieldBinding
    {
        [SerializeField] private FsmGraphBehaviourOwnerKind ownerKind;
        [SerializeField] private string ownerId;
        [SerializeField] private string fieldName;
        [SerializeField] private Object target;

        public FsmGraphBehaviourOwnerKind OwnerKind
        {
            get => ownerKind;
            set => ownerKind = value;
        }

        public string OwnerId
        {
            get => ownerId;
            set => ownerId = value;
        }

        public string FieldName
        {
            get => fieldName;
            set => fieldName = value;
        }

        public Object Target
        {
            get => target;
            set => target = value;
        }
    }
}
