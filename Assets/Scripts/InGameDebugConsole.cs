using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class InGameDebugConsole : MonoBehaviour
{
    private readonly List<LogType> levelsOrder = new()
        { LogType.Log, LogType.Warning, LogType.Assert, LogType.Error, LogType.Exception };

    public TMP_Text header;
    public TMP_Text body;
    public int maxLines = 10;
    [FormerlySerializedAs("LogLevel")] public LogType logLevel = LogType.Log;

    private struct LogLine
    {
        public string Message;
        public string Stacktrace;
        public LogType LogType;
        public int Repeats;
        public DateTime LogTime;

        public LogLine(string message, string stacktrace, LogType logLogType, int repeats = 0)
        {
            this.Message = message;
            this.Stacktrace = stacktrace;
            this.LogType = logLogType;
            this.Repeats = repeats;
            this.LogTime = DateTime.Now;
        }

        public override string ToString()
        {
            var repeatsString = Repeats > 0 ? $" | (x{1 + Repeats})" : "";
            return $"[{LogTime:HH:mm:ss} | {LogType}{repeatsString}] {Message}";
        }
    }

    private readonly List<LogLine> logLines = new List<LogLine>();
    private IEnumerable<LogLine> FilteredLogLines => logLines.Where(ShouldShow);

    private bool ShouldShow(LogLine line)
    {
        var logLevelIndex = levelsOrder.IndexOf(logLevel);
        var lineIndex = levelsOrder.IndexOf(line.LogType);
        var logLevelShouldShow = lineIndex >= logLevelIndex;

        // Add other filters here

        return logLevelShouldShow;
    }

    private string GetBodyText()
    {
        return string.Join("\n", FilteredLogLines);
    }

    // Start is called before the first frame update
    void Awake()
    {
        header.text = "Console";
        body.text = "";
        Application.logMessageReceivedThreaded += OnMessageReceived;
    }

    private void OnMessageReceived(string condition, string stacktrace, LogType type)
    {
        if (logLines.Count > maxLines)
        {
            logLines.RemoveAt(0);
        }

        if (logLines.Count > 0 && logLines[^1].Message == condition)
        {
            var lastLine = logLines[^1];
            lastLine.Repeats += 1;
            lastLine.LogTime = DateTime.Now;
            logLines[^1] = lastLine;
        }
        else
        {
            var logLine = new LogLine(condition, stacktrace, type);
            logLines.Add(logLine);
        }

        // Update text
        body.text = GetBodyText();
    }

    public void SetLogLevel(int level)
    {
        var logType = levelsOrder[level];
        logLevel = logType;
        body.text = GetBodyText();
    }
}