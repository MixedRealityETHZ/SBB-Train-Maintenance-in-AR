#region

using System.Collections;
using TMPro;
using UnityEngine;

#endregion

namespace UI
{
	/// <summary>
	///     Shows various notifications as an in-game UI element.
	/// </summary>
	public class InGameNotification : MonoBehaviour
	{
		private static InGameNotification _instance;
		private static Coroutine _timeout;

		[Tooltip("GameObject containing the notification text that should be enabled/disabled")]
		public GameObject notification;

		[Tooltip("Notification text UI element")]
		public TMP_Text text;

		// Start is called before the first frame update
		private void Start()
		{
			notification.SetActive(false);
			_instance = this;
		}

		/// <summary>
		///     Sets the currently displayed notification. If `timeout == null`, notification stays visible indefinitely.
		///     If `timeout > 0`, notification is cleared after `timeout` seconds. If another notification is currently
		///     being displayed, its timeout is canceled and the notification is replaced by the new one.
		/// </summary>
		/// <param name="message">Notification text</param>
		/// <param name="timeout">
		///     Seconds after which notification is cleared. Set to `null` for a permanent notification (can still be
		///     cleared by subsequent timed notifications)
		/// </param>
		public static void SetNotification(string message, float? timeout = null)
		{
			Debug.Log($"Showing notification: {message}");
			if (_timeout != null) _instance.StopCoroutine(_timeout);
			_instance.notification.SetActive(true);
			_instance.text.text = message;

			if (timeout != null) _timeout = _instance.StartCoroutine(ClearAfterTimeout(timeout.Value));
		}

		/// <summary>
		/// Clears the currently displayed notification (if any)
		/// </summary>
		public static void ClearNotification()
		{
			Debug.Log("Clearing current notification");
			_instance.notification.SetActive(false);
		}

		private static IEnumerator ClearAfterTimeout(float timeout)
		{
			yield return new WaitForSeconds(timeout);
			Debug.Log("Clearing notification after timeout");
			ClearNotification();
		}
	}
}