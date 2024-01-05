#region

using TMPro;
using UnityEngine;

#endregion

namespace Utils
{
	/// <summary>
	/// Provides function to conditionally set this GameObject active based on assigned text contents.
	/// </summary>
	public class TextConditionalActivator : MonoBehaviour
	{
		// Needs to be assigned from the editor as `Start()` does not get called on disabled objects
		[Tooltip("UI text to read text value from")]
		public TMP_Text text;

		private string _initialText;

		/// <summary>
		/// Sets the GameObject to active only if the displayed text has changed since startup.
		/// </summary>
		/// <param name="state"></param>
		public void SetActiveConditional(bool state)
		{
			if (_initialText == null) _initialText = text.text;

			if (!state) gameObject.SetActive(false);

			gameObject.SetActive(text.text != _initialText);
		}
	}
}