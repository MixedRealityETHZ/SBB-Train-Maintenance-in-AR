#region

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#endregion

namespace ObjectTracking
{
	public class ModelSwitcher : MonoBehaviour
	{
		public List<ModelSettings> visualizationPrefabs = new();

		private GameObject _visualizationPrefabInstance;

		public void SetModel(string modelName)
		{
			if (_visualizationPrefabInstance != null) Destroy(_visualizationPrefabInstance);

			var prefab = visualizationPrefabs.Find(x => x.name == modelName).visualizationPrefab;
			if (prefab is null)
			{
				Debug.LogWarning($"No visualization prefab for model with name: {modelName}");
				return;
			}

			_visualizationPrefabInstance = Instantiate(prefab, gameObject.transform);
		}

		[Serializable]
		public struct ModelSettings
		{
			public string name;
			public GameObject visualizationPrefab;
		}
	}
}