using System;
using System.Collections;
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

    private Dictionary<Guid, GameObject> _buttons;

    public void Start()
    {
        noObjectText.SetActive(true);
    }

    public void AddObject(Guid modelId, Guid instanceId, string objectName)
    {
        var button = Instantiate(objectButtonPrefab, transform);
        _buttons[modelId] = button;
        var text = button.GetNamedChild("Text").GetComponent<TextMeshPro>();
        text.text = $"<alpha=#70>Remove <alpha=#ff>{objectName}";
        button.GetComponent<PressableButton>().OnClicked.AddListener(() =>
            onObjectRemoveRequested.Invoke(modelId, instanceId));
        noObjectText.SetActive(false);
    }

    public void RemoveObject(Guid modelId)
    {
        _buttons.Remove(modelId, out var button);
        Destroy(button);

        if (_buttons.Count == 0)
        {
            noObjectText.SetActive(true);
        }
    }
}
