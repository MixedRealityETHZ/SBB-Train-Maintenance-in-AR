#region

using System;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace ObjectTracking
{
	/// <summary>
	///     Dynamically switches the displayed model based on a mapping from model name to visualization prefab.
	///     Attach to a GameObject and assign your mappings in the editor. Then call the `SetModel` method to select one of
	///     the models.
	/// </summary>
	public class ModelSwitcher : MonoBehaviour
	{
		public List<ModelSettings> visualizationPrefabs = new();

		private GameObject _visualizationPrefabInstance;

		/// <summary>
		///     Selects the currently active model. Mapping must be set up in editor before calling this.
		/// </summary>
		/// <param name="modelName">Name of the `ModelSettings` object.</param>
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

		/// <summary>
		///     Struct to keep model name to visualization prefab mappings. Dictionaries are not serializable in Unity.
		/// </summary>
		[Serializable]
		public struct ModelSettings
		{
			public string name;
			public GameObject visualizationPrefab;
		}
	}
}