using System.Collections;
using TMPro;
using UnityEngine;

public class InGameNotification : MonoBehaviour
{
	private static InGameNotification _instance;
	private static Coroutine _timeout;

	public GameObject notification;
	public TMP_Text text;

	// Start is called before the first frame update
	private void Start()
	{
		notification.SetActive(false);
		_instance = this;
	}

	public static void SetNotification(string message, float? timeout = null)
	{
		Debug.Log($"Showing notification: {message}");
		if (_timeout != null) _instance.StopCoroutine(_timeout);
		_instance.notification.SetActive(true);
		_instance.text.text = message;

		if (timeout != null) _timeout = _instance.StartCoroutine(ClearAfterTimeout(timeout.Value));
	}

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