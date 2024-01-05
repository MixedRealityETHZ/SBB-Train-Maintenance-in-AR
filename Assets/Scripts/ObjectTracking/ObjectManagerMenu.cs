#region

using System;
using System.Collections.Generic;
using MixedReality.Toolkit.UX;
using TMPro;
using UI;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;

#endregion

namespace ObjectTracking
{
	/// <summary>
	///     MonoBehavior script managing the "Manage Tracked Objects" menu.
	/// </summary>
	public class ObjectManagerMenu : MonoBehaviour
	{
		public GameObject noObjectText;
		public GameObject objectButtonPrefab;

		public UnityEvent<Guid, Guid> onObjectRemoveRequested;

		private readonly Dictionary<Guid, GameObject> _buttons = new();

		public void Start()
		{
			noObjectText.SetActive(true);
		}

		/// <summary>
		///     Enables or disables the menu being shown.
		/// </summary>
		public void ToggleActive()
		{
			gameObject.SetActive(!gameObject.activeSelf);
			if (gameObject.activeSelf) InGameNotification.ClearNotification();
		}

		/// <summary>
		///     Add an object to the list of actively tracked objects.
		/// </summary>
		/// <param name="modelId">AOA model ID</param>
		/// <param name="instanceId">AOA instance ID</param>
		/// <param name="objectName">Display name of the object</param>
		public void AddObject(Guid modelId, Guid instanceId, string objectName)
		{
			Debug.Log($"Adding object to manage objects menu: {modelId}/{instanceId}/{objectName}");
			var button = Instantiate(objectButtonPrefab, transform);
			_buttons[modelId] = button;
			var text = button.GetNamedChild("ObjectNameText").GetComponent<TMP_Text>();
			text.text = $"<alpha=#70>Re-track <alpha=#ff>{objectName}";
			button.GetComponent<PressableButton>().OnClicked.AddListener(() =>
				onObjectRemoveRequested.Invoke(modelId, instanceId));
			noObjectText.SetActive(false);
		}

		/// <summary>
		///     Removes an object from the list of tracked objects.
		/// </summary>
		/// <param name="modelId">AOA model ID</param>
		public void RemoveObject(Guid modelId)
		{
			_buttons.Remove(modelId, out var button);
			Destroy(button);

			if (_buttons.Count == 0) noObjectText.SetActive(true);
		}
	}
}