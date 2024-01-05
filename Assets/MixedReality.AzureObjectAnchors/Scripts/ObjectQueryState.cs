#if WINDOWS_UWP || DOTNETWINRT_PRESENT
#define SPATIALCOORDINATESYSTEM_API_PRESENT
#endif

#region

using System;
using System.Threading.Tasks;
using Microsoft.Azure.ObjectAnchors;
using Microsoft.Azure.ObjectAnchors.Unity;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

#endregion

public class ObjectQueryState : MonoBehaviour
{
	public Material EnvironmentMaterial;
	private DateTime? _lastMeshUpdateTime;
	private MeshFilter _meshFilter;
	private bool _updateInProgress;
	public ObjectQuery Query;

	// Start is called before the first frame update
	private void Start()
	{
		_meshFilter = gameObject.AddComponent<MeshFilter>();
		_meshFilter.mesh = new Mesh();
		gameObject.AddComponent<MeshRenderer>().sharedMaterial = EnvironmentMaterial;
	}

	// Update is called once per frame
	private async void Update()
	{
		if (!_updateInProgress && Query != null && EnvironmentMaterial != null && (!_lastMeshUpdateTime.HasValue ||
			    DateTime.Now - _lastMeshUpdateTime.Value > TimeSpan.FromSeconds(2)))
		{
			_updateInProgress = true;
			var observation =
				await Query.ComputeLatestEnvironmentObservationAsync(EnvironmentObservationTopology.PointCloud);

			MeshLoader.MeshData? meshData = null;
			ObjectAnchorsLocation? observationLocation = null;

			await Task.Run(() => {
				var vertexPositions = new Vector3[observation.VertexCount];
				var vertexNormals = new Vector3[vertexPositions.Length];
				var triangleIndices = new uint[observation.TriangleIndexCount];
				observation.GetVertexPositions(vertexPositions);
				observation.GetVertexNormals(vertexNormals);
				observation.GetTriangleIndices(triangleIndices);
				meshData = new MeshLoader.MeshData(vertexPositions, vertexNormals, triangleIndices);

#if SPATIALCOORDINATESYSTEM_API_PRESENT
				observationLocation = observation.Origin.ToSpatialCoordinateSystem()
					.TryGetTransformTo(ObjectAnchorsWorldManager.WorldOrigin)?.ToUnityLocation();
#endif
			});


			if (observationLocation.HasValue && meshData.HasValue)
			{
				MeshLoader.LoadMesh(_meshFilter.mesh, meshData.Value);
				transform.SetPositionAndRotation(observationLocation.Value.Position,
					observationLocation.Value.Orientation);
			}

			_lastMeshUpdateTime = DateTime.Now;
			_updateInProgress = false;
		}
	}

	private void OnDestroy()
	{
		Query?.Dispose();
		Query = null;
	}
}