// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#if WINDOWS_UWP || DOTNETWINRT_PRESENT
#define SPATIALCOORDINATESYSTEM_API_PRESENT
#endif

#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ObjectAnchors;
using Microsoft.Azure.ObjectAnchors.SpatialGraph;
using Microsoft.Azure.ObjectAnchors.Unity;
using UI;
using UnityEditor;
using UnityEngine;
using Utils;

#endregion

#if WINDOWS_UWP
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Storage.Search;
#endif

namespace ObjectTracking
{
	public class ObjectTrackingManager : MonoBehaviour
	{
		private struct InstanceState
		{
			public Vector3? Position;
			public Quaternion? Rotation;
			public float SurfaceCoverage;
		}

		public enum SearchAreaKind
		{
			Box,
			FieldOfView,
			Sphere
		}

		[Tooltip("Far distance in meter of object search frustum.")]
		public float searchFrustumFarDistance = 4.0f;

		[Tooltip("Horizontal field of view in degrees of object search frustum.")]
		public float searchFrustumHorizontalFovInDegrees = 75.0f;

		[Tooltip("Aspect ratio (horizontal / vertical) of object search frustum.")]
		public float searchFrustumAspectRatio = 1.0f;

		[Tooltip("Scale on model size to deduce object search area.")]
		public float searchAreaScaleFactor = 2.0f;

		[Tooltip("Search area shape.")] public SearchAreaKind searchAreaShape = SearchAreaKind.Box;

		[Tooltip("Observation mode.")] public ObjectObservationMode observationMode = ObjectObservationMode.Ambient;

		// [Tooltip("Tracking mode.")]
		// public ObjectInstanceTrackingMode trackingMode = ObjectInstanceTrackingMode.LowLatencyCoarsePosition;

		[Tooltip("Show environment observations.")]
		public bool showEnvironmentObservations;

		[Tooltip("Search single vs. multiple instances.")]
		public bool searchSingleInstance = true;

		[Tooltip(@"Sentinel file in `Application\LocalCache` folder to enable capturing diagnostics.")]
		public string diagnosticsSentinelFilename = "debug";

		[Tooltip("Material used to render a wire frame.")]
		public Material wireframeMaterial;

		[Tooltip("Material used to render the environment.")]
		public Material environmentMaterial;

		[Tooltip("Prefab used to determine placement of detected objects.")]
		public GameObject multiAnchorPlacementPrefab;

		[Tooltip("Prefab used to visualize mesh")]
		public GameObject visualizationPrefab;

		[Tooltip("Fraction of recognized model mesh to be considered detected")]
		public float minSurfaceCoverage = 0.5f;

		[Tooltip("Maximum expected deviation from vertical orientation in degrees")]
		public float expectedMaxVerticalOrientationInDegrees;

		[Tooltip("Minimum distance difference for it to be considered a change in position")]
		public float positionChangeThreshold = 0.1f;

		[Tooltip("Minimum rotation difference in degrees to be considered a change in position")]
		public float rotationChangeThreshold = 5.0f;

		[Tooltip("Tracking mode for new instances")]
		public ObjectInstanceTrackingMode trackingMode = ObjectInstanceTrackingMode.LowLatencyCoarsePosition;


		public ObjectManagerMenu objectManagerMenu;


		/// <summary>
		///     Flag to indicate the detection operation, 0 - in detection, 1 - detection completed.
		/// </summary>
		private int _detectionCompleted = 1;

		/// <summary>
		///     Cached camera instance.
		/// </summary>
		private Camera _cachedCameraMain;

		/// <summary>
		///     Object Anchors service object.
		/// </summary>
		private IObjectAnchorsService _objectAnchorsService;

		/// <summary>
		///     Placement of each object instance with guid as instance id.
		/// </summary>
		private readonly Dictionary<Guid, MultiAnchorObjectPlacement> _objectPlacements = new();

		private readonly Dictionary<Guid, GameObject> _visualizationMeshes = new();

		/// <summary>
		///     Query associated with each model with guid as model id.
		/// </summary>
		private Dictionary<Guid, ObjectQueryState> _objectQueries = new();

		private readonly Dictionary<Guid, InstanceState> _prevInstanceStates = new();

		private readonly Dictionary<Guid, string> _modelIdToName = new();

		private Dictionary<Guid, ObjectQueryState> InitializeObjectQueries()
		{
			var objectQueries = new Dictionary<Guid, ObjectQueryState>();

			foreach (var modelId in _objectAnchorsService.ModelIds)
			{
				//
				// Create a query and set the parameters.
				//

				var queryState =
					new GameObject($"ObjectQueryState for model {modelId}").AddComponent<ObjectQueryState>();
				queryState.Query = _objectAnchorsService.CreateObjectQuery(modelId, observationMode);
				if (showEnvironmentObservations) queryState.EnvironmentMaterial = environmentMaterial;

				objectQueries.Add(modelId, queryState);
			}

			return objectQueries;
		}

		private enum ObjectAnchorsServiceEventKind
		{
			/// <summary>
			///     Attempted to detect objects.
			/// </summary>
			DetectionAttempted,

			/// <summary>
			///     An new object is found for the first time.
			/// </summary>
			Added,

			/// <summary>
			///     State of a tracked object changed.
			/// </summary>
			Updated,

			/// <summary>
			///     An object lost tracking.
			/// </summary>
			Removed
		}

		private class ObjectAnchorsServiceEvent
		{
			public IObjectAnchorsServiceEventArgs Args;
			public ObjectAnchorsServiceEventKind Kind;
		}

		/// <summary>
		///     A queue to cache the Object Anchors events.
		///     Events are added in the callbacks from Object Anchors service, then consumed in the Update method.
		/// </summary>
		private readonly ConcurrentQueue<ObjectAnchorsServiceEvent> _objectAnchorsEventQueue = new();


		/// <summary>
		///     Returns true if diagnostics capture is enabled.
		/// </summary>
		public bool IsDiagnosticsCaptureEnabled =>
			File.Exists(Path.Combine(Application.persistentDataPath.Replace('/', '\\'), diagnosticsSentinelFilename));

		private void Awake()
		{
			_objectAnchorsService = ObjectAnchorsService.GetService();

			AddObjectAnchorsListeners();
		}

		private async void Start()
		{
			try
			{
				await _objectAnchorsService.InitializeAsync();
			}
			catch (ArgumentException ex)
			{
#if WINDOWS_UWP
				string message = ex.Message;
				Windows.Foundation.IAsyncOperation<Windows.UI.Popups.IUICommand> dialog = null;
				UnityEngine.WSA.Application.InvokeOnUIThread(() => dialog = new Windows.UI.Popups.MessageDialog(message, "Invalid account information").ShowAsync(), true);
				await dialog;
#elif UNITY_EDITOR
				EditorUtility.DisplayDialog("Invaild account information", ex.Message, "OK");
#endif // WINDOWS_UWP
				throw ex;
			}

			_objectAnchorsService.Pause();
			Debug.Log("Object search initialized.");

			foreach (var file in FileHelper.GetFilesInDirectory(Application.persistentDataPath, "*.ou"))
			{
				Debug.Log($"Loading model ({Path.GetFileNameWithoutExtension(file)})");

				await _objectAnchorsService.AddObjectModelAsync(file.Replace('/', '\\'));
			}


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
					var modelName = Path.GetFileNameWithoutExtension(filePath);
					Debug.Log($"Loading model ({modelName})");
					byte[] buffer = await ReadFileBytesAsync(filePath);
					var modelId = await _objectAnchorsService.AddObjectModelAsync(buffer);
					_modelIdToName[modelId] = modelName;
				}
			}
			catch (UnauthorizedAccessException ex)
			{
				Debug.Log("access denied to objects 3d folder. Trying through path");
				StorageFolder objects3dAcc = await StorageFolder.GetFolderFromPathAsync(objects3d.Path);
				foreach (StorageFile file in await objects3dAcc.GetFilesAsync(CommonFileQuery.OrderByName))
				{
					if (Path.GetExtension(file.Name) == ".ou")
					{
						var modelName = Path.GetFileNameWithoutExtension(file.Name);
						Debug.Log($"Loading model ({modelName})");
						byte[] buffer = await ReadFileBytesAsync(file);
						var modelId = await _objectAnchorsService.AddObjectModelAsync(buffer);
						_modelIdToName[modelId] = modelName;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("unexpected exception accessing objects 3d folder");
				Debug.LogException(ex);
			}
#endif

			_objectQueries = InitializeObjectQueries();

			if (IsDiagnosticsCaptureEnabled)
			{
				Debug.Log("Start capture diagnostics.");

				_objectAnchorsService.StartDiagnosticsSession();
			}

			Debug.Log($"ObjectAnchorsService status: {_objectAnchorsService.Status}");
		}

		public void StartSearch()
		{
			_objectAnchorsService.Resume();
			InGameNotification.SetNotification("Search started", 4);
		}

		public void StopSearch()
		{
			_objectAnchorsService.Pause();
			InGameNotification.SetNotification("Search stopped", 4);
		}

		public void ToggleSearch()
		{
			if (_objectAnchorsService.Status == ObjectAnchorsServiceStatus.Paused) StartSearch();
			else StopSearch();
		}

		private async void OnDestroy()
		{
			_objectAnchorsService.Pause();

			await _objectAnchorsService.StopDiagnosticsSessionAsync();

			RemoveObjectAnchorsListeners();

			lock (_objectQueries)
			{
				foreach (var query in _objectQueries)
					if (query.Value != null)
						Destroy(query.Value.gameObject);

				_objectQueries.Clear();
			}

			_objectAnchorsService.Dispose();
		}

		private void Update()
		{
			// Process current events.
			HandleObjectAnchorsServiceEvent();

			// Optionally kick off a detection if no object found yet.
			TrySearchObject();
		}

		private void AddObjectAnchorsListeners()
		{
			_objectAnchorsService.RunningChanged += ObjectAnchorsService_RunningChanged;
			_objectAnchorsService.ObjectAdded += ObjectAnchorsService_ObjectAdded;
			_objectAnchorsService.ObjectUpdated += ObjectAnchorsService_ObjectUpdated;
			_objectAnchorsService.ObjectRemoved += ObjectAnchorsService_ObjectRemoved;
		}

		private void RemoveObjectAnchorsListeners()
		{
			_objectAnchorsService.RunningChanged -= ObjectAnchorsService_RunningChanged;
			_objectAnchorsService.ObjectAdded -= ObjectAnchorsService_ObjectAdded;
			_objectAnchorsService.ObjectUpdated -= ObjectAnchorsService_ObjectUpdated;
			_objectAnchorsService.ObjectRemoved -= ObjectAnchorsService_ObjectRemoved;
		}

		private void ObjectAnchorsService_RunningChanged(object sender, ObjectAnchorsServiceStatus status)
		{
			Debug.Log($"Object search {status}");
		}

		private void ObjectAnchorsService_ObjectAdded(object sender, IObjectAnchorsServiceEventArgs args)
		{
			// This event handler is called from a non-UI thread.
			_objectAnchorsEventQueue.Enqueue(new ObjectAnchorsServiceEvent
			{
				Kind = ObjectAnchorsServiceEventKind.Added, Args = args
			});
		}

		private void ObjectAnchorsService_ObjectUpdated(object sender, IObjectAnchorsServiceEventArgs args)
		{
			// This event handler is called from a non-UI thread.
			_objectAnchorsEventQueue.Enqueue(new ObjectAnchorsServiceEvent
			{
				Kind = ObjectAnchorsServiceEventKind.Updated, Args = args
			});
		}

		private void ObjectAnchorsService_ObjectRemoved(object sender, IObjectAnchorsServiceEventArgs args)
		{
			// This event handler is called from a non-UI thread.
			_objectAnchorsEventQueue.Enqueue(new ObjectAnchorsServiceEvent
			{
				Kind = ObjectAnchorsServiceEventKind.Removed, Args = args
			});
		}

		private void HandleObjectAnchorsServiceEvent()
		{
			Func<IObjectAnchorsServiceEventArgs, string> eventArgsFormatter = args => {
				return
					$"[{args.LastUpdatedTime.ToLongTimeString()}] ${TextLogger.Truncate(args.InstanceId.ToString(), 5)}";
			};

			ObjectAnchorsServiceEvent @event;
			while (_objectAnchorsEventQueue.TryDequeue(out @event))
				switch (@event.Kind)
				{
					case ObjectAnchorsServiceEventKind.DetectionAttempted:
					{
						Debug.Log("detection attempted");
						break;
					}
					case ObjectAnchorsServiceEventKind.Added:
					{
						Debug.Log($"{eventArgsFormatter(@event.Args)} added, " +
						          $"coverage {@event.Args.SurfaceCoverage.ToString("0.0000")}, " +
						          $"position {@event.Args.Location?.Position}, " +
						          $"rotation {@event.Args.Location?.Orientation}");
						_objectAnchorsService.SetObjectInstanceTrackingMode(@event.Args.InstanceId, trackingMode);
						objectManagerMenu.AddObject(@event.Args.ModelId, @event.Args.InstanceId,
							_modelIdToName[@event.Args.ModelId]);
						DrawBoundingBox(@event.Args);
						PlaceVisualizationMesh(@event.Args, false);
						break;
					}
					case ObjectAnchorsServiceEventKind.Updated:
					{
						Debug.Log($"{eventArgsFormatter(@event.Args)} updated, " +
						          $"coverage {@event.Args.SurfaceCoverage.ToString("0.0000")}, " +
						          $"position {@event.Args.Location?.Position}, " +
						          $"rotation {@event.Args.Location?.Orientation}");
						DrawBoundingBox(@event.Args);
						PlaceVisualizationMesh(@event.Args, false);
						break;
					}
					case ObjectAnchorsServiceEventKind.Removed:
					{
						Debug.Log($"{eventArgsFormatter(@event.Args)} removed");

						var placement = _objectPlacements[@event.Args.InstanceId];
						_objectPlacements.Remove(@event.Args.InstanceId);

						Destroy(placement.gameObject);
						objectManagerMenu.RemoveObject(@event.Args.ModelId);

						break;
					}
				}
		}

		public void ResetObject(Guid modelId, Guid instanceId)
		{
			_objectAnchorsService.RemoveObjectInstance(instanceId);

			_visualizationMeshes.Remove(modelId, out var mesh);
			Destroy(mesh);
			_prevInstanceStates.Remove(modelId);
		}

		private void PlaceVisualizationMesh(IObjectAnchorsServiceEventArgs instance, bool replace)
		{
			// If replace is false:
			// - If mesh not yet created, create and add to dict, update positions
			// - Else, just update position
			// If replace is true:
			// - If mesh not yet created, create and add to dict, update positions
			// - If mesh already in dict, delete current mesh, create new and replace, update positions
			var curInstanceState = new InstanceState
			{
				Position = instance.Location?.Position,
				Rotation = instance.Location?.Orientation,
				SurfaceCoverage = instance.SurfaceCoverage
			};
			GameObject visualizationMesh;
			var found = _visualizationMeshes.TryGetValue(instance.ModelId, out visualizationMesh);
			var isNew = false;
			if (!found || replace)
			{
				if (replace && found)
					// If replace, delete old mesh
					Destroy(visualizationMesh);

				visualizationMesh = Instantiate(visualizationPrefab);
				isNew = true;
				_visualizationMeshes[instance.ModelId] = visualizationMesh;
				_prevInstanceStates[instance.ModelId] = curInstanceState;

				var modelSwitcher = visualizationMesh.GetComponentInChildren<ModelSwitcher>();
				modelSwitcher.SetModel(_modelIdToName[instance.ModelId]);
			}

			// Check if should update position
			var prevInstanceState = _prevInstanceStates[instance.ModelId];

			var updateReason = "";

			bool ShouldUpdate()
			{
				// If it's new update in any case
				updateReason = "new";
				if (isNew) return true;
				// Cannot compute previous state if one of these is null, don't update
				updateReason = "did not update due to null";
				if (prevInstanceState.Position is null || curInstanceState.Position is null ||
				    prevInstanceState.Rotation is null || curInstanceState.Rotation is null) return false;
				// If we have a better tracking, update
				updateReason = "surface coverage";
				if (prevInstanceState.SurfaceCoverage < curInstanceState.SurfaceCoverage) return true;
				// Position changed enough, update
				var positionChange = (prevInstanceState.Position.Value - curInstanceState.Position.Value).magnitude;
				updateReason = $"position change: {positionChange}";
				if (positionChange > positionChangeThreshold) return true;
				// Rotation changed enough, update
				var rotationChange =
					Quaternion.Angle(prevInstanceState.Rotation.Value, curInstanceState.Rotation.Value);
				updateReason = $"rotation change: {rotationChange}";
				if (rotationChange > rotationChangeThreshold) return true;
				// No other conditions, do not update
				updateReason = "no update necessary";
				return false;
			}

			if (!ShouldUpdate())
			{
				Debug.Log($"Did not update, reason: {updateReason}");
				return;
			}

			Debug.Log(
				$"{instance.InstanceId}: Updating visualization prefab location, reason: {updateReason}. ({Time.time})");

			// Update position
			var modelOrigin = visualizationMesh.GetComponentInChildren<ObjectOriginTransform>().transform;
			var smoothTransform = visualizationMesh.GetComponent<SmoothTransform>();


			// Set visualizationMesh position to tracked location
			Debug.Assert(instance.Location.HasValue);
			var loc = instance.Location.Value;
			smoothTransform.SetPosition(loc.Position, isNew);
			smoothTransform.SetRotation(loc.Orientation, isNew);

			// Apply center to origin transform to modelOrigin
			var originToCenterTransform = _objectAnchorsService.GetModelOriginToCenterTransform(instance.ModelId);
			Debug.Assert(originToCenterTransform.HasValue);
			var t = originToCenterTransform.Value;
			modelOrigin.transform.localPosition = t.GetPosition();
			modelOrigin.transform.localRotation =
				t.rotation * Quaternion.Euler(0, 180f, 0); // Rotate by 180 deg for some reason
			modelOrigin.transform.localScale = t.lossyScale;

			_prevInstanceStates[instance.ModelId] = curInstanceState;
		}

		private void DrawBoundingBox(IObjectAnchorsServiceEventArgs instance)
		{
			MultiAnchorObjectPlacement placement;
			if (!_objectPlacements.TryGetValue(instance.InstanceId, out placement))
			{
				var boundingBox = _objectAnchorsService.GetModelBoundingBox(instance.ModelId);
				Debug.Assert(boundingBox.HasValue);

				placement = Instantiate(multiAnchorPlacementPrefab).GetComponent<MultiAnchorObjectPlacement>();

				var bbox = placement.ModelSpaceContent.AddComponent<WireframeBoundingBox>();
				bbox.UpdateBounds(boundingBox.Value.Center,
					Vector3.Scale(boundingBox.Value.Extents, instance.ScaleChange), boundingBox.Value.Orientation,
					wireframeMaterial);

				var mesh = new GameObject("Model Mesh");
				mesh.AddComponent<MeshRenderer>().sharedMaterial = wireframeMaterial;
				mesh.transform.SetParent(placement.ModelSpaceContent.transform, false);
				MeshLoader.AddMesh(mesh, _objectAnchorsService, instance.ModelId);

				_objectPlacements.Add(instance.InstanceId, placement);
			}

			if (instance.SurfaceCoverage > placement.SurfaceCoverage || !instance.Location.HasValue)
				placement.UpdatePlacement(instance);
		}

		private void TrySearchObject()
		{
			if (_objectAnchorsService.Status == ObjectAnchorsServiceStatus.Paused) return;
			if (Interlocked.CompareExchange(ref _detectionCompleted, 0, 1) == 1)
			{
				if (_cachedCameraMain == null) _cachedCameraMain = Camera.main;

				var cameraLocation = new ObjectAnchorsLocation
				{
					Position = _cachedCameraMain.transform.position,
					Orientation = _cachedCameraMain.transform.rotation
				};

#if SPATIALCOORDINATESYSTEM_API_PRESENT
				var coordinateSystem = ObjectAnchorsWorldManager.WorldOrigin;

				Task.Run(async () => {
					try
					{
						await DetectObjectAsync(coordinateSystem.TryToSpatialGraph(), cameraLocation);
					}
					catch (Exception ex)
					{
						UnityEngine.WSA.Application.InvokeOnAppThread(() => { Debug.Log($"Detection failed. Exception message: {ex.ToString()}"); }, false);
					}

					Interlocked.CompareExchange(ref _detectionCompleted, 1, 0);
				});
#endif
			}
		}

		private Task DetectObjectAsync(SpatialGraphCoordinateSystem? coordinateSystem,
			ObjectAnchorsLocation cameraLocation)
		{
			//
			// Coordinate system may not be available at this time, try it later.
			//

			if (!coordinateSystem.HasValue) return Task.CompletedTask;

			//
			// Get camera location and coordinate system.
			//

			var cameraForward = cameraLocation.Orientation * Vector3.forward;
			var estimatedTargetLocation = new ObjectAnchorsLocation
			{
				Position = cameraLocation.Position + cameraForward * searchFrustumFarDistance * 0.5f,
				Orientation = Quaternion.Euler(0.0f, cameraLocation.Orientation.eulerAngles.y, 0.0f)
			};

			//
			// Remove detected objects far away from the camera.
			//

			foreach (var instance in _objectAnchorsService.TrackingResults)
			{
				var location = instance.Location;
				if (location.HasValue)
				{
					var modelBbox = _objectAnchorsService.GetModelBoundingBox(instance.ModelId);
					Debug.Assert(modelBbox.HasValue);

					// Compute the coordinate of instance bounding box center in Unity world.
					var instancePosition =
						location.Value.Position + location.Value.Orientation * modelBbox.Value.Center;

					var offset = instancePosition - cameraLocation.Position;

					if (offset.magnitude > searchFrustumFarDistance * 1.5f)
						_objectAnchorsService.RemoveObjectInstance(instance.InstanceId);
				}
			}

			//
			// Detect object(s) in field of view, bounding box, or sphere.
			//

			var objectQueries = new List<ObjectQuery>();

			var trackingResults = _objectAnchorsService.TrackingResults;

			lock (_objectQueries)
			{
				foreach (var objectQuery in _objectQueries)
				{
					var modelId = objectQuery.Key;
					var query = objectQuery.Value.Query;

					//
					// Optionally skip a model detection if an instance is already found.
					//

					if (searchSingleInstance)
						if (trackingResults.Where(r => r.ModelId == modelId).Count() > 0)
							continue;

					var modelBox = _objectAnchorsService.GetModelBoundingBox(modelId);
					Debug.Assert(modelBox.HasValue);

					query.SearchAreas.Clear();
					switch (searchAreaShape)
					{
						case SearchAreaKind.Box:
						{
							// Adapt bounding box size to model size. Note that Extents.z is model's height.
							var modelXYSize = new Vector2(modelBox.Value.Extents.x, modelBox.Value.Extents.y).magnitude;

							var boundingBox = new ObjectAnchorsBoundingBox
							{
								Center = estimatedTargetLocation.Position,
								Orientation = estimatedTargetLocation.Orientation,
								Extents = new Vector3(modelXYSize * searchAreaScaleFactor,
									modelBox.Value.Extents.z * searchAreaScaleFactor,
									modelXYSize * searchAreaScaleFactor)
							};

							query.SearchAreas.Add(ObjectSearchArea.FromOrientedBox(coordinateSystem.Value,
								boundingBox.ToSpatialGraph()));
							break;
						}

						case SearchAreaKind.FieldOfView:
						{
							var fieldOfView = new ObjectAnchorsFieldOfView
							{
								Position = cameraLocation.Position,
								Orientation = cameraLocation.Orientation,
								FarDistance = searchFrustumFarDistance,
								HorizontalFieldOfViewInDegrees = searchFrustumHorizontalFovInDegrees,
								AspectRatio = searchFrustumAspectRatio
							};

							query.SearchAreas.Add(ObjectSearchArea.FromFieldOfView(coordinateSystem.Value,
								fieldOfView.ToSpatialGraph()));
							break;
						}

						case SearchAreaKind.Sphere:
						{
							// Adapt sphere radius to model size.
							var modelDiagonalSize = modelBox.Value.Extents.magnitude;

							var sphere = new ObjectAnchorsSphere
							{
								Center = estimatedTargetLocation.Position,
								Radius = modelDiagonalSize * 0.5f * searchAreaScaleFactor
							};

							query.SearchAreas.Add(ObjectSearchArea.FromSphere(coordinateSystem.Value,
								sphere.ToSpatialGraph()));
							break;
						}
					}

					query.MinSurfaceCoverage = minSurfaceCoverage;
					query.ExpectedMaxVerticalOrientationInDegrees = expectedMaxVerticalOrientationInDegrees;

					objectQueries.Add(query);
				}
			}

			//
			// Pause a while if detection is not required.
			//

			if (objectQueries.Count == 0)
			{
				Thread.Sleep(100);

				return Task.CompletedTask;
			}

			//
			// Run detection.
			//

			// Add event to the queue.
			_objectAnchorsEventQueue.Enqueue(new ObjectAnchorsServiceEvent
			{
				Kind = ObjectAnchorsServiceEventKind.DetectionAttempted, Args = null
			});

			return _objectAnchorsService.DetectObjectAsync(objectQueries.ToArray());
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
}