using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FSMModule.Graph
{
    public static class FsmBehaviourInjectionUtility
    {
        private static readonly Dictionary<Type, FieldInfo[]> InjectableFieldCache = new();

        public static IReadOnlyList<FieldInfo> GetInjectableFields(Type type)
        {
            if (type == null)
                return Array.Empty<FieldInfo>();

            if (InjectableFieldCache.TryGetValue(type, out var cachedFields))
                return cachedFields;

            var typeHierarchy = new Stack<Type>();
            for (var currentType = type; currentType != null && currentType != typeof(object); currentType = currentType.BaseType)
                typeHierarchy.Push(currentType);

            var injectableFields = new List<FieldInfo>();
            while (typeHierarchy.Count > 0)
            {
                var currentType = typeHierarchy.Pop();
                var declaredFields = currentType.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                Array.Sort(declaredFields, (left, right) => left.MetadataToken.CompareTo(right.MetadataToken));

                foreach (var field in declaredFields)
                {
                    if (IsInjectableField(field))
                        injectableFields.Add(field);
                }
            }

            cachedFields = injectableFields.ToArray();
            InjectableFieldCache[type] = cachedFields;
            return cachedFields;
        }

        public static bool IsInjectableField(FieldInfo field)
        {
            if (field == null || field.IsStatic)
                return false;

            return field.GetCustomAttribute<FsmInjectAttribute>() != null &&
                   typeof(Object).IsAssignableFrom(field.FieldType);
        }

        public static void ApplyInjectedFields(
            Object target,
            FsmGraphBehaviourOwnerKind ownerKind,
            string ownerId,
            IReadOnlyList<FsmInjectedFieldBinding> bindings,
            Object logContext = null)
        {
            if (target == null)
                return;

            var injectableFields = GetInjectableFields(target.GetType());
            for (var i = 0; i < injectableFields.Count; i++)
            {
                var field = injectableFields[i];
                var rawValue = FindBoundObject(bindings, ownerKind, ownerId, field.Name);
                var resolvedValue = ResolveObject(field.FieldType, rawValue);

                if (rawValue != null && resolvedValue == null)
                {
                    Debug.LogWarning(
                        $"Could not inject '{field.Name}' on '{target.GetType().Name}'. " +
                        $"Expected {field.FieldType.Name}, got {rawValue.GetType().Name}.",
                        logContext);
                }

                field.SetValue(target, resolvedValue);
            }
        }

        public static Object ResolveObject(Type targetType, Object rawValue)
        {
            if (targetType == null || rawValue == null)
                return null;

            if (targetType.IsInstanceOfType(rawValue))
                return rawValue;

            return rawValue switch
            {
                GameObject gameObject => ResolveFromGameObject(targetType, gameObject),
                Component component => ResolveFromComponent(targetType, component),
                _ => null,
            };
        }

        private static Object FindBoundObject(
            IReadOnlyList<FsmInjectedFieldBinding> bindings,
            FsmGraphBehaviourOwnerKind ownerKind,
            string ownerId,
            string fieldName)
        {
            if (bindings == null)
                return null;

            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding == null ||
                    binding.OwnerKind != ownerKind ||
                    binding.OwnerId != ownerId ||
                    binding.FieldName != fieldName)
                {
                    continue;
                }

                return binding.Target;
            }

            return null;
        }

        private static Object ResolveFromGameObject(Type targetType, GameObject gameObject)
        {
            if (targetType == typeof(GameObject))
                return gameObject;

            if (targetType == typeof(Transform))
                return gameObject.transform;

            return typeof(Component).IsAssignableFrom(targetType)
                ? gameObject.GetComponent(targetType)
                : null;
        }

        private static Object ResolveFromComponent(Type targetType, Component component)
        {
            if (targetType == typeof(GameObject))
                return component.gameObject;

            if (targetType == typeof(Transform))
                return component.transform;

            return typeof(Component).IsAssignableFrom(targetType)
                ? component.GetComponent(targetType)
                : null;
        }
    }
}
