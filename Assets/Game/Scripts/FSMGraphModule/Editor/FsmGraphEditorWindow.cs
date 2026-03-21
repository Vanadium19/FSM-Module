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
        private const float DefaultInspectorWidth = 240f;
        private const float MinInspectorWidth = 240f;
        private const float MaxInspectorWidth = 420f;
        private const float MinCanvasWidth = 320f;
        private const float InspectorSplitterWidth = 2f;
        private const string InspectorWidthPrefsKey = "FSMModule.Graph.Editor.InspectorWidth";
        private const float NodeWidth = 248f;
        private const float NodeHeight = 108f;
        private const float NodeHeaderHeight = 38f;
        private const float NodeFooterHeight = 28f;
        private const float NodeAccentWidth = 4f;
        private const float NodeShadowOffsetX = 2f;
        private const float NodeShadowOffsetY = 8f;
        private const float NodeBadgeHeight = 18f;

        private static readonly Color CanvasBackground = new(0.10f, 0.115f, 0.14f);
        private static readonly Color GridDotColor = new(0.88f, 0.93f, 1f, 0.035f);
        private static readonly Color GridMajorDotColor = new(0.88f, 0.95f, 1f, 0.07f);
        private static readonly Color NodeCardColor = new(0.16f, 0.185f, 0.225f);
        private static readonly Color NodeCardHeaderColor = new(0.19f, 0.215f, 0.26f);
        private static readonly Color NodeCardHoverColor = new(0.19f, 0.215f, 0.255f);
        private static readonly Color NodeCardHoverHeaderColor = new(0.22f, 0.245f, 0.29f);
        private static readonly Color NodeBorderColor = new(0.64f, 0.72f, 0.84f, 0.18f);
        private static readonly Color NodeDividerColor = new(0.72f, 0.80f, 0.90f, 0.12f);
        private static readonly Color NodeShadowColor = new(0f, 0f, 0f, 0.20f);
        private static readonly Color NodeAccentColor = new(0.45f, 0.57f, 0.74f);
        private static readonly Color InitialAccentColor = new(0.93f, 0.56f, 0.30f);
        private static readonly Color RuntimeActiveColor = new(0.36f, 0.80f, 0.53f);
        private static readonly Color SelectedStateOutlineColor = new(0.26f, 0.72f, 1f, 1f);
        private static readonly Color WarningColor = new(0.93f, 0.72f, 0.24f);
        private static readonly Color InvalidColor = new(0.88f, 0.33f, 0.34f);
        private static readonly Color TransitionColor = new(0.82f, 0.87f, 0.95f, 0.32f);
        private static readonly Color HoveredTransitionColor = new(0.86f, 0.91f, 0.97f, 0.55f);
        private static readonly Color SelectedTransitionColor = new(0.33f, 0.77f, 1f, 0.96f);
        private static readonly Color PendingTransitionColor = new(0.30f, 0.84f, 1f, 0.88f);
        private static readonly Color NodeTitleColor = new(0.96f, 0.97f, 0.99f);
        private static readonly Color NodeSubtitleColor = new(0.79f, 0.83f, 0.90f);
        private static readonly Color NodeFooterColor = new(0.69f, 0.75f, 0.84f);
        private static readonly Color InspectorBackgroundColor = new(0.115f, 0.13f, 0.16f);
        private static readonly Color InspectorDividerColor = new(0.52f, 0.61f, 0.74f, 0.10f);
        private static readonly Color InspectorCardColor = new(0.16f, 0.185f, 0.225f);
        private static readonly Color InspectorCardBorderColor = new(0.56f, 0.65f, 0.77f, 0.12f);
        private static readonly Color InspectorSectionTitleColor = new(0.95f, 0.97f, 0.99f);
        private static readonly Color InspectorSectionSubtitleColor = new(0.68f, 0.75f, 0.85f);
        private static readonly Color InspectorMetaLabelColor = new(0.57f, 0.65f, 0.76f);
        private static readonly Color InspectorMetaValueColor = new(0.89f, 0.92f, 0.96f);
        private static readonly Color InspectorInfoBackgroundColor = new(0.13f, 0.155f, 0.19f);
        private static readonly Color InspectorInfoBorderColor = new(0.38f, 0.52f, 0.70f, 0.16f);
        private static readonly Color InspectorControlTint = new(0.68f, 0.75f, 0.85f);
        private static readonly Color InspectorControlTintStrong = new(0.68f, 0.75f, 0.85f);
        private static readonly Color InspectorControlDangerTint = new(0.43f, 0.21f, 0.24f);
        private static readonly Color InspectorButtonColor = new(0.21f, 0.25f, 0.31f);
        private static readonly Color InspectorButtonHoverColor = new(0.25f, 0.30f, 0.37f);
        private static readonly Color InspectorButtonActiveColor = new(0.18f, 0.22f, 0.28f);
        private static readonly Color InspectorButtonBorderColor = new(0.56f, 0.68f, 0.83f, 0.18f);
        private static readonly Color InspectorDangerButtonColor = new(0.31f, 0.22f, 0.26f);
        private static readonly Color InspectorDangerButtonHoverColor = new(0.38f, 0.25f, 0.31f);
        private static readonly Color InspectorDangerButtonActiveColor = new(0.26f, 0.18f, 0.22f);
        private static readonly Color InspectorButtonTextColor = new(0.94f, 0.96f, 0.99f);
        private const float TransitionLaneSpacing = 28f;
        private const float TransitionSelectionDistance = 10f;
        private const float TransitionArrowLength = 11f;
        private const float TransitionArrowWidth = 8f;
        private const float SelfTransitionLoopWidth = 84f;
        private const float SelfTransitionLoopHeight = 30f;
        private const float TransitionLabelPaddingX = 10f;
        private const float TransitionLabelPaddingY = 5f;
        private const float TransitionLineWidth = 2.5f;
        private const float TransitionSelectedLineWidth = 3.5f;
        private const float SelectedStateOutlineThickness = 2f;
        private const float StateCornerRadius = 13f;
        private const int TransitionCurveSampleCount = 24;

        private FsmGraphAsset _graph;
        private FsmGraphRunner _runner;
        private float _inspectorWidth = DefaultInspectorWidth;
        private Vector2 _canvasPan = new(120f, 120f);
        private Vector2 _inspectorScroll;
        private string _blackboardSearch = string.Empty;

        private string _selectedStateId;
        private string _selectedTransitionId;
        private string _pendingTransitionFromStateId;
        private string _hoveredStateId;
        private string _hoveredTransitionId;

        private string _draggedStateId;
        private Vector2 _dragOffset;
        private bool _isPanning;
        private bool _isResizingInspector;
        private Vector2 _panMouseStart;
        private Vector2 _panStart;

        private UnityEditor.Editor _embeddedEditor;
        private UnityEngine.Object _embeddedEditorTarget;
        private GUIStyle _nodeTitleStyle;
        private GUIStyle _nodeSubtitleStyle;
        private GUIStyle _nodeFooterStyle;
        private GUIStyle _nodeBadgeStyle;
        private GUIStyle _transitionLabelStyle;
        private GUIStyle _inspectorSectionStyle;
        private GUIStyle _inspectorSectionTitleStyle;
        private GUIStyle _inspectorSectionSubtitleStyle;
        private GUIStyle _inspectorMetaLabelStyle;
        private GUIStyle _inspectorMetaValueStyle;
        private GUIStyle _inspectorEmptyStateStyle;
        private GUIStyle _inspectorInfoTextStyle;
        private GUIStyle _inspectorButtonStyle;
        private GUIStyle _inspectorDangerButtonStyle;
        private GUIStyle _inspectorMiniButtonStyle;
        private Texture2D _inspectorSectionTexture;
        private Texture2D _inspectorInfoTexture;
        private Texture2D _inspectorButtonTexture;
        private Texture2D _inspectorButtonHoverTexture;
        private Texture2D _inspectorButtonActiveTexture;
        private Texture2D _inspectorDangerButtonTexture;
        private Texture2D _inspectorDangerButtonHoverTexture;
        private Texture2D _inspectorDangerButtonActiveTexture;

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
            _inspectorWidth = EditorPrefs.GetFloat(InspectorWidthPrefsKey, DefaultInspectorWidth);
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

            DestroyInspectorTexture(ref _inspectorSectionTexture);
            DestroyInspectorTexture(ref _inspectorInfoTexture);
            DestroyInspectorTexture(ref _inspectorButtonTexture);
            DestroyInspectorTexture(ref _inspectorButtonHoverTexture);
            DestroyInspectorTexture(ref _inspectorButtonActiveTexture);
            DestroyInspectorTexture(ref _inspectorDangerButtonTexture);
            DestroyInspectorTexture(ref _inspectorDangerButtonHoverTexture);
            DestroyInspectorTexture(ref _inspectorDangerButtonActiveTexture);
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
            EnsureStyles();
            HandleKeyboardShortcuts(Event.current);
            DrawToolbar();

            if (_graph == null)
            {
                DrawEmptyState();
                return;
            }

            var inspectorWidth = GetInspectorWidth();
            var splitterRect = new Rect(
                position.width - inspectorWidth - InspectorSplitterWidth,
                ToolbarHeight,
                InspectorSplitterWidth,
                position.height - ToolbarHeight);

            HandleInspectorResize(splitterRect, Event.current);
            inspectorWidth = GetInspectorWidth();
            splitterRect.x = position.width - inspectorWidth - InspectorSplitterWidth;

            var canvasRect = new Rect(0f, ToolbarHeight, splitterRect.x, position.height - ToolbarHeight);
            var inspectorRect = new Rect(splitterRect.xMax, ToolbarHeight, inspectorWidth, position.height - ToolbarHeight);

            DrawCanvas(canvasRect);
            DrawInspectorSplitter(splitterRect);
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
            UpdateHoverState(canvasRect, Event.current);
            DrawDottedGrid(canvasRect, 24f, GridDotColor, 1.4f);
            DrawDottedGrid(canvasRect, 96f, GridMajorDotColor, 2.1f);

            HandleCanvasEvents(canvasRect, Event.current);

            foreach (var transition in _graph.Transitions)
                DrawTransition(canvasRect, transition);

            foreach (var state in _graph.States)
                DrawStateNode(canvasRect, state);

            if (!string.IsNullOrWhiteSpace(_pendingTransitionFromStateId))
                DrawPendingTransition(canvasRect, _pendingTransitionFromStateId, Event.current.mousePosition);

            if (_graph.FindTransition(_selectedTransitionId) is { } selectedTransition)
                DrawSelectedTransitionOverlay(canvasRect, selectedTransition);
        }

        private void DrawDottedGrid(Rect canvasRect, float spacing, Color color, float dotSize)
        {
            var startX = canvasRect.x + (_canvasPan.x % spacing);
            var startY = canvasRect.y + (_canvasPan.y % spacing);
            var halfDot = dotSize * 0.5f;

            for (var x = startX; x < canvasRect.xMax; x += spacing)
            {
                for (var y = startY; y < canvasRect.yMax; y += spacing)
                {
                    EditorGUI.DrawRect(new Rect(x - halfDot, y - halfDot, dotSize, dotSize), color);
                }
            }
        }

        private void DrawStateNode(Rect canvasRect, FsmGraphStateNode state)
        {
            if (state == null)
                return;

            var nodeRect = GetNodeRect(canvasRect, state);
            var isSelected = state.Id == _selectedStateId;
            var isHovered = state.Id == _hoveredStateId;
            var isInitial = state.Id == _graph.InitialStateId;
            var isCurrent = state.Id == GetRuntimeCurrentStateId();
            var outgoingTransitionCount = GetOutgoingTransitionCount(state.Id);
            var isInvalid = state.State == null;
            var isWarning = !isInvalid && outgoingTransitionCount == 0;
            var cardHeaderColor = isHovered ? NodeCardHoverHeaderColor : NodeCardHeaderColor;
            var cardBodyColor = isHovered ? NodeCardHoverColor : NodeCardColor;
            var accentColor = isInvalid
                ? InvalidColor
                : isCurrent
                    ? RuntimeActiveColor
                    : isInitial
                        ? InitialAccentColor
                        : NodeAccentColor;
            var borderColor = isInvalid
                ? new Color(InvalidColor.r, InvalidColor.g, InvalidColor.b, 0.7f)
                : isWarning
                    ? new Color(WarningColor.r, WarningColor.g, WarningColor.b, 0.34f)
                    : new Color(NodeBorderColor.r, NodeBorderColor.g, NodeBorderColor.b, isHovered ? 0.30f : NodeBorderColor.a);

            DrawStateShadow(nodeRect);

            DrawRoundedRect(nodeRect, cardBodyColor, borderColor, StateCornerRadius);

            var headerRect = new Rect(nodeRect.x + 1f, nodeRect.y + 1f, nodeRect.width - 2f, NodeHeaderHeight + 10f);
            DrawRoundedRect(headerRect, cardHeaderColor, Color.clear, StateCornerRadius - 1f);
            EditorGUI.DrawRect(
                new Rect(nodeRect.x + 1f, nodeRect.y + NodeHeaderHeight - 1f, nodeRect.width - 2f, 1f),
                NodeDividerColor);

            var accentRect = new Rect(nodeRect.x + 11f, nodeRect.y + 10f, NodeAccentWidth, nodeRect.height - 20f);
            DrawRoundedRect(accentRect, accentColor, Color.clear, NodeAccentWidth * 0.5f);

            if (isSelected)
                DrawStateSelectionOutline(nodeRect);

            var titleRect = new Rect(nodeRect.x + 26f, nodeRect.y + 9f, nodeRect.width - 82f, 22f);
            var typeRect = new Rect(nodeRect.x + 26f, nodeRect.y + NodeHeaderHeight + 11f, nodeRect.width - 38f, 18f);
            var footerRect = new Rect(nodeRect.x + 26f, nodeRect.yMax - NodeFooterHeight + 4f, nodeRect.width - 38f, 16f);

            GUI.Label(titleRect, state.Name, _nodeTitleStyle);
            GUI.Label(
                typeRect,
                state.State != null ? state.State.GetType().Name : "No State Behaviour",
                _nodeSubtitleStyle);
            GUI.Label(
                footerRect,
                GetStateFooterText(outgoingTransitionCount),
                _nodeFooterStyle);

            if (TryGetStateBadge(state, outgoingTransitionCount, isInvalid, out var badgeText, out var badgeColor))
                DrawNodeBadge(nodeRect, badgeText, badgeColor);
        }

        private static void DrawStateSelectionOutline(Rect nodeRect)
        {
            Handles.BeginGUI();
            Handles.color = SelectedStateOutlineColor;
            DrawRoundedOutline(nodeRect, StateCornerRadius, SelectedStateOutlineThickness);
            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private static void DrawStateShadow(Rect nodeRect)
        {
            var shadowRect = new Rect(
                nodeRect.x + NodeShadowOffsetX,
                nodeRect.y + NodeShadowOffsetY,
                nodeRect.width,
                nodeRect.height);
            DrawRoundedRect(shadowRect, NodeShadowColor, Color.clear, StateCornerRadius + 1f);
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

        private void DrawNodeBadge(Rect nodeRect, string badgeText, Color badgeColor)
        {
            if (string.IsNullOrWhiteSpace(badgeText))
                return;

            var content = new GUIContent(badgeText);
            var textSize = _nodeBadgeStyle.CalcSize(content);
            var badgeWidth = Mathf.Max(46f, textSize.x + 14f);
            var badgeRect = new Rect(
                nodeRect.xMax - badgeWidth - 12f,
                nodeRect.y + 10f,
                badgeWidth,
                NodeBadgeHeight);

            DrawRoundedRect(badgeRect, badgeColor, Color.clear, 8f);
            GUI.Label(badgeRect, content, _nodeBadgeStyle);
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
            var isSelected = transition.Id == _selectedTransitionId;
            var isHovered = transition.Id == _hoveredTransitionId;
            var color = isSelected
                ? SelectedTransitionColor
                : isHovered
                    ? HoveredTransitionColor
                    : TransitionColor;
            var lineWidth = isSelected ? TransitionSelectedLineWidth : TransitionLineWidth;

            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawBezier(
                visual.StartPoint,
                visual.EndPoint,
                visual.StartTangent,
                visual.EndTangent,
                color,
                null,
                lineWidth);
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
            var direction = (mousePosition - start).normalized;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                direction = Vector2.right;

            var distance = Vector2.Distance(start, mousePosition);
            var tangentDistance = Mathf.Clamp(distance * 0.35f, 50f, 150f);
            var startTangent = start + direction * tangentDistance;
            var endTangent = mousePosition - direction * tangentDistance;

            Handles.BeginGUI();
            Handles.color = PendingTransitionColor;
            Handles.DrawBezier(start, mousePosition, startTangent, endTangent, PendingTransitionColor, null, TransitionSelectedLineWidth);
            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawSelectedTransitionOverlay(Rect canvasRect, FsmGraphTransition transition)
        {
            if (transition == null)
                return;

            var fromState = _graph.FindState(transition.FromStateId);
            var toState = _graph.FindState(transition.ToStateId);
            if (fromState == null || toState == null)
                return;

            var visual = GetTransitionVisualData(canvasRect, transition, fromState, toState);
            DrawSelectedTransitionLabel(visual.LabelPosition, GetTransitionCanvasLabel(transition));
        }

        private void DrawInspector(Rect inspectorRect)
        {
            EditorGUI.DrawRect(inspectorRect, InspectorBackgroundColor);
            EditorGUI.DrawRect(new Rect(inspectorRect.x, inspectorRect.y, 1f, inspectorRect.height), InspectorDividerColor);

            GUILayout.BeginArea(inspectorRect);
            _inspectorScroll = EditorGUILayout.BeginScrollView(new Vector2(0f, _inspectorScroll.y), GUIStyle.none, GUI.skin.verticalScrollbar);
            _inspectorScroll.x = 0f;

            GUILayout.Space(10f);
            DrawGraphSection();
            EditorGUILayout.Space(10f);
            DrawBlackboardSection();
            EditorGUILayout.Space(10f);
            DrawSelectionSection();
            GUILayout.Space(12f);

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawInspectorSection(string title, string subtitle, Action drawContent)
        {
            var sectionStyle = _inspectorSectionStyle ?? EditorStyles.helpBox;
            var titleStyle = _inspectorSectionTitleStyle ?? EditorStyles.boldLabel;
            var subtitleStyle = _inspectorSectionSubtitleStyle ?? EditorStyles.miniLabel;

            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                GUILayout.Label(title, titleStyle);

                if (!string.IsNullOrWhiteSpace(subtitle))
                {
                    GUILayout.Space(-1f);
                    GUILayout.Label(subtitle, subtitleStyle);
                }

                GUILayout.Space(8f);
                drawContent?.Invoke();
            }
        }

        private void DrawInspectorMetaRow(string label, string value)
        {
            var labelStyle = _inspectorMetaLabelStyle ?? EditorStyles.miniLabel;
            var valueStyle = _inspectorMetaValueStyle ?? EditorStyles.label;

            if (UseCompactInspectorLayout)
            {
                GUILayout.Label(label, labelStyle);
                GUILayout.Label(value, valueStyle);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, labelStyle, GUILayout.Width(72f));
                GUILayout.Label(value, valueStyle, GUILayout.ExpandWidth(true));
            }
        }

        private void DrawInspectorInfo(string message)
        {
            var infoContainerStyle = _inspectorEmptyStateStyle ?? EditorStyles.helpBox;
            var infoTextStyle = _inspectorInfoTextStyle ?? EditorStyles.wordWrappedMiniLabel;

            using (new EditorGUILayout.VerticalScope(infoContainerStyle))
            {
                GUILayout.Label(message, infoTextStyle);
            }
        }

        private void DrawGraphSection()
        {
            DrawInspectorSection(
                "Graph",
                _runner != null ? "Runtime binding" : "Asset context",
                () =>
                {
                    DrawInspectorMetaRow("Asset", _graph != null ? _graph.name : "<None>");

                    if (_runner != null)
                        DrawInspectorMetaRow("Runner", _runner.name);

                    if (_graph.States.Count == 0)
                    {
                        DrawInspectorInfo("Add at least one state to make the graph runnable.");
                        return;
                    }

                    var stateNames = _graph.States.Select(state => state.Name).ToArray();
                    var currentIndex = Mathf.Max(0, _graph.States.FindIndex(state => state.Id == _graph.InitialStateId));
                    int updatedIndex;
                    if (UseCompactInspectorLayout)
                    {
                        GUILayout.Label("Initial State", _inspectorMetaLabelStyle ?? EditorStyles.miniLabel);
                        using (new GuiBackgroundColorScope(InspectorControlTint))
                            updatedIndex = EditorGUILayout.Popup(currentIndex, stateNames);
                    }
                    else
                    {
                        using (new GuiBackgroundColorScope(InspectorControlTint))
                            updatedIndex = EditorGUILayout.Popup("Initial State", currentIndex, stateNames);
                    }

                    if (updatedIndex >= 0 && updatedIndex < _graph.States.Count && updatedIndex != currentIndex)
                    {
                        RecordGraph("Change Initial State");
                        _graph.InitialStateId = _graph.States[updatedIndex].Id;
                        MarkDirty();
                    }
                });
        }

        private void DrawBlackboardSection()
        {
            DrawInspectorSection(
                "Parameters",
                HasRuntimeRunner ? $"Runtime values from {_runner.name}" : "Transition parameter defaults",
                () =>
                {
                    if (HasRuntimeRunner)
                        DrawInspectorInfo($"Showing runtime values from '{_runner.name}'.");

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
                        DrawInspectorInfo("No parameters yet. Use + to add Float, Int or Bool.");
                    else if (visibleParameterCount == 0)
                        DrawInspectorInfo("No parameters match the current search.");
                });
        }

        private void DrawBlackboardToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new GuiBackgroundColorScope(InspectorControlTint))
                {
                    _blackboardSearch = EditorGUILayout.TextField(
                        _blackboardSearch,
                        GetToolbarSearchFieldStyle(),
                        GUILayout.ExpandWidth(true));
                }

                using (new EditorGUI.DisabledScope(HasRuntimeRunner))
                {
                    if (GUILayout.Button("+", _inspectorMiniButtonStyle, GUILayout.Width(28f)))
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
                string updatedKey;
                using (new GuiBackgroundColorScope(InspectorControlTint))
                    updatedKey = EditorGUILayout.TextField(parameter.Key, GUILayout.ExpandWidth(true));

                var updatedBoolValue = parameter.BoolValue;
                var updatedIntValue = parameter.IntValue;
                var updatedFloatValue = parameter.FloatValue;

                switch (parameter.Type)
                {
                    case BlackboardParameterType.Bool:
                        using (new GuiBackgroundColorScope(InspectorControlTintStrong))
                            updatedBoolValue = EditorGUILayout.Toggle(parameter.BoolValue, GUILayout.Width(18f));
                        break;
                    case BlackboardParameterType.Int:
                        using (new GuiBackgroundColorScope(InspectorControlTint))
                            updatedIntValue = EditorGUILayout.IntField(parameter.IntValue, GUILayout.Width(64f));
                        break;
                    case BlackboardParameterType.Float:
                        using (new GuiBackgroundColorScope(InspectorControlTint))
                            updatedFloatValue = EditorGUILayout.FloatField(parameter.FloatValue, GUILayout.Width(64f));
                        break;
                }

                if (GUILayout.Button("X", _inspectorDangerButtonStyle, GUILayout.Width(28f)))
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
                using (new EditorGUI.DisabledScope(true))
                using (new GuiBackgroundColorScope(InspectorControlTint))
                    EditorGUILayout.TextField(parameter.Key, GUILayout.ExpandWidth(true));

                switch (parameter.Type)
                {
                    case BlackboardParameterType.Bool:
                        EditorGUI.BeginChangeCheck();
                        bool updatedBoolValue;
                        using (new GuiBackgroundColorScope(InspectorControlTintStrong))
                            updatedBoolValue = EditorGUILayout.Toggle(GetRuntimeBoolValue(parameter), GUILayout.Width(18f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            _runner.SetBool(parameter.Key, updatedBoolValue);
                            GUI.changed = true;
                        }

                        break;
                    case BlackboardParameterType.Int:
                        EditorGUI.BeginChangeCheck();
                        int updatedIntValue;
                        using (new GuiBackgroundColorScope(InspectorControlTint))
                            updatedIntValue = EditorGUILayout.IntField(GetRuntimeIntValue(parameter), GUILayout.Width(64f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            _runner.SetInt(parameter.Key, updatedIntValue);
                            GUI.changed = true;
                        }

                        break;
                    case BlackboardParameterType.Float:
                        EditorGUI.BeginChangeCheck();
                        float updatedFloatValue;
                        using (new GuiBackgroundColorScope(InspectorControlTint))
                            updatedFloatValue = EditorGUILayout.FloatField(GetRuntimeFloatValue(parameter), GUILayout.Width(64f));
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
            DrawInspectorSection(
                "Selection",
                "Context-sensitive editor",
                () =>
                {
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

                    DrawInspectorInfo("Select a state or a transition on the canvas to edit it.");
                });
        }

        private void DrawSelectedState(FsmGraphStateNode state)
        {
            GUILayout.Label("State Node", _inspectorSectionSubtitleStyle ?? EditorStyles.miniBoldLabel);

            string updatedName;
            if (UseCompactInspectorLayout)
            {
                GUILayout.Label("Name", _inspectorMetaLabelStyle ?? EditorStyles.miniLabel);
                using (new GuiBackgroundColorScope(InspectorControlTint))
                    updatedName = EditorGUILayout.TextField(state.Name);
            }
            else
            {
                using (new GuiBackgroundColorScope(InspectorControlTint))
                    updatedName = EditorGUILayout.TextField("Name", state.Name);
            }

            if (updatedName != state.Name)
            {
                RecordGraph("Rename State");
                state.Name = updatedName;
                MarkDirty();
            }

            EditorGUI.BeginChangeCheck();
            FsmStateBehaviour updatedState;
            if (UseCompactInspectorLayout)
            {
                GUILayout.Label("Behaviour", _inspectorMetaLabelStyle ?? EditorStyles.miniLabel);
                using (new GuiBackgroundColorScope(InspectorControlTint))
                    updatedState = (FsmStateBehaviour)EditorGUILayout.ObjectField(state.State, typeof(FsmStateBehaviour), false);
            }
            else
            {
                using (new GuiBackgroundColorScope(InspectorControlTint))
                    updatedState = (FsmStateBehaviour)EditorGUILayout.ObjectField("Behaviour", state.State, typeof(FsmStateBehaviour), false);
            }
            if (EditorGUI.EndChangeCheck())
            {
                RecordGraph("Assign State Behaviour");
                state.State = updatedState;
                ClearEmbeddedEditor();
                MarkDirty();
            }

            if (UseCompactInspectorLayout)
            {
                if (GUILayout.Button("Create Behaviour", _inspectorButtonStyle ?? GUI.skin.button))
                    ShowCreateStateMenu(state);

                if (GUILayout.Button("Start Transition", _inspectorButtonStyle ?? GUI.skin.button))
                {
                    _pendingTransitionFromStateId = state.Id;
                    _selectedTransitionId = null;
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create Behaviour", _inspectorButtonStyle ?? GUI.skin.button))
                        ShowCreateStateMenu(state);

                    if (GUILayout.Button("Start Transition", _inspectorButtonStyle ?? GUI.skin.button))
                    {
                        _pendingTransitionFromStateId = state.Id;
                        _selectedTransitionId = null;
                    }
                }
            }

            if (state.State != null)
                DrawEmbeddedEditor(state.State);
            else
                DrawInspectorInfo("Choose or create a state behaviour. Users only need to inherit from FsmStateBehaviour.");

            EditorGUILayout.Space();

            if (GUILayout.Button("Delete State", _inspectorDangerButtonStyle ?? GUI.skin.button))
                DeleteState(state);
        }

        private void DrawSelectedTransition(FsmGraphTransition transition)
        {
            GUILayout.Label("Transition", _inspectorSectionSubtitleStyle ?? EditorStyles.miniBoldLabel);

            var stateOptions = _graph.States.ToArray();
            var stateNames = stateOptions.Select(state => state.Name).ToArray();

            var fromIndex = Mathf.Max(0, Array.FindIndex(stateOptions, state => state.Id == transition.FromStateId));
            var toIndex = Mathf.Max(0, Array.FindIndex(stateOptions, state => state.Id == transition.ToStateId));

            if (stateOptions.Length > 0)
            {
                int updatedFromIndex;
                int updatedToIndex;
                if (UseCompactInspectorLayout)
                {
                    GUILayout.Label("From", _inspectorMetaLabelStyle ?? EditorStyles.miniLabel);
                    using (new GuiBackgroundColorScope(InspectorControlTint))
                        updatedFromIndex = EditorGUILayout.Popup(fromIndex, stateNames);
                    GUILayout.Label("To", _inspectorMetaLabelStyle ?? EditorStyles.miniLabel);
                    using (new GuiBackgroundColorScope(InspectorControlTint))
                        updatedToIndex = EditorGUILayout.Popup(toIndex, stateNames);
                }
                else
                {
                    using (new GuiBackgroundColorScope(InspectorControlTint))
                        updatedFromIndex = EditorGUILayout.Popup("From", fromIndex, stateNames);
                    using (new GuiBackgroundColorScope(InspectorControlTint))
                        updatedToIndex = EditorGUILayout.Popup("To", toIndex, stateNames);
                }

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
                DrawInspectorInfo("This transition has no settings asset yet.");

                if (GUILayout.Button("Create Standard Transition", _inspectorButtonStyle ?? GUI.skin.button))
                    ReplaceWithStandardTransition(transition);
            }
            else if (transition.Transition is not FsmBlackboardTransitionBehaviour)
            {
                DrawInspectorInfo("Legacy custom transition detected. New transitions use the built-in blackboard condition workflow.");

                if (GUILayout.Button("Replace With Standard Transition", _inspectorButtonStyle ?? GUI.skin.button))
                    ReplaceWithStandardTransition(transition);

                DrawEmbeddedEditor(transition.Transition);
            }
            else
            {
                DrawEmbeddedEditor(transition.Transition);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Delete Transition", _inspectorDangerButtonStyle ?? GUI.skin.button))
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
            GUILayout.Label("Behaviour Settings", _inspectorSectionSubtitleStyle ?? EditorStyles.miniBoldLabel);

            var previousWideMode = EditorGUIUtility.wideMode;
            var previousHierarchyMode = EditorGUIUtility.hierarchyMode;
            var previousLabelWidth = EditorGUIUtility.labelWidth;
            var previousFieldWidth = EditorGUIUtility.fieldWidth;
            var previousIndentLevel = EditorGUI.indentLevel;
            var maxWidth = Mathf.Max(110f, GetInspectorWidth() - 32f);

            try
            {
                if (UseCompactInspectorLayout)
                {
                    EditorGUIUtility.wideMode = false;
                    EditorGUIUtility.hierarchyMode = false;
                    EditorGUIUtility.labelWidth = 78f;
                    EditorGUIUtility.fieldWidth = 0f;
                    EditorGUI.indentLevel = 0;
                }

                using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(maxWidth)))
                using (new GuiBackgroundColorScope(InspectorControlTint))
                    _embeddedEditor.OnInspectorGUI();
            }
            finally
            {
                EditorGUIUtility.wideMode = previousWideMode;
                EditorGUIUtility.hierarchyMode = previousHierarchyMode;
                EditorGUIUtility.labelWidth = previousLabelWidth;
                EditorGUIUtility.fieldWidth = previousFieldWidth;
                EditorGUI.indentLevel = previousIndentLevel;
            }
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
            var canvasSize = new Vector2(position.width - GetInspectorWidth() - InspectorSplitterWidth, position.height - ToolbarHeight);
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
            var canvasSize = new Vector2(position.width - GetInspectorWidth() - InspectorSplitterWidth, position.height - ToolbarHeight);
            _canvasPan = (canvasSize - graphSize) * 0.5f - min;
            Repaint();
        }

        private float GetInspectorWidth()
        {
            var maxWidth = Mathf.Min(MaxInspectorWidth, Mathf.Max(MinInspectorWidth, position.width - MinCanvasWidth - InspectorSplitterWidth));
            _inspectorWidth = Mathf.Clamp(_inspectorWidth, MinInspectorWidth, maxWidth);
            return _inspectorWidth;
        }

        private void HandleInspectorResize(Rect splitterRect, Event currentEvent)
        {
            if (currentEvent == null)
                return;

            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && splitterRect.Contains(currentEvent.mousePosition))
            {
                _isResizingInspector = true;
                currentEvent.Use();
                return;
            }

            if (_isResizingInspector && currentEvent.type == EventType.MouseDrag)
            {
                var nextWidth = position.width - currentEvent.mousePosition.x - InspectorSplitterWidth;
                var maxWidth = Mathf.Min(MaxInspectorWidth, Mathf.Max(MinInspectorWidth, position.width - MinCanvasWidth - InspectorSplitterWidth));
                _inspectorWidth = Mathf.Clamp(nextWidth, MinInspectorWidth, maxWidth);
                EditorPrefs.SetFloat(InspectorWidthPrefsKey, _inspectorWidth);
                GUI.changed = true;
                Repaint();
                currentEvent.Use();
                return;
            }

            if (_isResizingInspector && (currentEvent.type == EventType.MouseUp || currentEvent.rawType == EventType.MouseUp))
            {
                _isResizingInspector = false;
                EditorPrefs.SetFloat(InspectorWidthPrefsKey, _inspectorWidth);
                currentEvent.Use();
            }
        }

        private void DrawInspectorSplitter(Rect splitterRect)
        {
            var isHovered = splitterRect.Contains(Event.current.mousePosition);
            var color = isHovered || _isResizingInspector
                ? new Color(InspectorDividerColor.r, InspectorDividerColor.g, InspectorDividerColor.b, 0.45f)
                : InspectorDividerColor;
            EditorGUI.DrawRect(splitterRect, color);
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

            var startTangent = default(Vector2);
            var endTangent = default(Vector2);
            var labelPosition = default(Vector2);
            var arrowTip = default(Vector2);
            var arrowTail = default(Vector2);

            if (transition.FromStateId == transition.ToStateId)
            {
                var loopOffset = GetSelfTransitionLoopOffset(transition);
                var start = new Vector2(fromRect.xMax - 8f, fromRect.center.y - (SelfTransitionLoopHeight * 0.65f));
                var end = new Vector2(fromRect.xMax - 8f, fromRect.center.y + (SelfTransitionLoopHeight * 0.65f));
                startTangent = start + new Vector2(SelfTransitionLoopWidth + loopOffset, -SelfTransitionLoopHeight);
                endTangent = end + new Vector2(SelfTransitionLoopWidth + loopOffset, SelfTransitionLoopHeight);
                labelPosition = EvaluateBezier(start, end, startTangent, endTangent, 0.45f);
                arrowTip = EvaluateBezier(start, end, startTangent, endTangent, 0.84f);
                arrowTail = EvaluateBezier(start, end, startTangent, endTangent, 0.77f);

                return new TransitionVisualData(
                    start,
                    end,
                    startTangent,
                    endTangent,
                    BuildBezierSamples(start, end, startTangent, endTangent),
                    labelPosition,
                    arrowTip,
                    (arrowTip - arrowTail).normalized);
            }

            var direction = toRect.center - fromRect.center;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                direction = Vector2.right;

            direction.Normalize();

            var offset = GetTransitionLaneOffset(transition);
            var startLinePoint = fromRect.center + offset;
            var endLinePoint = toRect.center + offset;
            var startPoint = GetRectEdgePoint(fromRect, startLinePoint, endLinePoint - startLinePoint);
            var endPoint = GetRectEdgePoint(toRect, endLinePoint, startLinePoint - endLinePoint);
            var distance = Vector2.Distance(startPoint, endPoint);
            var tangentDistance = Mathf.Clamp(distance * 0.42f, 55f, 170f);
            var controlOffset = offset * 0.45f;
            startTangent = startPoint + (direction * tangentDistance) + controlOffset;
            endTangent = endPoint - (direction * tangentDistance) + controlOffset;
            labelPosition = EvaluateBezier(startPoint, endPoint, startTangent, endTangent, 0.5f);
            arrowTip = EvaluateBezier(startPoint, endPoint, startTangent, endTangent, 0.92f);
            arrowTail = EvaluateBezier(startPoint, endPoint, startTangent, endTangent, 0.84f);
            var arrowDirection = arrowTip - arrowTail;
            if (arrowDirection.sqrMagnitude <= Mathf.Epsilon)
                arrowDirection = direction;

            return new TransitionVisualData(
                startPoint,
                endPoint,
                startTangent,
                endTangent,
                BuildBezierSamples(startPoint, endPoint, startTangent, endTangent),
                labelPosition,
                arrowTip,
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

        private static Vector2[] BuildBezierSamples(Vector2 start, Vector2 end, Vector2 startTangent, Vector2 endTangent)
        {
            var points = new Vector2[TransitionCurveSampleCount + 1];
            for (var i = 0; i <= TransitionCurveSampleCount; i++)
                points[i] = EvaluateBezier(start, end, startTangent, endTangent, i / (float)TransitionCurveSampleCount);

            return points;
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

        private static Vector2 EvaluateBezier(Vector2 start, Vector2 end, Vector2 startTangent, Vector2 endTangent, float t)
        {
            var invT = 1f - t;
            return
                (invT * invT * invT * start) +
                (3f * invT * invT * t * startTangent) +
                (3f * invT * t * t * endTangent) +
                (t * t * t * end);
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

        private void DrawSelectedTransitionLabel(Vector2 position, string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return;

            var content = new GUIContent(label);
            var textSize = _transitionLabelStyle.CalcSize(content);
            var labelRect = new Rect(
                position.x - (textSize.x * 0.5f) - TransitionLabelPaddingX,
                position.y - (textSize.y * 0.5f) - TransitionLabelPaddingY,
                textSize.x + (TransitionLabelPaddingX * 2f),
                textSize.y + (TransitionLabelPaddingY * 2f));

            DrawRoundedRect(labelRect, new Color(0.12f, 0.15f, 0.20f, 0.96f), new Color(0.38f, 0.65f, 0.90f, 0.35f), 9f);
            GUI.Label(labelRect, content, _transitionLabelStyle);
        }

        private string GetTransitionCanvasLabel(FsmGraphTransition transition)
        {
            if (transition?.Transition == null)
                return "Missing transition settings";

            if (transition.Transition is FsmBlackboardTransitionBehaviour blackboardTransition)
            {
                var conditionCount = blackboardTransition.Conditions.Count;
                var conditionLabel = conditionCount == 1 ? "condition" : "conditions";
                return $"{blackboardTransition.ConditionMode} · {conditionCount} {conditionLabel}";
            }

            return transition.Transition.GetType().Name;
        }

        private int GetOutgoingTransitionCount(string stateId) =>
            _graph.Transitions.Count(transition => transition != null && transition.FromStateId == stateId);

        private string GetStateFooterText(int transitionCount)
        {
            var transitionLabel = transitionCount == 1 ? "transition" : "transitions";
            return $"{transitionCount} {transitionLabel}";
        }

        private bool TryGetStateBadge(
            FsmGraphStateNode state,
            int outgoingTransitionCount,
            bool isInvalid,
            out string badgeText,
            out Color badgeColor)
        {
            badgeText = null;
            badgeColor = Color.clear;

            if (isInvalid)
            {
                badgeText = "Invalid";
                badgeColor = InvalidColor;
                return true;
            }

            if (state != null && state.Id == GetRuntimeCurrentStateId())
            {
                badgeText = "Active";
                badgeColor = RuntimeActiveColor;
                return true;
            }

            if (outgoingTransitionCount == 0)
            {
                badgeText = "No Exit";
                badgeColor = WarningColor;
                return true;
            }

            if (_graph.InitialStateId == state.Id)
            {
                badgeText = "Default";
                badgeColor = InitialAccentColor;
                return true;
            }

            return false;
        }

        private void UpdateHoverState(Rect canvasRect, Event currentEvent)
        {
            string hoveredStateId = null;
            string hoveredTransitionId = null;

            if (currentEvent != null && canvasRect.Contains(currentEvent.mousePosition))
            {
                var hoveredState = FindStateAt(canvasRect, currentEvent.mousePosition);
                hoveredStateId = hoveredState?.Id;

                if (hoveredState == null)
                    hoveredTransitionId = FindTransitionAt(canvasRect, currentEvent.mousePosition)?.Id;
            }

            if (_hoveredStateId == hoveredStateId && _hoveredTransitionId == hoveredTransitionId)
                return;

            _hoveredStateId = hoveredStateId;
            _hoveredTransitionId = hoveredTransitionId;
            GUI.changed = true;
        }

        private void EnsureStyles()
        {
            if (_nodeTitleStyle != null &&
                _nodeSubtitleStyle != null &&
                _nodeFooterStyle != null &&
                _nodeBadgeStyle != null &&
                _transitionLabelStyle != null &&
                _inspectorSectionStyle != null &&
                _inspectorSectionTitleStyle != null &&
                _inspectorSectionSubtitleStyle != null &&
                _inspectorMetaLabelStyle != null &&
                _inspectorMetaValueStyle != null &&
                _inspectorEmptyStateStyle != null &&
                _inspectorInfoTextStyle != null &&
                _inspectorButtonStyle != null &&
                _inspectorDangerButtonStyle != null &&
                _inspectorMiniButtonStyle != null &&
                _inspectorSectionTexture != null &&
                _inspectorInfoTexture != null &&
                _inspectorButtonTexture != null &&
                _inspectorButtonHoverTexture != null &&
                _inspectorButtonActiveTexture != null &&
                _inspectorDangerButtonTexture != null &&
                _inspectorDangerButtonHoverTexture != null &&
                _inspectorDangerButtonActiveTexture != null)
                return;

            DestroyInspectorTexture(ref _inspectorSectionTexture);
            DestroyInspectorTexture(ref _inspectorInfoTexture);
            DestroyInspectorTexture(ref _inspectorButtonTexture);
            DestroyInspectorTexture(ref _inspectorButtonHoverTexture);
            DestroyInspectorTexture(ref _inspectorButtonActiveTexture);
            DestroyInspectorTexture(ref _inspectorDangerButtonTexture);
            DestroyInspectorTexture(ref _inspectorDangerButtonHoverTexture);
            DestroyInspectorTexture(ref _inspectorDangerButtonActiveTexture);
            _inspectorSectionTexture = CreateRoundedTexture(24, 24, InspectorCardColor, InspectorCardBorderColor, 8);
            _inspectorInfoTexture = CreateRoundedTexture(20, 20, InspectorInfoBackgroundColor, InspectorInfoBorderColor, 7);
            _inspectorButtonTexture = CreateRoundedTexture(20, 20, InspectorButtonColor, InspectorButtonBorderColor, 6);
            _inspectorButtonHoverTexture = CreateRoundedTexture(20, 20, InspectorButtonHoverColor, InspectorButtonBorderColor, 6);
            _inspectorButtonActiveTexture = CreateRoundedTexture(20, 20, InspectorButtonActiveColor, InspectorButtonBorderColor, 6);
            _inspectorDangerButtonTexture = CreateRoundedTexture(20, 20, InspectorDangerButtonColor, new Color(0.75f, 0.39f, 0.44f, 0.18f), 6);
            _inspectorDangerButtonHoverTexture = CreateRoundedTexture(20, 20, InspectorDangerButtonHoverColor, new Color(0.82f, 0.44f, 0.49f, 0.22f), 6);
            _inspectorDangerButtonActiveTexture = CreateRoundedTexture(20, 20, InspectorDangerButtonActiveColor, new Color(0.70f, 0.35f, 0.40f, 0.18f), 6);

            _nodeTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
            };
            _nodeTitleStyle.normal.textColor = NodeTitleColor;

            _nodeSubtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
            };
            _nodeSubtitleStyle.normal.textColor = NodeSubtitleColor;

            _nodeFooterStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
            };
            _nodeFooterStyle.normal.textColor = NodeFooterColor;

            _nodeBadgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                padding = new RectOffset(6, 6, 2, 1),
            };
            _nodeBadgeStyle.normal.textColor = Color.white;

            _transitionLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
            };
            _transitionLabelStyle.normal.textColor = NodeTitleColor;

            _inspectorSectionStyle = new GUIStyle(GUIStyle.none)
            {
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(8, 8, 0, 0),
                normal =
                {
                    background = _inspectorSectionTexture,
                },
            };

            _inspectorSectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
            };
            _inspectorSectionTitleStyle.normal.textColor = InspectorSectionTitleColor;

            _inspectorSectionSubtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                clipping = TextClipping.Clip,
            };
            _inspectorSectionSubtitleStyle.normal.textColor = InspectorSectionSubtitleColor;

            _inspectorMetaLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 11,
                clipping = TextClipping.Clip,
            };
            _inspectorMetaLabelStyle.normal.textColor = InspectorMetaLabelColor;

            _inspectorMetaValueStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                clipping = TextClipping.Clip,
                wordWrap = false,
            };
            _inspectorMetaValueStyle.normal.textColor = InspectorMetaValueColor;

            _inspectorEmptyStateStyle = new GUIStyle(GUIStyle.none)
            {
                border = new RectOffset(7, 7, 7, 7),
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 2, 2),
                normal =
                {
                    background = _inspectorInfoTexture,
                },
            };

            _inspectorInfoTextStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                fontSize = 11,
                wordWrap = true,
            };
            _inspectorInfoTextStyle.normal.textColor = InspectorSectionSubtitleColor;

            _inspectorButtonStyle = CreateInspectorButtonStyle(
                _inspectorButtonTexture,
                _inspectorButtonHoverTexture,
                _inspectorButtonActiveTexture,
                11,
                new RectOffset(8, 8, 6, 6));

            _inspectorDangerButtonStyle = CreateInspectorButtonStyle(
                _inspectorDangerButtonTexture,
                _inspectorDangerButtonHoverTexture,
                _inspectorDangerButtonActiveTexture,
                11,
                new RectOffset(8, 8, 6, 6));

            _inspectorMiniButtonStyle = CreateInspectorButtonStyle(
                _inspectorButtonTexture,
                _inspectorButtonHoverTexture,
                _inspectorButtonActiveTexture,
                10,
                new RectOffset(8, 8, 5, 5));
        }

        private GUIStyle CreateInspectorButtonStyle(
            Texture2D normalBackground,
            Texture2D hoverBackground,
            Texture2D activeBackground,
            int fontSize,
            RectOffset padding)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                border = new RectOffset(6, 6, 6, 6),
                padding = padding,
                margin = new RectOffset(2, 2, 2, 2),
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
            };

            style.normal.background = normalBackground;
            style.normal.scaledBackgrounds = new[] { normalBackground };
            style.hover.background = hoverBackground;
            style.hover.scaledBackgrounds = new[] { hoverBackground };
            style.active.background = activeBackground;
            style.active.scaledBackgrounds = new[] { activeBackground };
            style.focused.background = hoverBackground;
            style.focused.scaledBackgrounds = new[] { hoverBackground };
            style.onNormal.background = normalBackground;
            style.onNormal.scaledBackgrounds = new[] { normalBackground };
            style.onHover.background = hoverBackground;
            style.onHover.scaledBackgrounds = new[] { hoverBackground };
            style.onActive.background = activeBackground;
            style.onActive.scaledBackgrounds = new[] { activeBackground };
            style.onFocused.background = hoverBackground;
            style.onFocused.scaledBackgrounds = new[] { hoverBackground };
            style.normal.textColor = InspectorButtonTextColor;
            style.hover.textColor = InspectorButtonTextColor;
            style.active.textColor = InspectorButtonTextColor;
            style.focused.textColor = InspectorButtonTextColor;
            style.onNormal.textColor = InspectorButtonTextColor;
            style.onHover.textColor = InspectorButtonTextColor;
            style.onActive.textColor = InspectorButtonTextColor;
            style.onFocused.textColor = InspectorButtonTextColor;
            return style;
        }

        private bool UseCompactInspectorLayout => GetInspectorWidth() <= 220f;

        private static Texture2D CreateRoundedTexture(int width, int height, Color fillColor, Color borderColor, int radius)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color[width * height];
            var innerRadius = Mathf.Max(0, radius - 1);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var isInside = IsInsideRoundedRect(x, y, width, height, radius);
                    if (!isInside)
                    {
                        pixels[(y * width) + x] = Color.clear;
                        continue;
                    }

                    var isBorder = !IsInsideRoundedRect(x, y, width, height, innerRadius);
                    pixels[(y * width) + x] = isBorder ? borderColor : fillColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            if (radius <= 0)
                return x >= 0 && x < width && y >= 0 && y < height;

            var maxX = width - 1;
            var maxY = height - 1;

            if ((x >= radius && x <= maxX - radius) || (y >= radius && y <= maxY - radius))
                return true;

            var cornerCenterX = x < radius ? radius : maxX - radius;
            var cornerCenterY = y < radius ? radius : maxY - radius;
            var deltaX = x - cornerCenterX;
            var deltaY = y - cornerCenterY;
            return (deltaX * deltaX) + (deltaY * deltaY) <= radius * radius;
        }

        private static void DestroyInspectorTexture(ref Texture2D texture)
        {
            if (texture == null)
                return;

            DestroyImmediate(texture);
            texture = null;
        }

        private readonly struct GuiBackgroundColorScope : IDisposable
        {
            private readonly Color _previousColor;

            public GuiBackgroundColorScope(Color color)
            {
                _previousColor = GUI.backgroundColor;
                GUI.backgroundColor = color;
            }

            public void Dispose()
            {
                GUI.backgroundColor = _previousColor;
            }
        }

        private readonly struct TransitionVisualData
        {
            public TransitionVisualData(
                Vector2 startPoint,
                Vector2 endPoint,
                Vector2 startTangent,
                Vector2 endTangent,
                Vector2[] points,
                Vector2 labelPosition,
                Vector2 arrowTip,
                Vector2 arrowDirection)
            {
                StartPoint = startPoint;
                EndPoint = endPoint;
                StartTangent = startTangent;
                EndTangent = endTangent;
                Points = points;
                LabelPosition = labelPosition;
                ArrowTip = arrowTip;
                ArrowDirection = arrowDirection;
            }

            public Vector2 StartPoint { get; }
            public Vector2 EndPoint { get; }
            public Vector2 StartTangent { get; }
            public Vector2 EndTangent { get; }
            public Vector2[] Points { get; }
            public Vector2 LabelPosition { get; }
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
            _hoveredStateId = null;
            _hoveredTransitionId = null;
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
