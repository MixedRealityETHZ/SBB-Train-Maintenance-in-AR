using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelSwitcher : MonoBehaviour
{
    [Serializable]
    public struct ModelSettings
    {
        public string Name;
        public GameObject VisualizationPrefab;
    }

    public List<ModelSettings>
        visualizationPrefabs = new();

    private GameObject _visualizationPrefabInstance = null;

    public void SetModel(string name)
    {
        if (_visualizationPrefabInstance != null)
        {
            Destroy(_visualizationPrefabInstance);
        }

        var prefab = visualizationPrefabs.Find(x => x.Name == name).VisualizationPrefab;
        if (prefab is null)
        {
            Debug.LogWarning($"No visualization prefab for model with name: {name}");
            return;
        }
        
        _visualizationPrefabInstance = Instantiate(prefab, gameObject.transform);
    }
}
