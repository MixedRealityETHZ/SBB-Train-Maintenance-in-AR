#region

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

#endregion

namespace Debugging
{
	/// <summary>
	/// MonoBehavior script for displaying console messages in-game for easier run-time debugging
	/// </summary>
	public class InGameDebugConsole : MonoBehaviour
	{
		public TMP_Text header;
		public TMP_Text body;
		public int maxLines = 10;
		public LogType logLevel = LogType.Log;

		private readonly List<LogType> _levelsOrder = new()
		{
			LogType.Log,
			LogType.Warning,
			LogType.Assert,
			LogType.Error,
			LogType.Exception
		};

		private readonly List<LogLine> _logLines = new();
		private IEnumerable<LogLine> FilteredLogLines => _logLines.Where(ShouldShow);

		// Start is called before the first frame update
		private void Awake()
		{
			header.text = "Console";
			body.text = "";
			Application.logMessageReceivedThreaded += OnMessageReceived;
		}

		private bool ShouldShow(LogLine line)
		{
			var logLevelIndex = _levelsOrder.IndexOf(logLevel);
			var lineIndex = _levelsOrder.IndexOf(line.LogType);
			var logLevelShouldShow = lineIndex >= logLevelIndex;

			// Add other filters here

			return logLevelShouldShow;
		}

		private string GetBodyText()
		{
			return string.Join("\n", FilteredLogLines);
		}

		private void OnMessageReceived(string condition, string stacktrace, LogType type)
		{
			if (_logLines.Count > maxLines) _logLines.RemoveAt(0);

			if (_logLines.Count > 0 && _logLines[^1].Message == condition)
			{
				var lastLine = _logLines[^1];
				lastLine.Repeats += 1;
				lastLine.LogTime = DateTime.Now;
				_logLines[^1] = lastLine;
			}
			else
			{
				var logLine = new LogLine(condition, stacktrace, type);
				_logLines.Add(logLine);
			}

			// Update text
			body.text = GetBodyText();
		}

		/// <summary>
		/// Filters messages below the given log level
		/// </summary>
		/// <param name="level">Lowest level of message that is displayed.</param>
		public void SetLogLevel(int level)
		{
			var logType = _levelsOrder[level];
			logLevel = logType;
			body.text = GetBodyText();
		}

		private struct LogLine
		{
			public readonly string Message;
			public string Stacktrace;
			public readonly LogType LogType;
			public int Repeats;
			public DateTime LogTime;

			public LogLine(string message, string stacktrace, LogType logLogType, int repeats = 0)
			{
				Message = message;
				Stacktrace = stacktrace;
				LogType = logLogType;
				Repeats = repeats;
				LogTime = DateTime.Now;
			}

			public override string ToString()
			{
				var repeatsString = Repeats > 0 ? $" | (x{1 + Repeats})" : "";
				return $"[{LogTime:HH:mm:ss} | {LogType}{repeatsString}] {Message}";
			}
		}
	}
}