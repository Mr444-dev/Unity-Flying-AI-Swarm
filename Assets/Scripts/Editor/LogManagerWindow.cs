using UnityEngine;
using UnityEditor;
using FlyingAI.Utils;

namespace FlyingAI.Editor
{
    /// <summary>
    /// Editor window for managing and copying debug logs.
    /// Access via Window > FlyingAI > Log Manager
    /// </summary>
    public class LogManagerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private LogCategory _filterCategory = LogCategory.General;
        private bool _showAllCategories = true;
        private bool _autoScroll = true;
        private int _lastLogCount;
        
        [MenuItem("Window/FlyingAI/Log Manager %#L")] // Ctrl+Shift+L shortcut
        public static void ShowWindow()
        {
            var window = GetWindow<LogManagerWindow>("Log Manager");
            window.minSize = new Vector2(400, 300);
        }
        
        private void OnEnable()
        {
            _lastLogCount = DebugLogger.LogCount;
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            DrawLogList();
            DrawFooter();
            
            // Auto-refresh when new logs arrive
            if (DebugLogger.LogCount != _lastLogCount)
            {
                _lastLogCount = DebugLogger.LogCount;
                Repaint();
            }
        }
        
        private void OnInspectorUpdate()
        {
            // Refresh periodically
            Repaint();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Copy buttons
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("📋 Copy ALL Logs", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                DebugLogger.CopyAllLogsToClipboard();
            }
            
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("⚠️ Copy ERRORS", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                DebugLogger.CopyErrorsToClipboard();
            }
            
            GUI.backgroundColor = Color.white;
            
            GUILayout.FlexibleSpace();
            
            // Filter dropdown
            _showAllCategories = GUILayout.Toggle(_showAllCategories, "All", EditorStyles.toolbarButton, GUILayout.Width(40));
            
            if (!_showAllCategories)
            {
                _filterCategory = (LogCategory)EditorGUILayout.EnumPopup(_filterCategory, EditorStyles.toolbarPopup, GUILayout.Width(100));
                
                if (GUILayout.Button("Copy Category", EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    DebugLogger.CopyLogsByCategoryToClipboard(_filterCategory);
                }
            }
            
            GUILayout.Space(10);
            
            _autoScroll = GUILayout.Toggle(_autoScroll, "Auto-Scroll", EditorStyles.toolbarButton, GUILayout.Width(80));
            
            GUI.backgroundColor = new Color(1f, 1f, 0.5f);
            if (GUILayout.Button("🗑️ Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                DebugLogger.ClearLogHistory();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawLogList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            var logs = DebugLogger.LogHistory;
            
            foreach (var entry in logs)
            {
                if (!_showAllCategories && entry.Category != _filterCategory)
                    continue;
                
                DrawLogEntry(entry);
            }
            
            // Auto-scroll to bottom
            if (_autoScroll && logs.Count > 0)
            {
                _scrollPosition.y = float.MaxValue;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawLogEntry(LogEntry entry)
        {
            Color bgColor = entry.Level switch
            {
                LogLevel.Error => new Color(1f, 0.3f, 0.3f, 0.3f),
                LogLevel.Warning => new Color(1f, 1f, 0.3f, 0.3f),
                _ => new Color(1f, 1f, 1f, 0.1f)
            };
            
            Color textColor = entry.Level switch
            {
                LogLevel.Error => new Color(1f, 0.5f, 0.5f),
                LogLevel.Warning => new Color(1f, 0.9f, 0.5f),
                LogLevel.Verbose => new Color(0.7f, 0.7f, 0.7f),
                _ => Color.white
            };
            
            EditorGUILayout.BeginHorizontal();
            
            // Background box
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(18));
            EditorGUI.DrawRect(rect, bgColor);
            
            // Timestamp
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            EditorGUI.LabelField(new Rect(rect.x, rect.y, 80, rect.height), entry.Timestamp);
            
            // Level icon
            string levelIcon = entry.Level switch
            {
                LogLevel.Error => "❌",
                LogLevel.Warning => "⚠️",
                LogLevel.Verbose => "📝",
                _ => "ℹ️"
            };
            EditorGUI.LabelField(new Rect(rect.x + 85, rect.y, 20, rect.height), levelIcon);
            
            // Category
            GUI.color = GetCategoryColor(entry.Category);
            EditorGUI.LabelField(new Rect(rect.x + 108, rect.y, 90, rect.height), $"[{entry.Category}]");
            
            // Message
            GUI.color = textColor;
            EditorGUI.LabelField(new Rect(rect.x + 200, rect.y, rect.width - 210, rect.height), entry.Message);
            
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        
        private Color GetCategoryColor(LogCategory category)
        {
            return category switch
            {
                LogCategory.Flocking => Color.green,
                LogCategory.Formation => Color.cyan,
                LogCategory.Combat => Color.red,
                LogCategory.StateMachine => Color.yellow,
                LogCategory.Spawning => Color.magenta,
                LogCategory.Pooling => new Color(1f, 0.6f, 0f),
                _ => Color.white
            };
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField($"Total Logs: {DebugLogger.LogCount}", GUILayout.Width(120));
            EditorGUILayout.LabelField($"Errors: {DebugLogger.ErrorCount}", GUILayout.Width(100));
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.LabelField("Shortcut: Ctrl+Shift+L", EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
