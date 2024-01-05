#region

using UnityEngine;

#endregion

namespace Utils
{
	/// <summary>
	///     Add this to a GameObject to deactivate it on game startup.
	/// </summary>
	public class DeactivateOnStart : MonoBehaviour
	{
		// Start is called before the first frame update
		private void Awake()
		{
			gameObject.SetActive(false);
		}
	}
}