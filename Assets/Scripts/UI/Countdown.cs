#region

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

#endregion

namespace UI
{
	/// <summary>
	///     Shows a countdown on a UI text.
	/// </summary>
	public class Countdown : MonoBehaviour
	{
		[Tooltip("Number of seconds to count down from.")]
		public int countdownSeconds;

		[Tooltip("Add a small delay when the last second passed. 0.3 seconds 'feels' the best.")]
		public float onCompleteDelay = 0.3f;

		[Tooltip("Fired when the countdown is complete (after additional delay)")]
		public UnityEvent onComplete;

		[Tooltip("GameObject containing the countdown text. Gets enabled and disabled appropriately.")]
		public GameObject countdownPanel;

		[Tooltip("Text element where the countdown should be displayed")]
		public TMP_Text countdownText;

		private void Start()
		{
			countdownPanel.SetActive(false);
		}

		/// <summary>
		///     Starts the countdown. Use `this.onComplete` to invoke a callback when countdown finished.
		/// </summary>
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