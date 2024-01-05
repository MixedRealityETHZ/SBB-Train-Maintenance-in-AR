#region

using TMPro;
using UnityEngine;

#endregion

namespace Utils
{
	public class TextConditionalActivator : MonoBehaviour
	{
		// Needs to be assigned from the editor as `Start()` does not get called on disabled objects
		public TMP_Text text;

		private string _initialText;

		public void SetActiveConditional(bool state)
		{
			if (_initialText == null) _initialText = text.text;

			if (!state) gameObject.SetActive(false);

			gameObject.SetActive(text.text != _initialText);
		}
	}
}