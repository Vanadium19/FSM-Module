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
        private const float NodeWidth = 220f;
        private const float NodeHeight = 84f;

        private static readonly Color CanvasBackground = new(0.15f, 0.15f, 0.15f);
        private static readonly Color GridPrimary = new(1f, 1f, 1f, 0.04f);
        private static readonly Color GridSecondary = new(1f, 1f, 1f, 0.08f);
        private static readonly Color StateColor = new(0.24f, 0.26f, 0.29f);
        private static readonly Color SelectedStateColor = new(0.83f, 0.55f, 0.21f);
        private static readonly Color TransitionColor = new(0.73f, 0.73f, 0.73f);
        private static readonly Color SelectedTransitionColor = new(0.95f, 0.72f, 0.18f);
        private static readonly Color PendingTransitionColor = new(0.44f, 0.80f, 0.46f);
        private const float TransitionTangentLength = 60f;
        private const float TransitionLaneSpacing = 28f;
        private const float TransitionSelectionDistance = 12f;

        private FsmGraphAsset _graph;
        private Vector2 _canvasPan = new(120f, 120f);
        private Vector2 _inspectorScroll;

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
            window.SetGraph(graph);
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
            if (_graph == null && Selection.activeObject is FsmGraphAsset graph)
                _graph = graph;
        }

        private void OnDisable()
        {
            if (_embeddedEditor != null)
                DestroyImmediate(_embeddedEditor);
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is FsmGraphAsset graph)
            {
                SetGraph(graph);
                Repaint();
            }
        }

        private void OnGUI()
        {
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

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(ToolbarHeight)))
            {
                var updatedGraph = (FsmGraphAsset)EditorGUILayout.ObjectField(
                    _graph,
                    typeof(FsmGraphAsset),
                    false,
                    GUILayout.Width(260f));

                if (updatedGraph != _graph)
                    SetGraph(updatedGraph);

                using (new EditorGUI.DisabledScope(_graph == null))
                {
                    if (GUILayout.Button("New State", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                        AddState(GetCanvasCenterPosition());

                    if (GUILayout.Button("Add Parameter", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                        AddBlackboardParameter();

                    if (GUILayout.Button("Add Binding", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                        AddBindingDefinition();

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
                    "Create or select an FsmGraphAsset to start building a state graph.",
                    MessageType.Info);

                var selectedGraph = (FsmGraphAsset)EditorGUILayout.ObjectField(
                    "Graph Asset",
                    _graph,
                    typeof(FsmGraphAsset),
                    false);

                if (selectedGraph != _graph)
                    SetGraph(selectedGraph);
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

            EditorGUI.DrawRect(nodeRect, isSelected ? SelectedStateColor : StateColor);
            GUI.Box(nodeRect, GUIContent.none);

            var nameRect = new Rect(nodeRect.x + 10f, nodeRect.y + 8f, nodeRect.width - 20f, 20f);
            var typeRect = new Rect(nodeRect.x + 10f, nodeRect.y + 34f, nodeRect.width - 20f, 18f);
            var idRect = new Rect(nodeRect.x + 10f, nodeRect.y + 56f, nodeRect.width - 20f, 16f);

            EditorGUI.LabelField(nameRect, state.Name, EditorStyles.boldLabel);
            EditorGUI.LabelField(
                typeRect,
                state.State != null ? state.State.GetType().Name : "No State Behaviour",
                EditorStyles.miniLabel);
            EditorGUI.LabelField(idRect, state.Id, EditorStyles.centeredGreyMiniLabel);
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
            Handles.DrawBezier(visual.Start, visual.End, visual.StartTangent, visual.EndTangent, color, null, 3f);
            Handles.color = color;
            Handles.ArrowHandleCap(0, visual.End, Quaternion.LookRotation(Vector3.forward, visual.End - visual.EndTangent), 10f, EventType.Repaint);
            Handles.color = Color.white;
            Handles.EndGUI();

            var label = transition.Transition != null ? transition.Transition.GetType().Name : "Transition";

            if (GUI.Button(visual.LabelRect, label, EditorStyles.miniButton))
            {
                _selectedTransitionId = transition.Id;
                _selectedStateId = null;
                GUI.changed = true;
            }
        }

        private void DrawPendingTransition(Rect canvasRect, string fromStateId, Vector2 mousePosition)
        {
            var fromState = _graph.FindState(fromStateId);
            if (fromState == null)
                return;

            var startRect = GetNodeRect(canvasRect, fromState);
            var start = startRect.center + new Vector2(NodeWidth * 0.5f, 0f);
            var end = mousePosition;
            var startTangent = start + Vector2.right * 60f;
            var endTangent = end + Vector2.left * 60f;

            Handles.BeginGUI();
            Handles.DrawBezier(start, end, startTangent, endTangent, PendingTransitionColor, null, 3f);
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
            DrawBindingsSection();
            EditorGUILayout.Space();
            DrawSelectionSection();

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawGraphSection()
        {
            EditorGUILayout.LabelField("Graph", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Asset", _graph, typeof(FsmGraphAsset), false);

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
            EditorGUILayout.LabelField("Blackboard Parameters", EditorStyles.boldLabel);

            if (_graph.BlackboardParameters.Count == 0)
                EditorGUILayout.HelpBox("No blackboard parameters yet.", MessageType.None);

            for (var i = 0; i < _graph.BlackboardParameters.Count; i++)
            {
                var parameter = _graph.BlackboardParameters[i];
                if (parameter == null)
                    continue;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var updatedKey = parameter.Key;
                    var updatedType = parameter.Type;
                    var updatedBoolValue = parameter.BoolValue;
                    var updatedIntValue = parameter.IntValue;
                    var updatedFloatValue = parameter.FloatValue;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        updatedKey = EditorGUILayout.TextField("Key", parameter.Key);
                        if (GUILayout.Button("X", GUILayout.Width(24f)))
                        {
                            RecordGraph("Remove Blackboard Parameter");
                            _graph.BlackboardParameters.RemoveAt(i);
                            MarkDirty();
                            return;
                        }
                    }

                    updatedType = (BlackboardParameterType)EditorGUILayout.EnumPopup("Type", parameter.Type);

                    switch (updatedType)
                    {
                        case BlackboardParameterType.Bool:
                            updatedBoolValue = EditorGUILayout.Toggle("Default", parameter.BoolValue);
                            break;
                        case BlackboardParameterType.Int:
                            updatedIntValue = EditorGUILayout.IntField("Default", parameter.IntValue);
                            break;
                        case BlackboardParameterType.Float:
                            updatedFloatValue = EditorGUILayout.FloatField("Default", parameter.FloatValue);
                            break;
                    }

                    if (updatedKey != parameter.Key ||
                        updatedType != parameter.Type ||
                        updatedBoolValue != parameter.BoolValue ||
                        updatedIntValue != parameter.IntValue ||
                        !Mathf.Approximately(updatedFloatValue, parameter.FloatValue))
                    {
                        RecordGraph("Edit Blackboard Parameter");
                        parameter.Key = updatedKey;
                        parameter.Type = updatedType;
                        parameter.BoolValue = updatedBoolValue;
                        parameter.IntValue = updatedIntValue;
                        parameter.FloatValue = updatedFloatValue;
                        MarkDirty();
                    }
                }
            }

            if (GUILayout.Button("Add Blackboard Parameter"))
                AddBlackboardParameter();
        }

        private void DrawBindingsSection()
        {
            EditorGUILayout.LabelField("Binding Slots", EditorStyles.boldLabel);

            if (_graph.BindingDefinitions.Count == 0)
                EditorGUILayout.HelpBox("No binding slots yet.", MessageType.None);

            for (var i = 0; i < _graph.BindingDefinitions.Count; i++)
            {
                var binding = _graph.BindingDefinitions[i];
                if (binding == null)
                    continue;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var updatedKey = binding.Key;
                    var updatedDescription = binding.Description;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        updatedKey = EditorGUILayout.TextField("Key", binding.Key);
                        if (GUILayout.Button("X", GUILayout.Width(24f)))
                        {
                            RecordGraph("Remove Binding");
                            _graph.BindingDefinitions.RemoveAt(i);
                            MarkDirty();
                            return;
                        }
                    }

                    updatedDescription = EditorGUILayout.TextField("Description", binding.Description);

                    if (updatedKey != binding.Key || updatedDescription != binding.Description)
                    {
                        RecordGraph("Edit Binding Slot");
                        binding.Key = updatedKey;
                        binding.Description = updatedDescription;
                        MarkDirty();
                    }
                }
            }

            if (GUILayout.Button("Add Binding Slot"))
                AddBindingDefinition();
        }

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

            EditorGUILayout.SelectableLabel(state.Id, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

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

            EditorGUILayout.SelectableLabel(transition.Id, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

            EditorGUI.BeginChangeCheck();
            var updatedTransition = (FsmTransitionBehaviour)EditorGUILayout.ObjectField(
                "Behaviour",
                transition.Transition,
                typeof(FsmTransitionBehaviour),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                RecordGraph("Assign Transition Behaviour");
                transition.Transition = updatedTransition;
                ClearEmbeddedEditor();
                MarkDirty();
            }

            if (GUILayout.Button("Create Behaviour"))
                ShowCreateTransitionMenu(transition);

            if (transition.Transition != null)
                DrawEmbeddedEditor(transition.Transition);
            else
                EditorGUILayout.HelpBox("Choose or create a transition behaviour. Users only need to inherit from FsmTransitionBehaviour.", MessageType.Info);

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

        private void HandleCanvasEvents(Rect canvasRect, Event currentEvent)
        {
            if (!canvasRect.Contains(currentEvent.mousePosition))
                return;

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
            menu.AddItem(new GUIContent("Create State Behaviour"), false, () => ShowCreateStateMenu(state));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Delete State"), false, () => DeleteState(state));
            menu.ShowAsContext();
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

        private void ShowCreateTransitionMenu(FsmGraphTransition transition)
        {
            var menu = new GenericMenu();
            var types = GetCreatableTypes<FsmTransitionBehaviour>().ToArray();

            if (types.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No FsmTransitionBehaviour types found"));
                menu.ShowAsContext();
                return;
            }

            foreach (var type in types)
                menu.AddItem(new GUIContent(type.Name), false, () => AssignNewTransitionBehaviour(transition, type));

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

        private void AssignNewTransitionBehaviour(FsmGraphTransition transition, Type type)
        {
            RecordGraph("Create Transition Behaviour");
            RemoveOwnedSubAsset(transition.Transition);

            var behaviour = CreateSubAsset<FsmTransitionBehaviour>(type, type.Name);
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
            _graph.Transitions.Add(transition);
            _selectedTransitionId = transition.Id;
            _selectedStateId = null;
            MarkDirty();
        }

        private void AddBlackboardParameter()
        {
            RecordGraph("Add Blackboard Parameter");
            _graph.BlackboardParameters.Add(new FsmBlackboardParameterDefinition());
            MarkDirty();
        }

        private void AddBindingDefinition()
        {
            RecordGraph("Add Binding Slot");
            _graph.BindingDefinitions.Add(new FsmBindingDefinition());
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
                if (visual.LabelRect.Contains(mousePosition))
                    return transition;

                var distance = HandleUtility.DistancePointBezier(
                    mousePosition,
                    visual.Start,
                    visual.End,
                    visual.StartTangent,
                    visual.EndTangent);

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
                var start = new Vector2(fromRect.xMax - 26f, fromRect.center.y - 12f);
                var end = new Vector2(fromRect.xMax - 26f, fromRect.center.y + 12f);
                var startTangent = start + new Vector2(80f, -60f);
                var endTangent = end + new Vector2(80f, 60f);
                var labelCenter = EvaluateBezier(start, end, startTangent, endTangent, 0.5f);

                return new TransitionVisualData(
                    start,
                    end,
                    startTangent,
                    endTangent,
                    new Rect(labelCenter.x - 60f, labelCenter.y - 12f, 120f, 24f));
            }

            var startPoint = fromRect.center + new Vector2(NodeWidth * 0.5f, 0f);
            var endPoint = toRect.center - new Vector2(NodeWidth * 0.5f, 0f);
            var offset = GetTransitionCurveOffset(transition);

            var startTangentPoint = startPoint + Vector2.right * TransitionTangentLength + offset;
            var endTangentPoint = endPoint + Vector2.left * TransitionTangentLength + offset;
            var labelCenterPoint = EvaluateBezier(startPoint, endPoint, startTangentPoint, endTangentPoint, 0.5f);

            return new TransitionVisualData(
                startPoint,
                endPoint,
                startTangentPoint,
                endTangentPoint,
                new Rect(labelCenterPoint.x - 60f, labelCenterPoint.y - 12f, 120f, 24f));
        }

        private Vector2 GetTransitionCurveOffset(FsmGraphTransition transition)
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

        private static Vector2 EvaluateBezier(Vector2 start, Vector2 end, Vector2 startTangent, Vector2 endTangent, float t)
        {
            var oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * oneMinusT * start +
                3f * oneMinusT * oneMinusT * t * startTangent +
                3f * oneMinusT * t * t * endTangent +
                t * t * t * end;
        }

        private readonly struct TransitionVisualData
        {
            public TransitionVisualData(Vector2 start, Vector2 end, Vector2 startTangent, Vector2 endTangent, Rect labelRect)
            {
                Start = start;
                End = end;
                StartTangent = startTangent;
                EndTangent = endTangent;
                LabelRect = labelRect;
            }

            public Vector2 Start { get; }
            public Vector2 End { get; }
            public Vector2 StartTangent { get; }
            public Vector2 EndTangent { get; }
            public Rect LabelRect { get; }
        }

        private Vector2 ScreenToCanvasPosition(Vector2 mousePosition)
        {
            var canvasOrigin = new Vector2(0f, ToolbarHeight);
            return mousePosition - canvasOrigin - _canvasPan;
        }

        private void SetGraph(FsmGraphAsset graph)
        {
            _graph = graph;
            _selectedStateId = null;
            _selectedTransitionId = null;
            _pendingTransitionFromStateId = null;
            ClearEmbeddedEditor();
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
