#region

using System;
using System.IO;
using System.Linq;
using Microsoft.Azure.ObjectAnchors;
using Microsoft.Azure.ObjectAnchors.Unity;
using UnityEngine;
using UnityEngine.Rendering;
using Vector3 = System.Numerics.Vector3;

#endregion

public class MeshLoader : MonoBehaviour
{
	public string ModelPath;

	private async void Start()
	{
		Debug.Log($"Loading model from: '{ModelPath}'");
		var modelBytes = File.ReadAllBytes(ModelPath);

		using (var model = await ObjectModel.LoadAsync(modelBytes))
		{
			var modelVertices = new Vector3[model.VertexCount];
			model.GetVertexPositions(modelVertices);

			// Counter clock wise
			var modelIndicesCcw = new uint[model.TriangleIndexCount];
			model.GetTriangleIndices(modelIndicesCcw);

			var modelNormals = new Vector3[model.VertexCount];
			model.GetVertexNormals(modelNormals);

			gameObject.AddComponent<MeshFilter>().mesh = LoadMesh(modelVertices, modelNormals, modelIndicesCcw);
		}
	}

	public static Mesh LoadMesh(Vector3[] modelVertices, Vector3[] modelNormals, uint[] modelIndicesCcw)
	{
		var mesh = new Mesh();
		LoadMesh(mesh, new MeshData(modelVertices, modelNormals, modelIndicesCcw));
		return mesh;
	}

	public static void LoadMesh(Mesh mesh, MeshData meshData)
	{
		mesh.Clear();

		// We need to flip handedness of vertices and modify triangle list to
		// clockwise winding in order to be usable in Unity.

		mesh.vertices = meshData.vertices;
		if (mesh.vertices.Length > ushort.MaxValue) mesh.indexFormat = IndexFormat.UInt32;

		mesh.SetIndices(meshData.indices, meshData.topology, 0);
		mesh.normals = meshData.normals;
		mesh.RecalculateBounds();
	}

	public static void AddMesh(GameObject gameObject, IObjectAnchorsService service, Guid modelId)
	{
		gameObject.AddComponent<MeshFilter>().mesh = LoadMesh(service.GetModelVertexPositions(modelId),
			service.GetModelVertexNormals(modelId), new uint[] { });
	}

	public struct MeshData
	{
		public readonly UnityEngine.Vector3[] vertices;
		public readonly UnityEngine.Vector3[] normals;
		public readonly int[] indices;
		public readonly MeshTopology topology;

		public MeshData(Vector3[] modelVertices, Vector3[] modelNormals, uint[] modelIndicesCcw)
		{
			vertices = modelVertices.Select(v => v.ToUnity()).ToArray();

			if (modelIndicesCcw.Length > 2)
			{
				// Clock wise
				var modelIndicesCw = indices = new int[modelIndicesCcw.Length];
				Enumerable.Range(0, modelIndicesCcw.Length).Select(i =>
					i % 3 == 0 ? modelIndicesCw[i] = (int)modelIndicesCcw[i] :
					i % 3 == 1 ? modelIndicesCw[i] = (int)modelIndicesCcw[i + 1] :
					modelIndicesCw[i] = (int)modelIndicesCcw[i - 1]).ToArray();

				topology = MeshTopology.Triangles;
			}
			else
			{
				indices = Enumerable.Range(0, modelVertices.Length).ToArray();
				topology = MeshTopology.Points;
			}

			normals = modelNormals.Select(v => v.ToUnity()).ToArray();
		}
	}
}