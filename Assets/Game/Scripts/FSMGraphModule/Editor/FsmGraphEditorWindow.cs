using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace FSMModule.Graph.Editor
{
    public sealed class FsmGraphEditorWindow : EditorWindow
    {
        private const float ToolbarHeight = 24f;
        private const float InspectorWidth = 360f;
        private const float NodeWidth = 190f;
        private const float NodeHeight = 66f;

        private static readonly Color CanvasBackground = new(0.15f, 0.15f, 0.15f);
        private static readonly Color GridPrimary = new(1f, 1f, 1f, 0.04f);
        private static readonly Color GridSecondary = new(1f, 1f, 1f, 0.08f);
        private static readonly Color StateColor = new(0.24f, 0.26f, 0.29f);
        private static readonly Color InitialStateColor = new(0.68f, 0.42f, 0.12f);
        private static readonly Color CurrentStateColor = new(0.19f, 0.44f, 0.31f);
        private static readonly Color StateBorderColor = new(0f, 0f, 0f, 0.35f);
        private static readonly Color SelectedStateOutlineColor = new(0.97f, 0.79f, 0.30f);
        private static readonly Color TransitionColor = new(0.73f, 0.73f, 0.73f);
        private static readonly Color SelectedTransitionColor = new(0.95f, 0.72f, 0.18f);
        private static readonly Color PendingTransitionColor = new(0.44f, 0.80f, 0.46f);
        private const float TransitionLaneSpacing = 28f;
        private const float TransitionSelectionDistance = 12f;
        private const float TransitionArrowLength = 14f;
        private const float TransitionArrowWidth = 10f;
        private const float SelfTransitionLoopWidth = 44f;
        private const float SelfTransitionLoopHeight = 18f;
        private const float SelectedStateOutlineThickness = 3f;
        private const float StateCornerRadius = 10f;

        private FsmGraphAsset _graph;
        private FsmGraphRunner _runner;
        private Vector2 _canvasPan = new(120f, 120f);
        private Vector2 _inspectorScroll;
        private string _blackboardSearch = string.Empty;

        private string _selectedStateId;
        private string _selectedTransitionId;
        private string _pendingTransitionFromStateId;

        private string _draggedStateId;
        private Vector2 _dragOffset;
        private bool _isPanning;
        private Vector2 _panMouseStart;
        private Vector2 _panStart;

        private UnityEditor.Editor _embeddedEditor;
        private UnityEngine.Object _embeddedEditorTarget;

        [MenuItem("Window/FSM/Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<FsmGraphEditorWindow>("FSM Graph");
            window.minSize = new Vector2(960f, 560f);
            window.Show();
        }

        public static void Open(FsmGraphAsset graph)
        {
            var window = GetWindow<FsmGraphEditorWindow>("FSM Graph");
            window.minSize = new Vector2(960f, 560f);
            window.SetSelectionContext(graph, null);
            window.Show();
            window.Focus();
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not FsmGraphAsset graph)
                return false;

            Open(graph);
            return true;
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            Undo.undoRedoPerformed += HandleUndoRedoPerformed;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            if (_graph == null && TryGetSelectionContext(out var graph, out var runner))
                SetSelectionContext(graph, runner);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedoPerformed;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;

            if (_embeddedEditor != null)
                DestroyImmediate(_embeddedEditor);
        }

        private void OnSelectionChange()
        {
            if (TryGetSelectionContext(out var graph, out var runner))
            {
                SetSelectionContext(graph, runner);
                Repaint();
            }
        }

        private void OnInspectorUpdate()
        {
            if (HasRuntimeRunner)
                Repaint();
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (TryGetSelectionContext(out var graph, out var runner))
            {
                SetSelectionContext(graph, runner);
            }
            else if (_graph != null)
            {
                _runner = null;
            }

            Repaint();
        }

        private void OnGUI()
        {
            HandleKeyboardShortcuts(Event.current);
            DrawToolbar();

            if (_graph == null)
            {
                DrawEmptyState();
                return;
            }

            var canvasRect = new Rect(0f, ToolbarHeight, position.width - InspectorWidth, position.height - ToolbarHeight);
            var inspectorRect = new Rect(canvasRect.xMax, ToolbarHeight, InspectorWidth, position.height - ToolbarHeight);

            DrawCanvas(canvasRect);
            DrawInspector(inspectorRect);

            if (GUI.changed)
                Repaint();
        }

        private void HandleKeyboardShortcuts(Event currentEvent)
        {
            if (currentEvent == null || currentEvent.type != EventType.KeyDown)
                return;

            if (EditorGUIUtility.editingTextField)
                return;

            if (currentEvent.keyCode == KeyCode.Escape)
            {
                if (CancelCurrentAction())
                    currentEvent.Use();

                return;
            }

            if (!HasActionModifier(currentEvent))
                return;

            if (currentEvent.keyCode == KeyCode.Z && !currentEvent.shift)
            {
                Undo.PerformUndo();
                currentEvent.Use();
                return;
            }

            if ((currentEvent.keyCode == KeyCode.Z && currentEvent.shift) || currentEvent.keyCode == KeyCode.Y)
            {
                Undo.PerformRedo();
                currentEvent.Use();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(ToolbarHeight)))
            {
                GUILayout.Label(
                    GetToolbarTitle(),
                    EditorStyles.miniLabel,
                    GUILayout.Width(420f));

                using (new EditorGUI.DisabledScope(_graph == null))
                {
                    if (GUILayout.Button("New State", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                        AddState(GetCanvasCenterPosition());

                    if (GUILayout.Button("Frame Graph", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                        FrameGraph();
                }

                GUILayout.FlexibleSpace();

                if (!string.IsNullOrWhiteSpace(_pendingTransitionFromStateId) && _graph != null)
                {
                    var fromState = _graph.FindState(_pendingTransitionFromStateId);
                    var label = fromState != null
                        ? $"Connecting from {fromState.Name}. Click a target state."
                        : "Click a target state.";
                    GUILayout.Label(label, EditorStyles.miniLabel);
                }
            }
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("FSM Graph Editor", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Select an FSM graph asset in the Project view or a scene object with FsmGraphRunner to open its graph.",
                    MessageType.Info);
            }
            GUILayout.FlexibleSpace();
        }

        private void DrawCanvas(Rect canvasRect)
        {
            EditorGUI.DrawRect(canvasRect, CanvasBackground);
            DrawGrid(canvasRect, 20f, GridPrimary);
            DrawGrid(canvasRect, 100f, GridSecondary);

            HandleCanvasEvents(canvasRect, Event.current);

            foreach (var transition in _graph.Transitions)
                DrawTransition(canvasRect, transition);

            foreach (var state in _graph.States)
                DrawStateNode(canvasRect, state);

            if (!string.IsNullOrWhiteSpace(_pendingTransitionFromStateId))
                DrawPendingTransition(canvasRect, _pendingTransitionFromStateId, Event.current.mousePosition);
        }

        private void DrawGrid(Rect canvasRect, float spacing, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;

            var startX = canvasRect.x + (_canvasPan.x % spacing);
            for (var x = startX; x < canvasRect.xMax; x += spacing)
                Handles.DrawLine(new Vector3(x, canvasRect.y, 0f), new Vector3(x, canvasRect.yMax, 0f));

            var startY = canvasRect.y + (_canvasPan.y % spacing);
            for (var y = startY; y < canvasRect.yMax; y += spacing)
                Handles.DrawLine(new Vector3(canvasRect.x, y, 0f), new Vector3(canvasRect.xMax, y, 0f));

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawStateNode(Rect canvasRect, FsmGraphStateNode state)
        {
            if (state == null)
                return;

            var nodeRect = GetNodeRect(canvasRect, state);
            var isSelected = state.Id == _selectedStateId;
            var isInitial = state.Id == _graph.InitialStateId;
            var isCurrent = state.Id == GetRuntimeCurrentStateId();
            var fillColor = isCurrent
                ? CurrentStateColor
                : isInitial
                    ? InitialStateColor
                    : StateColor;

            DrawRoundedRect(nodeRect, fillColor, StateBorderColor, StateCornerRadius);

            if (isSelected)
                DrawStateSelectionOutline(nodeRect);

            var nameRect = new Rect(nodeRect.x + 12f, nodeRect.y + 8f, nodeRect.width - 24f, 18f);
            var typeRect = new Rect(nodeRect.x + 12f, nodeRect.y + 33f, nodeRect.width - 24f, 16f);

            EditorGUI.LabelField(nameRect, state.Name, EditorStyles.boldLabel);
            EditorGUI.LabelField(
                typeRect,
                state.State != null ? state.State.GetType().Name : "No State Behaviour",
                EditorStyles.miniLabel);
        }

        private static void DrawStateSelectionOutline(Rect nodeRect)
        {
            Handles.BeginGUI();
            Handles.color = SelectedStateOutlineColor;
            DrawRoundedOutline(nodeRect, StateCornerRadius, SelectedStateOutlineThickness);
            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private static void DrawRoundedRect(Rect rect, Color fillColor, Color borderColor, float radius)
        {
            var fillPoints = BuildRoundedRectPoints(rect, radius, false);

            Handles.BeginGUI();
            Handles.color = fillColor;
            Handles.DrawAAConvexPolygon(fillPoints);
            Handles.color = borderColor;
            DrawRoundedOutline(rect, radius, 1.5f);
            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private static void DrawRoundedOutline(Rect rect, float radius, float thickness)
        {
            var points = BuildRoundedRectPoints(rect, radius, true);
            Handles.DrawAAPolyLine(thickness, points);
        }

        private static Vector3[] BuildRoundedRectPoints(Rect rect, float radius, bool closedLoop)
        {
            radius = Mathf.Min(radius, rect.width * 0.5f, rect.height * 0.5f);

            if (radius <= 0f)
            {
                var sharpPoints = new[]
                {
                    new Vector3(rect.xMin, rect.yMin),
                    new Vector3(rect.xMax, rect.yMin),
                    new Vector3(rect.xMax, rect.yMax),
                    new Vector3(rect.xMin, rect.yMax),
                };

                if (!closedLoop)
                    return sharpPoints;

                return new[]
                {
                    sharpPoints[0],
                    sharpPoints[1],
                    sharpPoints[2],
                    sharpPoints[3],
                    sharpPoints[0],
                };
            }

            var points = new List<Vector3>(21);
            AddRoundedCorner(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, false);
            AddRoundedCorner(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f, true);
            AddRoundedCorner(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, true);
            AddRoundedCorner(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, true);

            if (closedLoop)
                points.Add(points[0]);

            return points.ToArray();
        }

        private static void AddRoundedCorner(
            List<Vector3> points,
            Vector2 center,
            float radius,
            float startAngle,
            float endAngle,
            bool skipFirstPoint)
        {
            const int segments = 6;
            for (var i = skipFirstPoint ? 1 : 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
                points.Add(new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius));
            }
        }

        private void DrawTransition(Rect canvasRect, FsmGraphTransition transition)
        {
            if (transition == null)
                return;

            var fromState = _graph.FindState(transition.FromStateId);
            var toState = _graph.FindState(transition.ToStateId);
            if (fromState == null || toState == null)
                return;

            var visual = GetTransitionVisualData(canvasRect, transition, fromState, toState);
            var color = transition.Id == _selectedTransitionId ? SelectedTransitionColor : TransitionColor;

            Handles.BeginGUI();
            Handles.color = color;
            DrawTransitionPolyline(visual.Points);
            DrawTransitionArrow(visual.ArrowTip, visual.ArrowDirection);
            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawPendingTransition(Rect canvasRect, string fromStateId, Vector2 mousePosition)
        {
            var fromState = _graph.FindState(fromStateId);
            if (fromState == null)
                return;

            var startRect = GetNodeRect(canvasRect, fromState);
            var start = GetRectEdgePoint(startRect, mousePosition - startRect.center);

            Handles.BeginGUI();
            Handles.color = PendingTransitionColor;
            Handles.DrawAAPolyLine(3f, start, mousePosition);
            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawInspector(Rect inspectorRect)
        {
            GUILayout.BeginArea(inspectorRect, EditorStyles.helpBox);
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

            DrawGraphSection();
            EditorGUILayout.Space();
            DrawBlackboardSection();
            EditorGUILayout.Space();
            DrawSelectionSection();

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawGraphSection()
        {
            EditorGUILayout.LabelField("Graph", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Asset", _graph != null ? _graph.name : "<None>");

            if (_runner != null)
                EditorGUILayout.LabelField("Runner", _runner.name);

            if (_graph.States.Count == 0)
            {
                EditorGUILayout.HelpBox("Add at least one state to make the graph runnable.", MessageType.Info);
                return;
            }

            var stateNames = _graph.States.Select(state => state.Name).ToArray();
            var currentIndex = Mathf.Max(0, _graph.States.FindIndex(state => state.Id == _graph.InitialStateId));
            var updatedIndex = EditorGUILayout.Popup("Initial State", currentIndex, stateNames);

            if (updatedIndex >= 0 && updatedIndex < _graph.States.Count && updatedIndex != currentIndex)
            {
                RecordGraph("Change Initial State");
                _graph.InitialStateId = _graph.States[updatedIndex].Id;
                MarkDirty();
            }
        }

        private void DrawBlackboardSection()
        {
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (HasRuntimeRunner)
                    EditorGUILayout.HelpBox($"Showing runtime values from '{_runner.name}'.", MessageType.None);

                DrawBlackboardToolbar();

                var visibleParameterCount = 0;
                for (var i = 0; i < _graph.BlackboardParameters.Count; i++)
                {
                    var parameter = _graph.BlackboardParameters[i];
                    if (parameter == null || !MatchesBlackboardSearch(parameter))
                        continue;

                    visibleParameterCount++;

                    if (DrawBlackboardParameterRow(parameter, i))
                        return;
                }

                if (_graph.BlackboardParameters.Count == 0)
                    EditorGUILayout.HelpBox("No parameters yet. Use + to add Float, Int or Bool.", MessageType.None);
                else if (visibleParameterCount == 0)
                    EditorGUILayout.HelpBox("No parameters match the current search.", MessageType.None);
            }
        }

        private void DrawBlackboardToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _blackboardSearch = EditorGUILayout.TextField(
                    _blackboardSearch,
                    GetToolbarSearchFieldStyle(),
                    GUILayout.ExpandWidth(true));

                using (new EditorGUI.DisabledScope(HasRuntimeRunner))
                {
                    if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(26f)))
                        ShowAddBlackboardParameterMenu();
                }
            }
        }

        private bool DrawBlackboardParameterRow(FsmBlackboardParameterDefinition parameter, int index)
        {
            if (HasRuntimeRunner)
            {
                DrawRuntimeBlackboardParameterRow(parameter);
                return false;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(parameter.Type.ToString(), EditorStyles.miniLabel, GUILayout.Width(34f));

                var updatedKey = EditorGUILayout.TextField(parameter.Key, GUILayout.ExpandWidth(true));
                var updatedBoolValue = parameter.BoolValue;
                var updatedIntValue = parameter.IntValue;
                var updatedFloatValue = parameter.FloatValue;

                switch (parameter.Type)
                {
                    case BlackboardParameterType.Bool:
                        updatedBoolValue = EditorGUILayout.Toggle(parameter.BoolValue, GUILayout.Width(18f));
                        break;
                    case BlackboardParameterType.Int:
                        updatedIntValue = EditorGUILayout.IntField(parameter.IntValue, GUILayout.Width(64f));
                        break;
                    case BlackboardParameterType.Float:
                        updatedFloatValue = EditorGUILayout.FloatField(parameter.FloatValue, GUILayout.Width(64f));
                        break;
                }

                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(22f)))
                {
                    RecordGraph("Remove Parameter");
                    _graph.BlackboardParameters.RemoveAt(index);
                    MarkDirty();
                    return true;
                }

                if (updatedKey != parameter.Key ||
                    updatedBoolValue != parameter.BoolValue ||
                    updatedIntValue != parameter.IntValue ||
                    !Mathf.Approximately(updatedFloatValue, parameter.FloatValue))
                {
                    RecordGraph("Edit Parameter");
                    parameter.Key = updatedKey;
                    parameter.BoolValue = updatedBoolValue;
                    parameter.IntValue = updatedIntValue;
                    parameter.FloatValue = updatedFloatValue;
                    MarkDirty();
                }
            }

            return false;
        }

        private void DrawRuntimeBlackboardParameterRow(FsmBlackboardParameterDefinition parameter)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(parameter.Type.ToString(), EditorStyles.miniLabel, GUILayout.Width(34f));

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.TextField(parameter.Key, GUILayout.ExpandWidth(true));

                switch (parameter.Type)
                {
                    case BlackboardParameterType.Bool:
                        EditorGUI.BeginChangeCheck();
                        var updatedBoolValue = EditorGUILayout.Toggle(GetRuntimeBoolValue(parameter), GUILayout.Width(18f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            _runner.SetBool(parameter.Key, updatedBoolValue);
                            GUI.changed = true;
                        }

                        break;
                    case BlackboardParameterType.Int:
                        EditorGUI.BeginChangeCheck();
                        var updatedIntValue = EditorGUILayout.IntField(GetRuntimeIntValue(parameter), GUILayout.Width(64f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            _runner.SetInt(parameter.Key, updatedIntValue);
                            GUI.changed = true;
                        }

                        break;
                    case BlackboardParameterType.Float:
                        EditorGUI.BeginChangeCheck();
                        var updatedFloatValue = EditorGUILayout.FloatField(GetRuntimeFloatValue(parameter), GUILayout.Width(64f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            _runner.SetFloat(parameter.Key, updatedFloatValue);
                            GUI.changed = true;
                        }

                        break;
                }
            }
        }

        private void ShowAddBlackboardParameterMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Float"), false, () => AddBlackboardParameter(BlackboardParameterType.Float));
            menu.AddItem(new GUIContent("Int"), false, () => AddBlackboardParameter(BlackboardParameterType.Int));
            menu.AddItem(new GUIContent("Bool"), false, () => AddBlackboardParameter(BlackboardParameterType.Bool));
            menu.ShowAsContext();
        }

        private void AddBlackboardParameter(BlackboardParameterType type)
        {
            RecordGraph("Add Parameter");

            var parameter = new FsmBlackboardParameterDefinition
            {
                Type = type,
                Key = GetUniqueBlackboardParameterName(type),
            };

            _graph.BlackboardParameters.Add(parameter);
            _blackboardSearch = string.Empty;
            MarkDirty();
        }

        private string GetUniqueBlackboardParameterName(BlackboardParameterType type)
        {
            var baseName = $"New {type}";
            var candidate = baseName;
            var suffix = 1;

            while (_graph.BlackboardParameters.Any(parameter =>
                       parameter != null &&
                       string.Equals(parameter.Key, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                suffix++;
                candidate = $"{baseName} {suffix}";
            }

            return candidate;
        }

        private bool MatchesBlackboardSearch(FsmBlackboardParameterDefinition parameter)
        {
            if (parameter == null)
                return false;

            if (string.IsNullOrWhiteSpace(_blackboardSearch))
                return true;

            return (!string.IsNullOrWhiteSpace(parameter.Key) &&
                    parameter.Key.IndexOf(_blackboardSearch, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   parameter.Type.ToString().IndexOf(_blackboardSearch, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static GUIStyle GetToolbarSearchFieldStyle() =>
            GUI.skin.FindStyle("ToolbarSearchTextField") ??
            GUI.skin.FindStyle("ToolbarSeachTextField") ??
            EditorStyles.textField;

        private void DrawSelectionSection()
        {
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);

            if (_graph.FindState(_selectedStateId) is { } selectedState)
            {
                DrawSelectedState(selectedState);
                return;
            }

            if (_graph.FindTransition(_selectedTransitionId) is { } selectedTransition)
            {
                DrawSelectedTransition(selectedTransition);
                return;
            }

            EditorGUILayout.HelpBox("Select a state or a transition on the canvas to edit it.", MessageType.None);
        }

        private void DrawSelectedState(FsmGraphStateNode state)
        {
            EditorGUILayout.LabelField("State Node", EditorStyles.miniBoldLabel);

            var updatedName = EditorGUILayout.TextField("Name", state.Name);
            if (updatedName != state.Name)
            {
                RecordGraph("Rename State");
                state.Name = updatedName;
                MarkDirty();
            }

            EditorGUI.BeginChangeCheck();
            var updatedState = (FsmStateBehaviour)EditorGUILayout.ObjectField("Behaviour", state.State, typeof(FsmStateBehaviour), false);
            if (EditorGUI.EndChangeCheck())
            {
                RecordGraph("Assign State Behaviour");
                state.State = updatedState;
                ClearEmbeddedEditor();
                MarkDirty();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Behaviour"))
                    ShowCreateStateMenu(state);

                if (GUILayout.Button("Start Transition"))
                {
                    _pendingTransitionFromStateId = state.Id;
                    _selectedTransitionId = null;
                }
            }

            if (state.State != null)
                DrawEmbeddedEditor(state.State);
            else
                EditorGUILayout.HelpBox("Choose or create a state behaviour. Users only need to inherit from FsmStateBehaviour.", MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("Delete State"))
                DeleteState(state);
        }

        private void DrawSelectedTransition(FsmGraphTransition transition)
        {
            EditorGUILayout.LabelField("Transition", EditorStyles.miniBoldLabel);

            var stateOptions = _graph.States.ToArray();
            var stateNames = stateOptions.Select(state => state.Name).ToArray();

            var fromIndex = Mathf.Max(0, Array.FindIndex(stateOptions, state => state.Id == transition.FromStateId));
            var toIndex = Mathf.Max(0, Array.FindIndex(stateOptions, state => state.Id == transition.ToStateId));

            if (stateOptions.Length > 0)
            {
                var updatedFromIndex = EditorGUILayout.Popup("From", fromIndex, stateNames);
                var updatedToIndex = EditorGUILayout.Popup("To", toIndex, stateNames);

                if (updatedFromIndex != fromIndex)
                {
                    RecordGraph("Change Transition Source");
                    transition.FromStateId = stateOptions[updatedFromIndex].Id;
                    MarkDirty();
                }

                if (updatedToIndex != toIndex)
                {
                    RecordGraph("Change Transition Target");
                    transition.ToStateId = stateOptions[updatedToIndex].Id;
                    MarkDirty();
                }
            }

            if (transition.Transition == null)
            {
                EditorGUILayout.HelpBox("This transition has no settings asset yet.", MessageType.Warning);

                if (GUILayout.Button("Create Standard Transition"))
                    ReplaceWithStandardTransition(transition);
            }
            else if (transition.Transition is not FsmBlackboardTransitionBehaviour)
            {
                EditorGUILayout.HelpBox(
                    "Legacy custom transition detected. New transitions use the built-in blackboard condition workflow.",
                    MessageType.Warning);

                if (GUILayout.Button("Replace With Standard Transition"))
                    ReplaceWithStandardTransition(transition);

                DrawEmbeddedEditor(transition.Transition);
            }
            else
            {
                DrawEmbeddedEditor(transition.Transition);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Delete Transition"))
                DeleteTransition(transition);
        }

        private void DrawEmbeddedEditor(UnityEngine.Object target)
        {
            if (_embeddedEditorTarget != target)
            {
                ClearEmbeddedEditor();
                _embeddedEditor = UnityEditor.Editor.CreateEditor(target);
                _embeddedEditorTarget = target;
            }

            if (_embeddedEditor == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Behaviour Settings", EditorStyles.miniBoldLabel);
            _embeddedEditor.OnInspectorGUI();
        }

        private bool CancelCurrentAction()
        {
            var hadAction =
                !string.IsNullOrWhiteSpace(_pendingTransitionFromStateId) ||
                !string.IsNullOrWhiteSpace(_draggedStateId) ||
                _isPanning;

            if (!hadAction)
                return false;

            _pendingTransitionFromStateId = null;
            _draggedStateId = null;
            _isPanning = false;
            GUI.changed = true;
            Repaint();
            return true;
        }

        private void HandleCanvasEvents(Rect canvasRect, Event currentEvent)
        {
            if (!canvasRect.Contains(currentEvent.mousePosition))
                return;

            if (!string.IsNullOrWhiteSpace(_pendingTransitionFromStateId) &&
                (currentEvent.type == EventType.MouseMove || currentEvent.type == EventType.MouseDrag))
            {
                Repaint();
            }

            if (currentEvent.type == EventType.MouseDown && (currentEvent.button == 2 || (currentEvent.button == 0 && currentEvent.alt)))
            {
                _isPanning = true;
                _panMouseStart = currentEvent.mousePosition;
                _panStart = _canvasPan;
                currentEvent.Use();
                return;
            }

            if (_isPanning && currentEvent.type == EventType.MouseDrag)
            {
                _canvasPan = _panStart + (currentEvent.mousePosition - _panMouseStart);
                GUI.changed = true;
                currentEvent.Use();
                return;
            }

            if (_isPanning && currentEvent.type == EventType.MouseUp)
            {
                _isPanning = false;
                currentEvent.Use();
                return;
            }

            var hoveredState = FindStateAt(canvasRect, currentEvent.mousePosition);
            var hoveredTransition = hoveredState == null ? FindTransitionAt(canvasRect, currentEvent.mousePosition) : null;

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
            {
                if (hoveredState != null)
                {
                    _selectedStateId = hoveredState.Id;
                    _selectedTransitionId = null;
                    ShowStateContextMenu(hoveredState);
                }
                else if (hoveredTransition != null)
                {
                    _selectedTransitionId = hoveredTransition.Id;
                    _selectedStateId = null;
                    GUI.changed = true;
                }
                else
                {
                    ShowCanvasContextMenu(ScreenToCanvasPosition(currentEvent.mousePosition));
                }

                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                if (!string.IsNullOrWhiteSpace(_pendingTransitionFromStateId) && hoveredState != null)
                {
                    if (_pendingTransitionFromStateId != hoveredState.Id)
                        AddTransition(_pendingTransitionFromStateId, hoveredState.Id);

                    _pendingTransitionFromStateId = null;
                    currentEvent.Use();
                    return;
                }

                if (hoveredState != null)
                {
                    _selectedStateId = hoveredState.Id;
                    _selectedTransitionId = null;
                    _draggedStateId = hoveredState.Id;
                    _dragOffset = currentEvent.mousePosition - GetNodeRect(canvasRect, hoveredState).position;
                    RecordGraph("Move FSM State");
                    currentEvent.Use();
                    return;
                }

                if (hoveredTransition != null)
                {
                    _selectedTransitionId = hoveredTransition.Id;
                    _selectedStateId = null;
                    GUI.changed = true;
                    currentEvent.Use();
                    return;
                }

                _selectedStateId = null;
                _selectedTransitionId = null;
                GUI.changed = true;
            }

            if (currentEvent.type == EventType.MouseDrag && !string.IsNullOrWhiteSpace(_draggedStateId))
            {
                var draggedState = _graph.FindState(_draggedStateId);
                if (draggedState != null)
                {
                    draggedState.Position = currentEvent.mousePosition - canvasRect.position - _canvasPan - _dragOffset;
                    MarkDirty();
                    GUI.changed = true;
                }

                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseUp)
                _draggedStateId = null;
        }

        private void ShowCanvasContextMenu(Vector2 canvasPosition)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add State"), false, () => AddState(canvasPosition));
            menu.ShowAsContext();
        }

        private void ShowStateContextMenu(FsmGraphStateNode state)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Start Transition"), false, () => _pendingTransitionFromStateId = state.Id);

            if (_graph.InitialStateId == state.Id)
                menu.AddDisabledItem(new GUIContent("Set As Default"));
            else
                menu.AddItem(new GUIContent("Set As Default"), false, () => SetInitialState(state));

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Delete State"), false, () => DeleteState(state));
            menu.ShowAsContext();
        }

        private void SetInitialState(FsmGraphStateNode state)
        {
            if (state == null || _graph == null || _graph.InitialStateId == state.Id)
                return;

            RecordGraph("Set Initial State");
            _graph.InitialStateId = state.Id;
            _selectedStateId = state.Id;
            _selectedTransitionId = null;
            MarkDirty();
        }

        private void ShowCreateStateMenu(FsmGraphStateNode state)
        {
            var menu = new GenericMenu();
            var types = GetCreatableTypes<FsmStateBehaviour>().ToArray();

            if (types.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No FsmStateBehaviour types found"));
                menu.ShowAsContext();
                return;
            }

            foreach (var type in types)
                menu.AddItem(new GUIContent(type.Name), false, () => AssignNewStateBehaviour(state, type));

            menu.ShowAsContext();
        }

        private void AssignNewStateBehaviour(FsmGraphStateNode state, Type type)
        {
            RecordGraph("Create State Behaviour");
            RemoveOwnedSubAsset(state.State);

            var behaviour = CreateSubAsset<FsmStateBehaviour>(type, $"{state.Name} {type.Name}");
            state.State = behaviour;
            _selectedStateId = state.Id;
            ClearEmbeddedEditor();
            MarkDirty();
        }

        private void ReplaceWithStandardTransition(FsmGraphTransition transition)
        {
            RecordGraph("Replace Transition Behaviour");
            RemoveOwnedSubAsset(transition.Transition);

            var behaviour = CreateSubAsset<FsmBlackboardTransitionBehaviour>(
                typeof(FsmBlackboardTransitionBehaviour),
                "Transition");
            transition.Transition = behaviour;
            _selectedTransitionId = transition.Id;
            ClearEmbeddedEditor();
            MarkDirty();
        }

        private void AddState(Vector2 canvasPosition)
        {
            RecordGraph("Add FSM State");

            var id = Guid.NewGuid().ToString("N");
            var name = $"State {_graph.States.Count + 1}";
            var state = new FsmGraphStateNode(id, name, canvasPosition);

            _graph.States.Add(state);
            if (string.IsNullOrWhiteSpace(_graph.InitialStateId))
                _graph.InitialStateId = id;

            _selectedStateId = id;
            _selectedTransitionId = null;
            MarkDirty();
        }

        private void AddTransition(string fromStateId, string toStateId)
        {
            RecordGraph("Add FSM Transition");
            var transition = new FsmGraphTransition(Guid.NewGuid().ToString("N"), fromStateId, toStateId);
            transition.Transition = CreateSubAsset<FsmBlackboardTransitionBehaviour>(
                typeof(FsmBlackboardTransitionBehaviour),
                "Transition");
            _graph.Transitions.Add(transition);
            _selectedTransitionId = transition.Id;
            _selectedStateId = null;
            MarkDirty();
        }

        private void DeleteState(FsmGraphStateNode state)
        {
            if (state == null)
                return;

            RecordGraph("Delete FSM State");
            RemoveOwnedSubAsset(state.State);

            for (var i = _graph.Transitions.Count - 1; i >= 0; i--)
            {
                var transition = _graph.Transitions[i];
                if (transition == null)
                    continue;

                if (transition.FromStateId != state.Id && transition.ToStateId != state.Id)
                    continue;

                RemoveOwnedSubAsset(transition.Transition);
                _graph.Transitions.RemoveAt(i);
            }

            _graph.States.Remove(state);

            if (_graph.InitialStateId == state.Id)
                _graph.InitialStateId = _graph.States.Count > 0 ? _graph.States[0].Id : null;

            _selectedStateId = null;
            _selectedTransitionId = null;
            ClearEmbeddedEditor();
            MarkDirty();
        }

        private void DeleteTransition(FsmGraphTransition transition)
        {
            if (transition == null)
                return;

            RecordGraph("Delete FSM Transition");
            RemoveOwnedSubAsset(transition.Transition);
            _graph.Transitions.Remove(transition);
            _selectedTransitionId = null;
            ClearEmbeddedEditor();
            MarkDirty();
        }

        private Vector2 GetCanvasCenterPosition()
        {
            var canvasSize = new Vector2(position.width - InspectorWidth, position.height - ToolbarHeight);
            return (canvasSize * 0.5f) - _canvasPan - new Vector2(NodeWidth * 0.5f, NodeHeight * 0.5f);
        }

        private void FrameGraph()
        {
            if (_graph == null || _graph.States.Count == 0)
                return;

            var min = _graph.States[0].Position;
            var max = _graph.States[0].Position + new Vector2(NodeWidth, NodeHeight);

            foreach (var state in _graph.States)
            {
                min = Vector2.Min(min, state.Position);
                max = Vector2.Max(max, state.Position + new Vector2(NodeWidth, NodeHeight));
            }

            var graphSize = max - min;
            var canvasSize = new Vector2(position.width - InspectorWidth, position.height - ToolbarHeight);
            _canvasPan = (canvasSize - graphSize) * 0.5f - min;
            Repaint();
        }

        private FsmGraphStateNode FindStateAt(Rect canvasRect, Vector2 mousePosition)
        {
            for (var i = _graph.States.Count - 1; i >= 0; i--)
            {
                var state = _graph.States[i];
                if (state != null && GetNodeRect(canvasRect, state).Contains(mousePosition))
                    return state;
            }

            return null;
        }

        private FsmGraphTransition FindTransitionAt(Rect canvasRect, Vector2 mousePosition)
        {
            FsmGraphTransition closestTransition = null;
            var closestDistance = TransitionSelectionDistance;

            for (var i = _graph.Transitions.Count - 1; i >= 0; i--)
            {
                var transition = _graph.Transitions[i];
                if (transition == null)
                    continue;

                var fromState = _graph.FindState(transition.FromStateId);
                var toState = _graph.FindState(transition.ToStateId);
                if (fromState == null || toState == null)
                    continue;

                var visual = GetTransitionVisualData(canvasRect, transition, fromState, toState);
                var distance = GetDistanceToPolyline(mousePosition, visual.Points);

                if (distance > closestDistance)
                    continue;

                closestDistance = distance;
                closestTransition = transition;
            }

            return closestTransition;
        }

        private Rect GetNodeRect(Rect canvasRect, FsmGraphStateNode state) =>
            new(canvasRect.x + _canvasPan.x + state.Position.x, canvasRect.y + _canvasPan.y + state.Position.y, NodeWidth, NodeHeight);

        private TransitionVisualData GetTransitionVisualData(
            Rect canvasRect,
            FsmGraphTransition transition,
            FsmGraphStateNode fromState,
            FsmGraphStateNode toState)
        {
            var fromRect = GetNodeRect(canvasRect, fromState);
            var toRect = GetNodeRect(canvasRect, toState);

            if (transition.FromStateId == transition.ToStateId)
            {
                var loopOffset = GetSelfTransitionLoopOffset(transition);
                var start = new Vector2(fromRect.xMax, fromRect.center.y - SelfTransitionLoopHeight);
                var cornerTop = new Vector2(fromRect.xMax + SelfTransitionLoopWidth + loopOffset, fromRect.center.y - SelfTransitionLoopHeight);
                var cornerBottom = new Vector2(fromRect.xMax + SelfTransitionLoopWidth + loopOffset, fromRect.center.y + SelfTransitionLoopHeight);
                var end = new Vector2(fromRect.xMax, fromRect.center.y + SelfTransitionLoopHeight);
                var arrowTip = Vector2.Lerp(cornerTop, cornerBottom, 0.5f);

                return new TransitionVisualData(
                    new[] { start, cornerTop, cornerBottom, end },
                    arrowTip,
                    (cornerBottom - cornerTop).normalized);
            }

            var direction = toRect.center - fromRect.center;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                direction = Vector2.right;

            direction.Normalize();

            var offset = GetTransitionLaneOffset(transition);
            var startPoint = GetRectEdgePoint(fromRect, fromRect.center + offset, direction);
            var endPoint = GetRectEdgePoint(toRect, toRect.center + offset, -direction);
            var arrowDirection = endPoint - startPoint;
            if (arrowDirection.sqrMagnitude <= Mathf.Epsilon)
                arrowDirection = direction;

            return new TransitionVisualData(
                new[] { startPoint, endPoint },
                Vector2.Lerp(startPoint, endPoint, 0.5f),
                arrowDirection.normalized);
        }

        private Vector2 GetTransitionLaneOffset(FsmGraphTransition transition)
        {
            var pairTransitions = _graph.Transitions
                .Where(candidate =>
                    candidate != null &&
                    ((candidate.FromStateId == transition.FromStateId && candidate.ToStateId == transition.ToStateId) ||
                     (candidate.FromStateId == transition.ToStateId && candidate.ToStateId == transition.FromStateId)))
                .ToArray();

            if (pairTransitions.Length <= 1)
                return Vector2.zero;

            var pairStartId = string.CompareOrdinal(transition.FromStateId, transition.ToStateId) <= 0
                ? transition.FromStateId
                : transition.ToStateId;
            var pairEndId = pairStartId == transition.FromStateId ? transition.ToStateId : transition.FromStateId;

            var pairStartState = _graph.FindState(pairStartId);
            var pairEndState = _graph.FindState(pairEndId);
            if (pairStartState == null || pairEndState == null)
                return Vector2.zero;

            var pairDirectionTransitions = pairTransitions
                .Where(candidate => candidate.FromStateId == transition.FromStateId && candidate.ToStateId == transition.ToStateId)
                .OrderBy(candidate => candidate.Id)
                .ToArray();

            var reverseDirectionCount = pairTransitions.Length - pairDirectionTransitions.Length;
            var directionIndex = Array.FindIndex(pairDirectionTransitions, candidate => candidate.Id == transition.Id);
            if (directionIndex < 0)
                directionIndex = 0;

            var canonicalDirection = (pairEndState.Position - pairStartState.Position).normalized;
            if (canonicalDirection.sqrMagnitude <= Mathf.Epsilon)
                canonicalDirection = Vector2.right;

            var perpendicular = new Vector2(-canonicalDirection.y, canonicalDirection.x);

            if (reverseDirectionCount > 0)
            {
                var side = transition.FromStateId == pairStartId ? 1f : -1f;
                return perpendicular * side * ((directionIndex + 1) * TransitionLaneSpacing);
            }

            var centeredLane = directionIndex - ((pairDirectionTransitions.Length - 1) * 0.5f);
            return perpendicular * (centeredLane * TransitionLaneSpacing);
        }

        private float GetSelfTransitionLoopOffset(FsmGraphTransition transition)
        {
            var selfTransitions = _graph.Transitions
                .Where(candidate =>
                    candidate != null &&
                    candidate.FromStateId == transition.FromStateId &&
                    candidate.ToStateId == transition.ToStateId)
                .OrderBy(candidate => candidate.Id)
                .ToArray();

            var transitionIndex = Array.FindIndex(selfTransitions, candidate => candidate.Id == transition.Id);
            if (transitionIndex <= 0)
                return 0f;

            return transitionIndex * TransitionLaneSpacing;
        }

        private static Vector2 GetRectEdgePoint(Rect rect, Vector2 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return rect.center;

            direction.Normalize();

            var halfWidth = rect.width * 0.5f;
            var halfHeight = rect.height * 0.5f;
            var scaleX = Mathf.Approximately(direction.x, 0f) ? float.PositiveInfinity : halfWidth / Mathf.Abs(direction.x);
            var scaleY = Mathf.Approximately(direction.y, 0f) ? float.PositiveInfinity : halfHeight / Mathf.Abs(direction.y);
            var scale = Mathf.Min(scaleX, scaleY);

            return rect.center + direction * scale;
        }

        private static Vector2 GetRectEdgePoint(Rect rect, Vector2 linePoint, Vector2 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return rect.center;

            direction.Normalize();

            var hasIntersection = false;
            var bestForwardT = float.NegativeInfinity;
            var bestFallbackT = float.NegativeInfinity;
            var bestForwardPoint = rect.center;
            var bestFallbackPoint = rect.center;

            TryRectEdgeIntersection(rect, linePoint, direction, rect.xMin, true, ref hasIntersection, ref bestForwardT, ref bestForwardPoint, ref bestFallbackT, ref bestFallbackPoint);
            TryRectEdgeIntersection(rect, linePoint, direction, rect.xMax, true, ref hasIntersection, ref bestForwardT, ref bestForwardPoint, ref bestFallbackT, ref bestFallbackPoint);
            TryRectEdgeIntersection(rect, linePoint, direction, rect.yMin, false, ref hasIntersection, ref bestForwardT, ref bestForwardPoint, ref bestFallbackT, ref bestFallbackPoint);
            TryRectEdgeIntersection(rect, linePoint, direction, rect.yMax, false, ref hasIntersection, ref bestForwardT, ref bestForwardPoint, ref bestFallbackT, ref bestFallbackPoint);

            if (bestForwardT > float.NegativeInfinity)
                return bestForwardPoint;

            if (hasIntersection)
                return bestFallbackPoint;

            return GetRectEdgePoint(rect, direction);
        }

        private static void TryRectEdgeIntersection(
            Rect rect,
            Vector2 linePoint,
            Vector2 direction,
            float edgeValue,
            bool verticalEdge,
            ref bool hasIntersection,
            ref float bestForwardT,
            ref Vector2 bestForwardPoint,
            ref float bestFallbackT,
            ref Vector2 bestFallbackPoint)
        {
            var axisDelta = verticalEdge ? direction.x : direction.y;
            if (Mathf.Approximately(axisDelta, 0f))
                return;

            var t = (edgeValue - (verticalEdge ? linePoint.x : linePoint.y)) / axisDelta;
            var point = linePoint + direction * t;

            var min = verticalEdge ? rect.yMin : rect.xMin;
            var max = verticalEdge ? rect.yMax : rect.xMax;
            var edgeCoordinate = verticalEdge ? point.y : point.x;
            if (edgeCoordinate < min - 0.01f || edgeCoordinate > max + 0.01f)
                return;

            hasIntersection = true;

            if (t >= 0f)
            {
                if (t > bestForwardT)
                {
                    bestForwardT = t;
                    bestForwardPoint = point;
                }

                return;
            }

            if (t > bestFallbackT)
            {
                bestFallbackT = t;
                bestFallbackPoint = point;
            }
        }

        private static float GetDistanceToPolyline(Vector2 point, IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count < 2)
                return float.PositiveInfinity;

            var closestDistance = float.PositiveInfinity;

            for (var i = 0; i < points.Count - 1; i++)
            {
                var distance = DistancePointToSegment(point, points[i], points[i + 1]);
                if (distance < closestDistance)
                    closestDistance = distance;
            }

            return closestDistance;
        }

        private static void DrawTransitionPolyline(IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count < 2)
                return;

            for (var i = 0; i < points.Count - 1; i++)
                Handles.DrawAAPolyLine(3f, points[i], points[i + 1]);
        }

        private static float DistancePointToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
                return Vector2.Distance(point, start);

            var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            var projection = start + segment * t;
            return Vector2.Distance(point, projection);
        }

        private static void DrawTransitionArrow(Vector2 arrowTip, Vector2 arrowDirection)
        {
            if (arrowDirection.sqrMagnitude <= Mathf.Epsilon)
                arrowDirection = Vector2.right;

            arrowDirection.Normalize();

            var perpendicular = new Vector2(-arrowDirection.y, arrowDirection.x);
            var arrowBase = arrowTip - arrowDirection * TransitionArrowLength;
            var leftPoint = arrowBase + perpendicular * (TransitionArrowWidth * 0.5f);
            var rightPoint = arrowBase - perpendicular * (TransitionArrowWidth * 0.5f);

            Handles.DrawAAConvexPolygon(arrowTip, leftPoint, rightPoint);
        }

        private readonly struct TransitionVisualData
        {
            public TransitionVisualData(
                Vector2[] points,
                Vector2 arrowTip,
                Vector2 arrowDirection)
            {
                Points = points;
                ArrowTip = arrowTip;
                ArrowDirection = arrowDirection;
            }

            public Vector2[] Points { get; }
            public Vector2 ArrowTip { get; }
            public Vector2 ArrowDirection { get; }
        }

        private Vector2 ScreenToCanvasPosition(Vector2 mousePosition)
        {
            var canvasOrigin = new Vector2(0f, ToolbarHeight);
            return mousePosition - canvasOrigin - _canvasPan;
        }

        private static bool TryGetSelectionContext(out FsmGraphAsset graph, out FsmGraphRunner runner)
        {
            graph = null;
            runner = null;

            if (Selection.activeObject is FsmGraphAsset selectedGraph)
            {
                graph = selectedGraph;
                return true;
            }

            if (Selection.activeObject is FsmGraphRunner selectedRunner && selectedRunner.Graph != null)
            {
                graph = selectedRunner.Graph;
                runner = selectedRunner;
                return true;
            }

            if (Selection.activeGameObject != null &&
                Selection.activeGameObject.TryGetComponent<FsmGraphRunner>(out var selectedGameObjectRunner) &&
                selectedGameObjectRunner.Graph != null)
            {
                graph = selectedGameObjectRunner.Graph;
                runner = selectedGameObjectRunner;
                return true;
            }

            if (Selection.activeObject is Component component &&
                component.TryGetComponent<FsmGraphRunner>(out var selectedComponentRunner) &&
                selectedComponentRunner.Graph != null)
            {
                graph = selectedComponentRunner.Graph;
                runner = selectedComponentRunner;
                return true;
            }

            return false;
        }

        private void SetSelectionContext(FsmGraphAsset graph, FsmGraphRunner runner)
        {
            _graph = graph;
            _runner = runner;

            if (_graph != null && _graph.EnsureBlackboardParameterMetadata())
                EditorUtility.SetDirty(_graph);

            _selectedStateId = null;
            _selectedTransitionId = null;
            _pendingTransitionFromStateId = null;
            ClearEmbeddedEditor();
        }

        private bool HasRuntimeRunner =>
            Application.isPlaying &&
            _runner != null &&
            _runner.Graph == _graph &&
            _runner.IsInitialized &&
            _runner.RuntimeContext != null;

        private void HandleUndoRedoPerformed()
        {
            if (_graph != null && _graph.EnsureBlackboardParameterMetadata())
                EditorUtility.SetDirty(_graph);

            if (_graph == null || _graph.FindState(_selectedStateId) == null)
                _selectedStateId = null;

            if (_graph == null || _graph.FindTransition(_selectedTransitionId) == null)
                _selectedTransitionId = null;

            if (_graph == null || _graph.FindState(_pendingTransitionFromStateId) == null)
                _pendingTransitionFromStateId = null;

            _draggedStateId = null;
            _isPanning = false;
            ClearEmbeddedEditor();
            Repaint();
        }

        private static bool HasActionModifier(Event currentEvent) =>
            currentEvent != null && (currentEvent.control || currentEvent.command);

        private string GetToolbarTitle()
        {
            if (_graph == null)
                return "Select an FSM graph asset or a scene object with FsmGraphRunner";

            if (_runner == null)
                return $"Graph: {_graph.name}";

            return HasRuntimeRunner
                ? $"Graph: {_graph.name} | Runner: {_runner.name} | State: {GetRuntimeCurrentStateId()}"
                : $"Graph: {_graph.name} | Runner: {_runner.name}";
        }

        private string GetRuntimeCurrentStateId() =>
            HasRuntimeRunner ? _runner.CurrentStateId : string.Empty;

        private bool GetRuntimeBoolValue(FsmBlackboardParameterDefinition parameter)
        {
            if (!HasRuntimeRunner || parameter == null)
                return parameter != null && parameter.BoolValue;

            return _runner.RuntimeContext.Parameters.TryGetBool(parameter.Key, out var value)
                ? value
                : parameter.BoolValue;
        }

        private int GetRuntimeIntValue(FsmBlackboardParameterDefinition parameter)
        {
            if (!HasRuntimeRunner || parameter == null)
                return parameter != null ? parameter.IntValue : default;

            return _runner.RuntimeContext.Parameters.TryGetInt(parameter.Key, out var value)
                ? value
                : parameter.IntValue;
        }

        private float GetRuntimeFloatValue(FsmBlackboardParameterDefinition parameter)
        {
            if (!HasRuntimeRunner || parameter == null)
                return parameter != null ? parameter.FloatValue : default;

            return _runner.RuntimeContext.Parameters.TryGetFloat(parameter.Key, out var value)
                ? value
                : parameter.FloatValue;
        }

        private void RecordGraph(string actionName)
        {
            if (_graph != null)
                Undo.RecordObject(_graph, actionName);
        }

        private void MarkDirty()
        {
            if (_graph == null)
                return;

            EditorUtility.SetDirty(_graph);
            GUI.changed = true;
        }

        private void ClearEmbeddedEditor()
        {
            if (_embeddedEditor != null)
                DestroyImmediate(_embeddedEditor);

            _embeddedEditor = null;
            _embeddedEditorTarget = null;
        }

        private T CreateSubAsset<T>(Type type, string assetName) where T : ScriptableObject
        {
            var created = ScriptableObject.CreateInstance(type) as T;
            created.name = assetName;
            created.hideFlags = HideFlags.HideInHierarchy;

            AssetDatabase.AddObjectToAsset(created, _graph);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_graph));
            Undo.RegisterCreatedObjectUndo(created, $"Create {type.Name}");

            return created;
        }

        private void RemoveOwnedSubAsset(UnityEngine.Object subAsset)
        {
            if (subAsset == null)
                return;

            if (AssetDatabase.GetAssetPath(subAsset) != AssetDatabase.GetAssetPath(_graph))
                return;

            Undo.DestroyObjectImmediate(subAsset);
        }

        private static IEnumerable<Type> GetCreatableTypes<T>() where T : ScriptableObject =>
            TypeCache.GetTypesDerivedFrom<T>()
                .Where(type => !type.IsAbstract && !type.IsGenericType && !type.IsInterface)
                .OrderBy(type => type.Name);
    }
}
