// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#region

using System;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

#endregion

public class TextLogger : MonoBehaviour
{
	private const int MaxMessageCountToShow = 8;
	private static TextLogger Instance;

	public Text LoggerText;

	private readonly ConcurrentQueue<string> _messageQueue = new();

	private void Awake()
	{
		if (Instance == null) Instance = this;
	}

	private void Start()
	{
	}

	private void OnEnable()
	{
		Application.logMessageReceivedThreaded += HandleLog;
	}

	private void OnDisable()
	{
		Application.logMessageReceivedThreaded -= HandleLog;
	}

	private void HandleLog(string logString, string stackTrace, LogType type)
	{
		// Do nothing here, as this handler could be called from a non-UI thread.
	}

	private void ShowMessage()
	{
		LoggerText.text = string.Empty;

		foreach (var item in _messageQueue.Skip(Math.Max(0, _messageQueue.Count - MaxMessageCountToShow)))
			LoggerText.text += $"{item}\n";
	}

	/// <summary>
	///     Log message without adding timestamp.
	/// </summary>
	public static void LogRaw(string message)
	{
		Debug.Log(message);
		while (Instance._messageQueue.Count >= MaxMessageCountToShow)
		{
			string _message;
			Instance._messageQueue.TryDequeue(out _message);
		}

		Instance._messageQueue.Enqueue(message);
		Instance.ShowMessage();
	}

	public static void Log(string message)
	{
		LogRaw($"[{DateTime.Now.ToLongTimeString()}] {message}");
	}

	public static string Truncate(string source, int length)
	{
		if (source.Length > length) source = source.Substring(0, length);

		return source;
	}
}