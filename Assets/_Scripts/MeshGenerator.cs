using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Esri;

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.IO.CoordinateSystems;

using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    public string filePath;
    public Vector2 mapCenter = new Vector2(0, 0);
    public Vector2 gameCenter = new Vector2(0, 0);
    public float XZScale = 1.0f;
    public float YScale = 1.0f;
    public float minWorldY = 0.0f;
    public bool usingTranslation = false;
    public bool usingSJSTK = true;

    private CoordinateTranslator ct;
    private ConcurrentDictionary<NetTopologySuite.Geometries.Coordinate, Vector3> cc = new ConcurrentDictionary<NetTopologySuite.Geometries.Coordinate, Vector3>();
    private CoordinateConverter _converter;
    private ICoordinateTransformation _transform;

    void Start()
    {
        _converter = new CoordinateConverter();
        _transform = _converter.CreateSJtskToWgs84Transformation();

        ct = new CoordinateTranslator(
            mapCenter,
            gameCenter,
            Mathf.Deg2Rad * 45,
            XZScale,
            YScale,
            (int)minWorldY
        );

        if (filePath.EndsWith(".shp"))
        {
            GenerateFromShapefile(filePath);
        }
    }

    private void GenerateFromShapefile(string filePath)
    {
        cc.Clear();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int vertexOffset = 0;
        int chunkIndex = 0;
        int maxVerticesPerChunk = 65535;

        var features = Shapefile.ReadAllFeatures(filePath);
        foreach (var feature in features)
        {
            if (feature.Geometry is MultiPolygon multiPolygon)
            {
                foreach (var polygon in multiPolygon.Geometries)
                {
                    if (polygon is Polygon)
                    {
                        AddPolygonToMesh((Polygon)polygon, vertices, triangles, ref vertexOffset);

                        if (vertices.Count >= maxVerticesPerChunk)
                        {
                            CreateMeshChunk(vertices, triangles, chunkIndex++);
                            vertices.Clear();
                            triangles.Clear();
                            vertexOffset = 0;
                        }
                    }
                }
            }
        }

        if (vertices.Count > 0)
        {
            CreateMeshChunk(vertices, triangles, chunkIndex);
            cc.Clear();
        }
    }

    private Vector3 TransformCoordinate(NetTopologySuite.Geometries.Coordinate coordinate)
    {
        return cc.GetOrAdd(coordinate, c =>
        {
            Vector3 result;
            if (usingTranslation)
            {
                Vector2 latLon;
                if (usingSJSTK)
                {
                    double[] sjtskPoint = { c.X, c.Y };
                    double[] wgs84Point = _transform.MathTransform.Transform(sjtskPoint);
                    latLon = new Vector2((float)wgs84Point[1], (float)wgs84Point[0]);
                }
                else
                {
                    latLon = new Vector2((float)c.Y, (float)c.X);
                }

                Vector2 xz = ct.LatLonToXZ(latLon.x, latLon.y);
                float y = ct.AltitudeToY((float)c.Z);
                result = new Vector3(xz.x, y, xz.y);
            }
            else
            {
                result = new Vector3((float)c.X * XZScale, (float)c.Z * YScale, (float)c.Y * XZScale);
            }
            return result;
        });
    }

    private void AddPolygonToMesh(Polygon polygon, List<Vector3> vertices, List<int> triangles, ref int vertexOffset)
    {
        var exteriorRing = polygon.ExteriorRing.Coordinates;
        var allVertices = new Vector3[exteriorRing.Length];

        Parallel.For(0, exteriorRing.Length, i =>
        {
            allVertices[i] = TransformCoordinate(exteriorRing[i]);
        });

        vertices.AddRange(allVertices);

        int vertexCount = allVertices.Length;
        for (int i = 1; i < vertexCount - 1; i++)
        {
            triangles.Add(vertexOffset);
            triangles.Add(vertexOffset + i + 1);
            triangles.Add(vertexOffset + i);
        }

        vertexOffset += vertexCount;
    }

    private void CreateMeshChunk(List<Vector3> vertices, List<int> triangles, int chunkIndex)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.Optimize();

        GameObject meshObject = new GameObject($"Mesh Chunk {chunkIndex}");
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;
        meshRenderer.material = new Material(Shader.Find("Standard"));
    }
}