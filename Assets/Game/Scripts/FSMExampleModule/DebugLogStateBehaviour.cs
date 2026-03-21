using UnityEngine;
using FSMModule.Graph;

namespace FSMModule.Examples
{
    public sealed class DebugLogStateBehaviour : FsmStateBehaviour
    {
        [SerializeField] private string enterMessage = "Enter State";
        [SerializeField] private string exitMessage = "Exit State";
        [SerializeField] private bool logOnEnter = true;
        [SerializeField] private bool logOnExit;

        public override void OnEnter()
        {
            if (logOnEnter)
                Debug.Log(enterMessage);
        }

        public override void OnExit()
        {
            if (logOnExit)
                Debug.Log(exitMessage);
        }
    }
}
