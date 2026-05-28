using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace FlyingAI.Utils
{
    #region Enums
    
    /// <summary>
    /// Log level enumeration for filtering debug messages.
    /// </summary>
    public enum LogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Verbose = 4
    }
    
    #endregion

    /// <summary>
    /// Stores a single log entry with timestamp and metadata.
    /// </summary>
    public struct LogEntry
    {
        public string Timestamp;
        public LogLevel Level;
        public LogCategory Category;
        public string Message;
        public string StackTrace;

        public override string ToString()
        {
            string levelStr = Level switch
            {
                LogLevel.Error => "[ERROR]",
                LogLevel.Warning => "[WARN]",
                LogLevel.Verbose => "[VERBOSE]",
                _ => "[INFO]"
            };
            return $"{Timestamp} {levelStr} [{Category}] {Message}";
        }

        public string ToStringWithStackTrace()
        {
            return $"{ToString()}\n{StackTrace}";
        }
    }

    /// <summary>
    /// Custom debug logger with editor toggle for enabling/disabling logging.
    /// Supports log levels and conditional compilation for production builds.
    /// </summary>
    [CreateAssetMenu(fileName = "DebugLoggerSettings", menuName = "FlyingAI/Settings/Debug Logger")]
    public class DebugLoggerSettings : ScriptableObject
    {
        #region Settings
        
        [Header("Logging Settings")]
        [Tooltip("Enable or disable all logging")]
        [SerializeField] private bool _enableLogging = true;
        
        [Tooltip("Current log level threshold")]
        [SerializeField] private LogLevel _logLevel = LogLevel.Info;
        
        [Header("Log History")]
        [Tooltip("Maximum number of logs to keep in memory")]
        [SerializeField] private int _maxLogHistory = 1000;
        
        [Tooltip("Include stack traces for errors")]
        [SerializeField] private bool _includeStackTraces = true;
        
        [Header("Category Filters")]
        [Tooltip("Enable flocking system logs")]
        [SerializeField] private bool _logFlocking = true;
        
        [Tooltip("Enable formation system logs")]
        [SerializeField] private bool _logFormation = true;
        
        [Tooltip("Enable combat system logs")]
        [SerializeField] private bool _logCombat = true;
        
        [Tooltip("Enable state machine logs")]
        [SerializeField] private bool _logStateMachine = true;
        
        [Tooltip("Enable spawning system logs")]
        [SerializeField] private bool _logSpawning = true;
        
        [Tooltip("Enable pooling system logs")]
        [SerializeField] private bool _logPooling = true;
        
        #endregion

        #region Properties
        
        public bool EnableLogging => _enableLogging;
        public LogLevel CurrentLogLevel => _logLevel;
        public int MaxLogHistory => _maxLogHistory;
        public bool IncludeStackTraces => _includeStackTraces;
        public bool LogFlocking => _logFlocking;
        public bool LogFormation => _logFormation;
        public bool LogCombat => _logCombat;
        public bool LogStateMachine => _logStateMachine;
        public bool LogSpawning => _logSpawning;
        public bool LogPooling => _logPooling;
        
        #endregion
    }

    /// <summary>
    /// Static debug logger utility class.
    /// Provides categorized logging with compile-time stripping for release builds.
    /// Stores logs in memory for easy clipboard copying.
    /// </summary>
    public static class DebugLogger
    {
        #region Fields
        
        private static DebugLoggerSettings _settings;
        private static bool _initialized;
        private static List<LogEntry> _logHistory = new List<LogEntry>();
        private static List<LogEntry> _errorHistory = new List<LogEntry>();
        private static int _maxHistory = 1000;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Gets all stored log entries.
        /// </summary>
        public static IReadOnlyList<LogEntry> LogHistory => _logHistory;
        
        /// <summary>
        /// Gets only error log entries.
        /// </summary>
        public static IReadOnlyList<LogEntry> ErrorHistory => _errorHistory;
        
        /// <summary>
        /// Gets the total number of stored logs.
        /// </summary>
        public static int LogCount => _logHistory.Count;
        
        /// <summary>
        /// Gets the number of stored errors.
        /// </summary>
        public static int ErrorCount => _errorHistory.Count;
        
        #endregion

        #region Initialization
        
        /// <summary>
        /// Initializes the logger with settings from Resources.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            
            _settings = Resources.Load<DebugLoggerSettings>("DebugLoggerSettings");
            _initialized = true;
            
            if (_settings != null)
            {
                _maxHistory = _settings.MaxLogHistory;
            }
            
            if (_settings == null)
            {
                Debug.LogWarning("[DebugLogger] No DebugLoggerSettings found in Resources folder. Using default settings.");
            }
        }
        
        #endregion

        #region Logging Methods
        
        /// <summary>
        /// Logs an info message.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message, Object context = null)
        {
            LogInternal(LogLevel.Info, message, LogCategory.General, context);
        }
        
        /// <summary>
        /// Logs a categorized info message.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message, LogCategory category, Object context = null)
        {
            LogInternal(LogLevel.Info, message, category, context);
        }
        
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string message, Object context = null)
        {
            LogInternal(LogLevel.Warning, message, LogCategory.General, context);
        }
        
        /// <summary>
        /// Logs a categorized warning message.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string message, LogCategory category, Object context = null)
        {
            LogInternal(LogLevel.Warning, message, category, context);
        }
        
        /// <summary>
        /// Logs an error message.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(string message, Object context = null)
        {
            LogInternal(LogLevel.Error, message, LogCategory.General, context);
        }
        
        /// <summary>
        /// Logs a categorized error message.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(string message, LogCategory category, Object context = null)
        {
            LogInternal(LogLevel.Error, message, category, context);
        }
        
        /// <summary>
        /// Logs a verbose message for detailed debugging.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogVerbose(string message, Object context = null)
        {
            LogInternal(LogLevel.Verbose, message, LogCategory.General, context);
        }
        
        /// <summary>
        /// Logs a categorized verbose message.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogVerbose(string message, LogCategory category, Object context = null)
        {
            LogInternal(LogLevel.Verbose, message, category, context);
        }
        
        #endregion

        #region Clipboard Methods
        
        /// <summary>
        /// Copies ALL logs to the system clipboard.
        /// </summary>
        public static void CopyAllLogsToClipboard()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== FlyingAI Debug Log Export ({System.DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===");
            sb.AppendLine($"Total Entries: {_logHistory.Count}");
            sb.AppendLine("========================================");
            sb.AppendLine();
            
            foreach (var entry in _logHistory)
            {
                sb.AppendLine(entry.ToString());
            }
            
            GUIUtility.systemCopyBuffer = sb.ToString();
            Debug.Log($"<color=#00FF00>[DebugLogger] Copied {_logHistory.Count} log entries to clipboard!</color>");
        }
        
        /// <summary>
        /// Copies only ERROR logs to the system clipboard.
        /// </summary>
        public static void CopyErrorsToClipboard()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== FlyingAI ERROR Log Export ({System.DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===");
            sb.AppendLine($"Total Errors: {_errorHistory.Count}");
            sb.AppendLine("========================================");
            sb.AppendLine();
            
            bool includeStack = _settings?.IncludeStackTraces ?? true;
            
            foreach (var entry in _errorHistory)
            {
                if (includeStack && !string.IsNullOrEmpty(entry.StackTrace))
                    sb.AppendLine(entry.ToStringWithStackTrace());
                else
                    sb.AppendLine(entry.ToString());
                sb.AppendLine();
            }
            
            GUIUtility.systemCopyBuffer = sb.ToString();
            Debug.Log($"<color=#FF0000>[DebugLogger] Copied {_errorHistory.Count} ERROR entries to clipboard!</color>");
        }
        
        /// <summary>
        /// Copies logs filtered by category to clipboard.
        /// </summary>
        public static void CopyLogsByCategoryToClipboard(LogCategory category)
        {
            StringBuilder sb = new StringBuilder();
            int count = 0;
            
            sb.AppendLine($"=== FlyingAI [{category}] Log Export ({System.DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===");
            sb.AppendLine("========================================");
            sb.AppendLine();
            
            foreach (var entry in _logHistory)
            {
                if (entry.Category == category)
                {
                    sb.AppendLine(entry.ToString());
                    count++;
                }
            }
            
            GUIUtility.systemCopyBuffer = sb.ToString();
            Debug.Log($"<color=#00FFFF>[DebugLogger] Copied {count} [{category}] entries to clipboard!</color>");
        }
        
        /// <summary>
        /// Clears all stored log history.
        /// </summary>
        public static void ClearLogHistory()
        {
            _logHistory.Clear();
            _errorHistory.Clear();
            Debug.Log("<color=#FFFF00>[DebugLogger] Log history cleared.</color>");
        }
        
        /// <summary>
        /// Gets all logs as a single string.
        /// </summary>
        public static string GetAllLogsAsString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var entry in _logHistory)
            {
                sb.AppendLine(entry.ToString());
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// Gets all errors as a single string.
        /// </summary>
        public static string GetErrorsAsString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var entry in _errorHistory)
            {
                sb.AppendLine(entry.ToString());
            }
            return sb.ToString();
        }
        
        #endregion

        #region Internal Methods
        
        private static void LogInternal(LogLevel level, string message, LogCategory category, Object context)
        {
            if (!_initialized) Initialize();
            
            if (_settings != null)
            {
                if (!_settings.EnableLogging) return;
                if (level > _settings.CurrentLogLevel) return;
                if (!IsCategoryEnabled(category)) return;
            }
            
            string prefix = GetCategoryPrefix(category);
            string formattedMessage = $"{prefix} {message}";
            
            // Store in history
            LogEntry entry = new LogEntry
            {
                Timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff"),
                Level = level,
                Category = category,
                Message = message,
                StackTrace = level == LogLevel.Error ? System.Environment.StackTrace : null
            };
            
            AddToHistory(entry);
            
            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(formattedMessage, context);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage, context);
                    break;
                default:
                    Debug.Log(formattedMessage, context);
                    break;
            }
        }
        
        private static void AddToHistory(LogEntry entry)
        {
            // Add to main history
            _logHistory.Add(entry);
            if (_logHistory.Count > _maxHistory)
            {
                _logHistory.RemoveAt(0);
            }
            
            // Add errors to separate list
            if (entry.Level == LogLevel.Error)
            {
                _errorHistory.Add(entry);
                if (_errorHistory.Count > _maxHistory)
                {
                    _errorHistory.RemoveAt(0);
                }
            }
        }
        
        private static bool IsCategoryEnabled(LogCategory category)
        {
            if (_settings == null) return true;
            
            return category switch
            {
                LogCategory.Flocking => _settings.LogFlocking,
                LogCategory.Formation => _settings.LogFormation,
                LogCategory.Combat => _settings.LogCombat,
                LogCategory.StateMachine => _settings.LogStateMachine,
                LogCategory.Spawning => _settings.LogSpawning,
                LogCategory.Pooling => _settings.LogPooling,
                _ => true
            };
        }
        
        private static string GetCategoryPrefix(LogCategory category)
        {
            return category switch
            {
                LogCategory.Flocking => "<color=#00FF00>[Flocking]</color>",
                LogCategory.Formation => "<color=#00FFFF>[Formation]</color>",
                LogCategory.Combat => "<color=#FF0000>[Combat]</color>",
                LogCategory.StateMachine => "<color=#FFFF00>[StateMachine]</color>",
                LogCategory.Spawning => "<color=#FF00FF>[Spawning]</color>",
                LogCategory.Pooling => "<color=#FFA500>[Pooling]</color>",
                _ => "<color=#FFFFFF>[General]</color>"
            };
        }
        
        #endregion
    }

    #region Log Categories
    
    /// <summary>
    /// Categories for filtering log messages by system.
    /// </summary>
    public enum LogCategory
    {
        General,
        Flocking,
        Formation,
        Combat,
        StateMachine,
        Spawning,
        Pooling
    }
    
    #endregion
}
