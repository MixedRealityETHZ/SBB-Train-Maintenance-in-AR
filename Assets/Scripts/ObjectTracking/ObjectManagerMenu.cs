using System;
using System.Collections.Generic;
using MixedReality.Toolkit.UX;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;

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

	public void ToggleActive()
	{
		gameObject.SetActive(!gameObject.activeSelf);
		if (gameObject.activeSelf) InGameNotification.ClearNotification();
	}

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

	public void RemoveObject(Guid modelId)
	{
		_buttons.Remove(modelId, out var button);
		Destroy(button);

		if (_buttons.Count == 0) noObjectText.SetActive(true);
	}
}