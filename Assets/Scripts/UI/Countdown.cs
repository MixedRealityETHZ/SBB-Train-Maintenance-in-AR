#region

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

#endregion

namespace UI
{
	public class Countdown : MonoBehaviour
	{
		public int countdownSeconds;
		public float onCompleteDelay = 0.3f;
		public UnityEvent onComplete;

		public GameObject countdownPanel;
		public TMP_Text countdownText;

		private void Start()
		{
			countdownPanel.SetActive(false);
		}

		public void StartCountdown()
		{
			countdownPanel.SetActive(true);
			StartCoroutine(CountdownCoroutine());
		}

		private IEnumerator CountdownCoroutine()
		{
			for (var i = countdownSeconds; i > 0; i--)
			{
				countdownText.text = i.ToString();
				yield return new WaitForSeconds(1f);
			}

			yield return new WaitForSeconds(onCompleteDelay);
			countdownPanel.SetActive(false);
			onComplete.Invoke();
		}
	}
}