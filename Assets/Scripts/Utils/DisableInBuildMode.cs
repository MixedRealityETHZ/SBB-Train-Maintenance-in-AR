#region

using UnityEngine;

#endregion

namespace Utils
{
	/// <summary>
	///     Attach this script to a GameObject to deactivate it in builds. Useful for objects that are only required
	///		while editing in the Unity editor.
	/// </summary>
	public class DisableInBuildMode : MonoBehaviour
	{
		private void Start()
		{
			if (!Application.isEditor) gameObject.SetActive(false);
		}
	}
}