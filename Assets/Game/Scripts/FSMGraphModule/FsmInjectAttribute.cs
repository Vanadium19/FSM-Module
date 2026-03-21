using System;
using UnityEngine;

namespace FSMModule.Graph
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FsmInjectAttribute : PropertyAttribute { }
}
