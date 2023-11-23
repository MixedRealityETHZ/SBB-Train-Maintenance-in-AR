#if WINDOWS_UWP || DOTNETWINRT_PRESENT || UNITY_EDITOR
#define SPATIALCOORDINATESYSTEM_API_PRESENT
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.ObjectAnchors;
using Microsoft.Azure.ObjectAnchors.SpatialGraph;
using Microsoft.Azure.ObjectAnchors.Unity;
using UnityEngine;
using UnityEngine.Events;
using System.IO;

#if WINDOWS_UWP
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Storage.Search;
#endif

public class ObjectAnchorManager : MonoBehaviour
{
	private IObjectAnchorsService _objectAnchorsService;
	private Camera _mainCamera;
	private SpatialGraphCoordinateSystem? _coordinateSystem;
	private Dictionary<Guid, ObjectModelSettings> _modelSettings = new();

	public ObjectModelSettings[] objectModels = { };
	public float searchAreaFarDistance = 4.0f;
	public float searchAreaFOV = 75.0f;
	public float searchAreaAspect = 1.0f;
	public float searchAreaScaleFactor = 2.0f;
	public ObjectObservationMode observationMode = ObjectObservationMode.Ambient;
	public UnityEvent<object, IObjectAnchorsServiceEventArgs> onAdded;
	public UnityEvent<object, IObjectAnchorsServiceEventArgs> onUpdated;
	public UnityEvent<object, IObjectAnchorsServiceEventArgs> onRemoved;


	[Serializable]
	public struct ObjectModelSettings
	{
		public string modelNameWithoutExtension;
		public GameObject visualizationPrefab;
	}


	private class ObjectAnchorsException : Exception
	{
		public ObjectAnchorsException(string message) : base("ObjectAnchorsException: " + message)
		{
		}
	}

	private void Awake()
	{
		_objectAnchorsService = ObjectAnchorsService.GetService();

		if (_objectAnchorsService == null)
			throw new ObjectAnchorsException("Could not get ObjectAnchorsService!");

		InitializeCallbacks();
	}

	private async void Start()
	{
		// Initialize service
		try
		{
			await _objectAnchorsService.InitializeAsync();
		}
		catch (ArgumentException ex)
		{
			throw new ObjectAnchorsException(
				"Failed to initialize object anchors service. Your account info might be incorrect.");
		}

		// Load models. Model IDs will be saved in the ObjectAnchorsService.
		await FindAndLoadModelFiles();
		Debug.Log("Loaded models: " + string.Join(", ", _objectAnchorsService.ModelIds));

		_mainCamera = Camera.main;
		if (_mainCamera == null) throw new ObjectAnchorsException("No main camera found!");

		// Repeatedly try to search for objects
		InvokeRepeating(nameof(SearchAllObjects), 0.0f, 1.0f); // 0s delay, repeat every second
	}

	private async Task FindAndLoadModelFiles()
	{
#if WINDOWS_UWP
        // Accessing a known but protected folder only works when using the StorageFolder/StorageFile apis
        // and not the System.IO apis. On some devices, the static StorageFolder for well known folders
        // like the 3d objects folder returns access denied when queried, but behaves as expected when
        // accessed by path. Frustratingly, on some devices the opposite is true, and the static StorageFolder 
        // works and the workaround finds no files.
        StorageFolder objects3d = KnownFolders.Objects3D;

        // First try using the static folder directly, which will throw an exception on some devices
        try
        {
            foreach (string filePath in FileHelper.GetFilesInDirectory(objects3d.Path, "*.ou"))
            {
	            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
	            Debug.Log($"Loading model ({fileNameWithoutExtension})");
                byte[] buffer =  await ReadFileBytesAsync(filePath);
                var modelId = await _objectAnchorsService.AddObjectModelAsync(buffer);
                var modelSettings = objectModels.First(x => x.modelNameWithoutExtension == fileNameWithoutExtension);
                _modelSettings[modelId] = modelSettings;
            }
        }
        catch(UnauthorizedAccessException ex)
        {
            Debug.Log("access denied to objects 3d folder. Trying through path");
            StorageFolder objects3dAcc = await StorageFolder.GetFolderFromPathAsync(objects3d.Path);
            foreach(StorageFile file in await objects3dAcc.GetFilesAsync(CommonFileQuery.OrderByName))
            {
                if (Path.GetExtension(file.Name) == ".ou")
                {
	                var fileNameWithoutExtension = file.Name.Replace(".ou", "");
                    Debug.Log($"Loading model ({file.Path} {file.Name}");
                    byte[] buffer =  await ReadFileBytesAsync(file);
                    var modelId = await _objectAnchorsService.AddObjectModelAsync(buffer);
                    var modelSettings = objectModels.First(x => x.modelNameWithoutExtension == fileNameWithoutExtension);
                    _modelSettings[modelId] = modelSettings;
                }
            }
        }
        catch(Exception ex)
        {
            Debug.LogWarning("unexpected exception accessing objects 3d folder");
            Debug.LogException(ex);
        }
#endif
	}


	private void InitializeCallbacks()
	{
		_objectAnchorsService.ObjectAdded += HandleOnObjectAdded;
		_objectAnchorsService.ObjectUpdated += HandleOnObjectUpdated;
		_objectAnchorsService.ObjectRemoved += HandleOnObjectRemoved;
	}

	private void HandleOnObjectAdded(object sender, IObjectAnchorsServiceEventArgs e)
	{
		onAdded.Invoke(sender, e);
	}

	private void HandleOnObjectUpdated(object sender, IObjectAnchorsServiceEventArgs e)
	{
		onUpdated.Invoke(sender, e);
	}

	private void HandleOnObjectRemoved(object sender, IObjectAnchorsServiceEventArgs e)
	{
		onRemoved.Invoke(sender, e);
	}


	private async Task<SpatialGraphCoordinateSystem> GetCoordinateSystem()
	{
		_coordinateSystem = null;

#if SPATIALCOORDINATESYSTEM_API_PRESENT
		var worldOrigin = ObjectAnchorsWorldManager.WorldOrigin;
		if (worldOrigin != null)
			_coordinateSystem = await Task.Run(() => worldOrigin.TryToSpatialGraph());
		else
			throw new ObjectAnchorsException("Failed to get world origin");
#endif

		if (!_coordinateSystem.HasValue)
			throw new ObjectAnchorsException("Failed to spatially graph");

		return _coordinateSystem.Value;
	}

	private async Task<ObjectQuery> CreateQuery(Guid modelId)
	{
		// Get current coordinate system
		var coordinateSystem = await GetCoordinateSystem();

		// Create query
		var query = _objectAnchorsService.CreateObjectQuery(modelId, observationMode);

		var boundingBox = CreateSearchArea(modelId);

		// Add search area to query
		query.SearchAreas.Add(ObjectSearchArea.FromOrientedBox(coordinateSystem, boundingBox.ToSpatialGraph()));
		return query;
	}

	private ObjectAnchorsBoundingBox CreateSearchArea(Guid modelId)
	{
		// Adapt bounding box size to model size. Note that Extents.z is model's height.
		var modelBox = _objectAnchorsService.GetModelBoundingBox(modelId);
		Debug.Assert(modelBox.HasValue);
		float modelXYSize = new Vector2(modelBox.Value.Extents.x, modelBox.Value.Extents.y).magnitude;

		var cameraLocation = new ObjectAnchorsLocation
		{
			Position = _mainCamera.transform.position, Orientation = _mainCamera.transform.rotation,
		};

		var cameraForward = cameraLocation.Orientation * Vector3.forward;
		var estimatedTargetLocation = new ObjectAnchorsLocation
		{
			Position = cameraLocation.Position + cameraForward * searchAreaFarDistance * 0.5f,
			Orientation = Quaternion.Euler(0.0f, cameraLocation.Orientation.eulerAngles.y, 0.0f),
		};

		var boundingBox = new ObjectAnchorsBoundingBox
		{
			Center = estimatedTargetLocation.Position,
			Orientation = estimatedTargetLocation.Orientation,
			Extents = new Vector3(modelXYSize * searchAreaScaleFactor, modelBox.Value.Extents.z * searchAreaScaleFactor,
				modelXYSize * searchAreaScaleFactor),
		};
		return boundingBox;
	}

	private async void SearchAllObjects()
	{
		Debug.Log("Searching for objects...");
		var queryTasks = _objectAnchorsService.ModelIds.Select(CreateQuery);
		var queries = await Task.WhenAll(queryTasks);
		var results = await _objectAnchorsService.DetectObjectAsync(queries);

		Debug.Log($"Found {results.Count} results.");

		foreach (var result in results)
		{
			// Print debug information
			DebugPrintResult(result);

			// Create reference frame
			var objectAnchor = new GameObject($"ObjectAnchor_{result.ModelId}_{result.InstanceId}");
			if (result.Location != null)
			{
				var loc = result.Location.Value;
				objectAnchor.transform.position = loc.Position;
				objectAnchor.transform.rotation = loc.Orientation;
			}

			// Get useful stuff
			HandleTrackingResult(result, objectAnchor);
		}
	}

	private void HandleTrackingResult(IObjectAnchorsTrackingResult result, GameObject objectAnchor)
	{
		if (result.Location == null)
		{
			// Nothing to do
			return;
		}

		var location = result.Location.Value;

		// Instantiate visualization prefab
		var settings = _modelSettings[result.ModelId];
		if (settings.visualizationPrefab != null)
		{
			Debug.Log($"Creating visualization mesh for model {result.ModelId}");
			var prefabInstance = Instantiate(settings.visualizationPrefab, objectAnchor.transform);
			var originToCenterTransform = _objectAnchorsService.GetModelOriginToCenterTransform(result.ModelId);
			if (originToCenterTransform == null) return;
			var transform = originToCenterTransform.Value.inverse;
			prefabInstance.transform.localPosition = transform.GetPosition();
			prefabInstance.transform.localRotation = transform.rotation;
			prefabInstance.transform.localScale = transform.lossyScale;
		}
	}

	private void DebugPrintResult(IObjectAnchorsTrackingResult result)
	{
		Debug.Log(string.Join("\n",
			new[]
			{
				$"Tracking result for {result.ModelId} (instance: {result.InstanceId})",
				$"Position:      {result.Location?.Position}",
				$"Rotation:      {result.Location?.Orientation.eulerAngles}",
				$"Coverage:      {result.SurfaceCoverage}", $"Scale change:  {result.ScaleChange}",
				$"Tracking mode: {result.TrackingMode}", $"Last updated:  {result.LastUpdatedTime}"
			}));
	}
	
#if WINDOWS_UWP
    private async Task<byte[]> ReadFileBytesAsync(string filePath)
    {
        StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
        if (file == null)
        {
            return null; 
        }

        return await ReadFileBytesAsync(file);
    }    

    private async Task<byte[]> ReadFileBytesAsync(StorageFile file)
    {
        using (IRandomAccessStream stream = await file.OpenReadAsync())
        {
            using (var reader = new DataReader(stream.GetInputStreamAt(0)))
            {
                await reader.LoadAsync((uint)stream.Size);
                var bytes = new byte[stream.Size];
                reader.ReadBytes(bytes);
                return bytes;
            }
        }
    }
#endif
}