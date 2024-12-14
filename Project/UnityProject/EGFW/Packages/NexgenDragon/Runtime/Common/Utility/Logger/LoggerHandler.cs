using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace NexgenDragon
{
	[AttributeUsage(AttributeTargets.Method)]
	public class StackTraceIgnoreAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Method)]
	public class LogUnityOnlyAttribute : Attribute {}

	public enum LogSeverity
	{
		Trace,
		Message,
		Warning,
		Error,
		Assert,

		Disable,
	}

	public interface ILogger
	{
		void Log(LogInfo logInfo);
	}

	[System.Serializable]
	public class LogInfo
	{
		private UnityEngine.Object _source;
		public UnityEngine.Object Source
		{
			get
			{
				return _source;
			}
		}

		private string _channel;
		public string Channel
		{
			get
			{
				// Get log channel from message
				var match = LoggerHandler.UnityChannelRegex.Matches(_message);
				if (match.Count > 0)
				{
					_channel = match[match.Count - 1].Groups[0].Value;

					// Parse Channel
					if (!string.IsNullOrEmpty(_channel))
					{
						string[] matchedChannel = _channel.Split('"');
						if (matchedChannel != null && matchedChannel.Length == 3)
						{
							_channel = matchedChannel[1];
						}
						else
						{
							_channel = "";
						}
					}
				}

				return _channel;
			}
		}

		private LogSeverity _severity;
		public LogSeverity Severity
		{
			get
			{
				return _severity;
			}
		}

		private string _message;
		public string Message
		{
			get
			{
				return _message;
			}
		}

		private string _lineMessage;
		public string LineMessage
		{
			get
			{
				if (string.IsNullOrEmpty(_lineMessage))
				{
					if (_messages == null)
					{
						_messages = System.Text.RegularExpressions.Regex.Split(_message, System.Environment.NewLine);
					}

					if (_lines == null)
					{
						_lines = System.Text.RegularExpressions.Regex.Split(_unityCallStack, System.Environment.NewLine);
					}

					if (_messages != null && _messages.Length >= 2)
					{
						_lineMessage = string.Format("{0}{2}{1}", _messages[0], _messages[1], System.Environment.NewLine);
					}
					else if (_lines != null && _lines.Length > 0)
					{
						_lineMessage = string.Format("{0}{2}{1}", _message, _lines[0], System.Environment.NewLine);
					}
				}

				return _lineMessage;
			}
		}

		private string _messageWithoutChannel;
		public string MessageWithoutChannel
		{
			get
			{
				_messageWithoutChannel = _message;

				// Get log channel from message
				var match = LoggerHandler.UnityChannelRegex.Matches(_message);
				if (match.Count > 0)
				{
					_channel = match[match.Count - 1].Groups[0].Value;

					// Get message without channel
					_messageWithoutChannel = _message.Substring(0, _message.Length - _channel.Length);
				}

				return _messageWithoutChannel;
			}
		}

		private string _unityCallStack;
		public string UnityCallStack
		{
			get
			{
				return _unityCallStack;
			}
		}

		private List<LogStackFrame> _callStack;
		public List<LogStackFrame> CallStack
		{
			get
			{
				return _callStack;
			}
		}

		private string _sourceFileName;
		public string SourceFileName
		{
			get
			{
				if (_lines == null)
				{
					_lines = System.Text.RegularExpressions.Regex.Split(_unityCallStack, System.Environment.NewLine);
				}

				if (_lines != null)
				{
					string declaringType = "";
					string methodName = "";
					string fileName = "";
					int lineNumber = 0;

					for (int i = 0; i < _lines.Length; i++)
					{
						if (LoggerHandler.ExtractInfoFromUnityStackInfo(_lines[i], ref declaringType, ref methodName, ref fileName, ref lineNumber))
						{
							_sourceFileName = fileName;
							_sourceFileLineNumber = lineNumber;

							break;
						}
					}

					if (string.IsNullOrEmpty(fileName))
					{
						for (int i = 0; i < _lines.Length; i++)
						{
							if (LoggerHandler.ExtractInfoFromUnityMessage(_lines[i], ref fileName, ref lineNumber))
							{
								_sourceFileName = fileName;
								_sourceFileLineNumber = lineNumber;

								break;
							}
						}
					}
				}

				return _sourceFileName;
			}
		}

		private int _sourceFileLineNumber;
		public int SourceFileLineNumber
		{
			get
			{
				return _sourceFileLineNumber;
			}
		}

		private string[] _messages = null;
		private string[] _lines = null;

		public LogInfo(UnityEngine.Object source, LogSeverity severity, List<LogStackFrame> callStack, object unityCallStack, object message, params object[] param)
		{
			_source = source;
			_severity = severity;
			_message = "";

			var tmpMessage = message as String;
			if (tmpMessage != null)
			{
				if (param.Length > 0)
				{
					_message = System.String.Format(tmpMessage, param);
				}
				else
				{
					_message = tmpMessage;
				}
			}

			_callStack = callStack;
			_unityCallStack = unityCallStack as String;
		}
	}

	[System.Serializable]
	public class LogStackFrame
	{
		private int _lineNumber;
		private string _fileName;

		private string _methodName;
		private string _declaringType;
		private string _parameters;

		private string _formattedMethodName;
		private bool _showFileDetail;

		public int LineNumber
		{
			get
			{
				return _lineNumber;
			}
		}

		public string FileName
		{
			get
			{
				return _fileName;
			}
		}

		public string DeclaringType
		{
			get
			{
				return _declaringType;
			}
		}

		/// <summary>
		/// Convert from a .Net stack frame
		/// </summary>
		public LogStackFrame(StackFrame frame, bool showFileDetail = true)
		{
			var method = frame.GetMethod();

			_methodName = method.Name;
			_declaringType = method.DeclaringType.Name;
			_showFileDetail = showFileDetail;

			var tmpParams = method.GetParameters();
			for (int i = 0; i < tmpParams.Length; i++)
			{
				_parameters += String.Format("{0} {1}", tmpParams[i].ParameterType, tmpParams[i].Name);
				if (i + 1 < tmpParams.Length)
				{
					_parameters += ", ";
				}
			}

			_fileName = frame.GetFileName();
			_lineNumber = frame.GetFileLineNumber();
			_formattedMethodName = MakeFormattedMethodName();
		}

		/// <summary>
		/// Convert from a Unity stack frame
		/// </summary>
		public LogStackFrame(string unityStackFrame, bool showFileDetail = true)
		{
			_showFileDetail = showFileDetail;

			// update method parameter
			var match = Regex.Matches(unityStackFrame, @"\(.*?\)");

			if (match.Count > 0)
			{
				_parameters = match[0].Groups[0].Value;
				_parameters = _parameters.Replace("(", "");
				_parameters = _parameters.Replace(")", "");
			}

			if (LoggerHandler.ExtractInfoFromUnityStackInfo(unityStackFrame, ref _declaringType, ref _methodName, ref _fileName, ref _lineNumber))
			{
				_formattedMethodName = MakeFormattedMethodName();
			}
			else
			{
				_formattedMethodName = unityStackFrame;
			}
		}

		/// <summary>
		/// Basic stack frame info when we have nothing else
		/// </summary>
		public LogStackFrame(string message, string fileName, int lineNumber)
		{
			_fileName = fileName;
			_lineNumber = lineNumber;
			_formattedMethodName = message;
		}

		public string GetFormattedMethodName()
		{
			return _formattedMethodName;
		}

		/// <summary>
		/// Make a nice string showing the stack information
		/// </summary>
		string MakeFormattedMethodName()
		{
			string fileName = _fileName;
			if (!String.IsNullOrEmpty(fileName))
			{
				var startSubName = fileName.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);

				if (startSubName > 0)
				{
					fileName = fileName.Substring(startSubName);
				}
			}

			string methodName = "";
			if (_showFileDetail && !string.IsNullOrEmpty(fileName))
			{
				methodName = String.Format("{0}.{1}({2}) (at {3}:{4})", _declaringType, _methodName, _parameters, fileName, _lineNumber);
			}
			else
			{
				methodName = String.Format("{0}.{1}({2})", _declaringType, _methodName, _parameters);
			}

			return methodName;
		}
	}
	
	public static class LoggerHandler
	{
		static int _maxMessageToKeep = 1000;
		static bool _forwardMessages = true;
		static int _currentSeverity = 0;

		const string LOG_CHANNEL_FORMAT = "<color=\"{0}\"></color>";

        static object _locker = new object();
		static List<ILogger> _loggers = new List<ILogger>();
		static LinkedList<LogInfo> _recentMessages = new LinkedList<LogInfo>();
        static Queue<LogMessage> _logQueue = new Queue<LogMessage>();
        static bool _alreadyLogging = false;

		static Regex _unityMessageRegex;
		static Regex _unityMessageFromStackRegex;
		static Regex _unityChannelRegex;

		public static int MaxMessageToKeep
		{
			get
			{
				return _maxMessageToKeep;
			}
		}

		public static bool ForwardMessages
		{
			get
			{
				return _forwardMessages;
			}
		}

		public static Regex UnityChannelRegex
		{
			get
			{
				return _unityChannelRegex;
			}
		}

		static LoggerHandler()
		{
			// Register with Unity's logging system
			Application.logMessageReceivedThreaded += UnityLogHandler;

			_unityMessageRegex = new Regex(@"(.*)\((\d+).*\)");
			_unityChannelRegex = new Regex(@"<color=""(.*)""></color>");
			_unityMessageFromStackRegex = new Regex(@"(.*)\:(.*)\s*\(.*\(at (.*):(\d+)");
		}

		#region Handle Unity callback

		public class LogMessage
		{
			public string logString;
			public string stackTrace;
			public LogType logType;

			public LogMessage(string logString, string stackTrace, LogType logType)
			{
				this.logString = logString;
				this.stackTrace = stackTrace;
				this.logType = logType;
			}
		}

		/// <summary>
		/// Registered Unity log handler
		/// </summary>
		[StackTraceIgnore]
		static void UnityLogHandler(string logString, string stackTrace, UnityEngine.LogType logType)
		{
            lock (_locker)
            {
                _logQueue.Enqueue(new LogMessage(logString, stackTrace, logType));
            }
		}

		#endregion

		#region public logger interface

		/// <summary>
		/// Finds a registered logger, if it exists
		/// </summary>
		public static T GetLogger<T>() where T : class
		{
			foreach (var logger in _loggers)
			{
				if (logger is T)
				{
					return logger as T;
				}
			}

			return null;
		}

		public static LogSeverity GetSeverityByType(LogType logType)
		{
			LogSeverity severity;

			switch (logType)
			{
			case UnityEngine.LogType.Assert:
				severity = LogSeverity.Assert;
				break;
				
			case UnityEngine.LogType.Error:
			case UnityEngine.LogType.Exception:
				severity = LogSeverity.Error;
				break;

			case UnityEngine.LogType.Warning:
				severity = LogSeverity.Warning;
				break;

			default:
				severity = LogSeverity.Message;
				break;
			}

			return severity;
		}

		/// <summary>
		/// Try to extract useful information about the log from a Unity stack trace
		/// </summary>
		public static bool ExtractInfoFromUnityStackInfo(string log, ref string declaringType, ref string methodName, ref string fileName, ref int lineNumber)
		{
			// log = "DebugLoggerEditorWindow.DrawLogDetails () (at Assets/Code/Editor.cs:298)";
			var match = _unityMessageFromStackRegex.Matches(log);

			if (match.Count > 0)
			{
				declaringType = match[0].Groups[1].Value;
				methodName = match[0].Groups[2].Value;
				fileName = match[0].Groups[3].Value;
				lineNumber = Convert.ToInt32(match[0].Groups[4].Value);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Try to extract useful information about the log from a Unity error message.
		/// Only used when handling a Unity error message and we can't get a useful callstack
		/// </summary>
		public static bool ExtractInfoFromUnityMessage(string log, ref string fileName, ref int lineNumber)
		{
			// log = "Assets/Code/Debug.cs(140,21): warning CS0618: 'some error'
			var match = _unityMessageRegex.Matches(log);

			if (match.Count > 0)
			{
				fileName = match[0].Groups[1].Value;
				lineNumber = Convert.ToInt32(match[0].Groups[2].Value);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Registers a new logger backend, which we be told every time there's a new log.
		/// If populateWithExistingMessages is true, Logger will immediately pump the new logger with past messages
		/// </summary>
		public static void AddLogger(ILogger logger, bool populateWithExistingMessages = true)
		{
            lock (_locker)
			{
				if (populateWithExistingMessages)
				{
					foreach(var oldLog in _recentMessages)
					{
						logger.Log(oldLog);
					}
				}

				if (!_loggers.Contains(logger))
				{
					_loggers.Add(logger);
				}
			}
		}

		public static void RemoveLogger(ILogger logger)
		{
			if (logger == null) return;

            lock (_locker)
            {
                if (_loggers.Contains(logger))
                {
                    _loggers.Remove(logger);
                }
            }
		}

		/// <summary>
		/// Execute once one frame to handle log queue
		/// </summary>
		[StackTraceIgnore]
		public static void Update()
		{
            lock (_locker)
            {
                for (int i = 0; i < _logQueue.Count; i++)
                {
                    LogMessage logMessage = _logQueue.Dequeue();
                    if (logMessage != null)
                    {
                        UnityLogInternal(logMessage.logString, logMessage.stackTrace, logMessage.logType);
                    }
                }    
            }
		}

		[StackTraceIgnore]
		public static void SetSeverity(int severity)
		{
			_currentSeverity = severity;
		}
        
        [StackTraceIgnore]
        public static int GetSeverity()
        {
            return _currentSeverity;
        }

		/// <summary>
		/// The core entry point of all logging coming from client code.
		/// Takes a log request, creates the call stack and pumps it to all the backends
		/// </summary>
		[StackTraceIgnore]
		public static void Log(string channel, UnityEngine.Object source, LogSeverity severity, object message, params object[] param)
		{
            lock (_locker)
			{
				if (!_alreadyLogging)
				{
					try
					{
						_alreadyLogging = true;
						
						if (_currentSeverity > (int)severity) return;

						// Append Channel to message
						message += string.Format(LOG_CHANNEL_FORMAT, channel);

						// If required, pump this message back into Unity console
						if (_forwardMessages)
						{
							ForwardToUnity(severity, message, param);
						}
						else
						{
							var callStack = new List<LogStackFrame>();
							var unityOnly = GetCallStack(ref callStack);

							if (unityOnly) return;

							var logInfo = new LogInfo(source, severity, callStack, "", message, param);

							// Add this message to our history
							_recentMessages.AddLast(logInfo);

							// Make sure our history doesn't get too big
							TrimOldMessages();

							// Delete any dead loggers and pump them with the new log
							_loggers.RemoveAll(l => l == null);
							_loggers.ForEach(l => l.Log(logInfo));
						}
					}
					finally
					{
						_alreadyLogging = false;
					}
				}
			}
		}

		#endregion

		#region private logger interface

		/// <summary>
		/// The core entry point of all logging coming from Unity. Takes a log request, creates the call stack and pumps it
		/// </summary>
		[StackTraceIgnore]
		static void UnityLogInternal(string unityMessage, string unityCallStack, UnityEngine.LogType logType)
		{
            lock (_locker)
			{
				// Prevent nasty recursion problems
				if (!_alreadyLogging)
				{
					try
					{
						_alreadyLogging = true;
						List<LogStackFrame> callStack = null;

						if (!_forwardMessages)
						{
							callStack = new List<LogStackFrame>();
							var unityOnly = GetCallStack(ref callStack);

							if (unityOnly) return;

							// If we have no useful callstack, fall back to parsing Unity's callstack
							if (callStack.Count == 0 || !string.IsNullOrEmpty(unityCallStack))
							{
								callStack = GetCallStackFromUnityLog(unityCallStack);
							}

							string fileName = "";
							int lineNumber = 0;

							// Finally, parse the error message so we can get basic file and line information
							if (ExtractInfoFromUnityMessage(unityMessage, ref fileName, ref lineNumber))
							{
								callStack.Insert(0, new LogStackFrame(unityMessage, fileName, lineNumber));
							}
						}

						var logInfo = new LogInfo(null, GetSeverityByType(logType), callStack, unityCallStack, unityMessage);

						// Add this message to our history
						_recentMessages.AddLast(logInfo);

						// Make sure our history doesn't get too big
						TrimOldMessages();

						// Delete any dead loggers and pump them with the new log
						_loggers.RemoveAll(l => l == null);
						_loggers.ForEach(l => l.Log(logInfo));
					}
					finally
					{
						_alreadyLogging = false;
					}
				}
			}
		}

		static void TrimOldMessages()
		{
			while (_recentMessages.Count > _maxMessageToKeep)
			{
				_recentMessages.RemoveFirst();
			}
		}

		/// <summary>
		/// Converts the curent stack trace into a list of Logger's LogStackFrame.
		/// Excludes any methods with the StackTraceIgnore attribute
		/// </summary>
		[StackTraceIgnore]
		static bool GetCallStack(ref List<LogStackFrame> callStack)
		{
			callStack.Clear();

			StackTrace stackTrace = new StackTrace(true);  // get call stack
			StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)

			foreach (StackFrame stackFrame in stackFrames)
			{
				var method = stackFrame.GetMethod();

				if (method.IsDefined(typeof(LogUnityOnlyAttribute), true))
				{
					return true;
				}

				if (!method.IsDefined(typeof(StackTraceIgnoreAttribute), true))
				{
					// Cut out some internal noise from Unity stuff
					if (!(method.Name == "CallLogCallback" && method.DeclaringType.Name == "Application")
						&& !(method.DeclaringType.Name == "Debug" && (method.Name == "Internal_Log" || method.Name == "Log")))
					{
						callStack.Add(new LogStackFrame(stackFrame));
					}
				}
				else if (method.DeclaringType.Name == "Logger")
				{
					callStack.Add(new LogStackFrame(stackFrame, false));
				}
			}

			// update the 1st log info
			if (callStack[0].DeclaringType == "Logger" && callStack.Count >= 2)
			{
				string msgStr = callStack[0].GetFormattedMethodName();
				string msgAppend = callStack[1].GetFormattedMethodName();
				int strAtIndex = msgAppend.IndexOf(" (at ");

				if (strAtIndex > 0 && strAtIndex < msgAppend.Length)
				{
					msgAppend = msgAppend.Substring(strAtIndex);
					msgStr += msgAppend;

					LogStackFrame tmpStackFrame = new LogStackFrame(msgStr, false);
					callStack[0] = tmpStackFrame;
				}
			}

			return false;
		}

		/// <summary>
		/// Forwards an Logger log to Unity so it's visible in the built-in console
		/// </summary>
		[LogUnityOnly]
		static void ForwardToUnity(LogSeverity severity, object message, params object[] param)
		{
			object showObject = null;
			if (message != null)
			{
				var msgAsStr = message as string;
				if (msgAsStr != null)
				{
					if (param.Length > 0)
					{
						showObject = String.Format(msgAsStr, param);
					}
					else
					{
						showObject = message;
					}
				}
				else
				{
					showObject = message;
				}
			}

			switch (severity)
			{
			case LogSeverity.Message:
			case LogSeverity.Trace:
				UnityEngine.Debug.Log(showObject);
				break;

			case LogSeverity.Warning:
				UnityEngine.Debug.LogWarning(showObject);
				break;

			case LogSeverity.Error:
			case LogSeverity.Assert:
				UnityEngine.Debug.LogError(showObject);
				break;
			}
		}

		static List<LogStackFrame> GetCallStackFromUnityLog(string unityCallStack)
		{
			var lines = Regex.Split(unityCallStack, System.Environment.NewLine);
			var stack = new List<LogStackFrame>();
			bool firstStackFrameAdded = false;
			bool curStackIncludeFileLine = false;
			int curLineIndex = 0;
			string appendStr = "";
			int startIndex = 0;

			foreach (var line in lines)
			{
				appendStr = line;

				if (appendStr.Contains(" (at "))
				{
					startIndex = appendStr.IndexOf(" (at ");
					break;
				}
			}

			foreach (var line in lines)
			{
				++curLineIndex;

				string tmpStr = line;

				if (tmpStr.Contains(" (at "))
				{
					firstStackFrameAdded = true;
					curStackIncludeFileLine = true;
				}

				if (!firstStackFrameAdded)
				{
					if (startIndex > 0)
					{
						tmpStr += appendStr.Substring(startIndex);
					}

					firstStackFrameAdded = true;
				}

				var frame = new LogStackFrame(tmpStr, curLineIndex != 1 || curStackIncludeFileLine);
				if (!string.IsNullOrEmpty(frame.GetFormattedMethodName()))
				{
					stack.Add(frame as LogStackFrame);
				}

				curStackIncludeFileLine = false;
			}

			return stack;
		}

		#endregion
		
		/// <summary>
		/// 异常
		/// </summary>
		public static void Exception(System.Exception e)
		{
			if (e == null) return;
#if MO_LOG || UNITY_DEBUG || MO_DEBUG
			UnityEngine.Debug.LogException(e);
#else
		if(_enableExceptionCallback)
			_onExceptionCallback?.Invoke(e);
#endif
		}

		/// <summary>
		/// 异常
		/// </summary>
		public static void Exception(string message)
		{
			if (string.IsNullOrEmpty(message)) return;
#if MO_LOG || UNITY_DEBUG || MO_DEBUG
			UnityEngine.Debug.LogException(new System.Exception(message));
#else
		if(_enableExceptionCallback)
			_onExceptionCallback?.Invoke(new System.Exception(message));
#endif
		}
		
		public static void EnableExceptionEvent(bool v)
		{
			_enableExceptionCallback = v;
		}
		
		public static void AddExceptionCallback(System.Action<System.Exception> callback)
		{
			_onExceptionCallback += callback;
		}
		public static void RemoveExceptionCallback(System.Action<System.Exception> callback)
		{
			_onExceptionCallback -= callback;
		}

		// exception回调
		private static event Action<Exception> _onExceptionCallback;

		// 是否开启exception监听
		private static bool _enableExceptionCallback = false;
	}
}
