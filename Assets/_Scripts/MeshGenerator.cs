using System.Collections;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Esri;

using ProjNet.CoordinateSystems.Transformations;

using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    [Header("Shapefile Generation")]
    public string filePath;
    public GeospatialManager geom;
    public bool usingTranslation = true;
    public bool usingSJSTK = true;

    [Header("Uniform Centroid Grid Chunking")]
    public bool useUniformCentroidChunking = true;
    public int N = 10;

    [Header("Optimization")]
    public bool useLODCulling = true;
    public float screenRelativeTransitionHeight = 0.5f;

    [Header("Visual Representation")]
    public Material material;

    protected CoordinateConverter _converter;
    protected ICoordinateTransformation _transform;

    protected Geometry gtype = null;
    protected ConcurrentDictionary<NetTopologySuite.Geometries.Coordinate, Vector3> cc;

    protected const int MaxVerticesPerChunk = 65535;

    protected virtual void Start()
    {
        _converter = new CoordinateConverter();
        _transform = _converter.CreateSJtskToWgs84Transformation();
        cc = new ConcurrentDictionary<NetTopologySuite.Geometries.Coordinate, Vector3>();

        if (material == null) material = new Material(Shader.Find("Standard"));
    }

    protected virtual void Update()
    {
        // Update logic if needed
    }

    public void GenerateMesh(string filePath, bool areaChunked)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("File path is empty. Please set a valid shapefile path.");
            return;
        }

        if (areaChunked) UniformChunkGenerate(filePath);
        else SimpleGenerate(filePath);
    }

    public IEnumerator GenerateMeshCo(string filePath, bool areaChunked)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("File path is empty. Please set a valid shapefile path.");
            yield break;
        }

        yield return SimpleGenerateCo(filePath);
    }

    protected void SimpleGenerate(string filePath)
    {
        cc.Clear();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int vertexOffset = 0;
        int chunkIndex = 0;

        var features = Shapefile.ReadAllFeatures(filePath);
        foreach (var feature in features)
        {
            if (gtype == null) gtype = feature.Geometry;

            if (feature.Geometry is MultiPolygon multiPolygon)
            {
                foreach (var polygon in multiPolygon.Geometries)
                {
                    if (polygon is Polygon)
                    {
                        AddPolygonToMesh((Polygon)polygon, vertices, triangles, ref vertexOffset);

                        if (vertices.Count > MaxVerticesPerChunk)
                        {
                            CreateMeshChunk(vertices, triangles, chunkIndex++);
                            vertices.Clear();
                            triangles.Clear();
                            vertexOffset = 0;
                        }
                    }
                }
            }

            if (feature.Geometry is MultiLineString multiLineString)
            {
                foreach (var lineString in multiLineString.Geometries)
                {
                    if (lineString is LineString)
                    {
                        // LineString uses LineRenderer to display.
                        AddLineStringToMesh((LineString)lineString, vertices, triangles, ref vertexOffset);
                        CreateLineChunk(vertices, chunkIndex++);
                        vertices.Clear();
                        triangles.Clear();
                        vertexOffset = 0;
                    }
                }
            }
        }

        // Left over vertices after chunking
        if (vertices.Count > 0)
        {
            if (gtype is MultiPolygon)
            {
                CreateMeshChunk(vertices, triangles, chunkIndex);
            }

            if (gtype is MultiLineString)
            {
                CreateLineChunk(vertices, chunkIndex);
            }
            cc.Clear();
        }
    }

    protected IEnumerator SimpleGenerateCo(string filePath)
    {
        Debug.Log("Generating mesh from shapefile: " + filePath);
        cc.Clear();
        Debug.Log("Cleared coordinate converter cache.");
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int vertexOffset = 0;
        int chunkIndex = 0;

        var features = Shapefile.ReadAllFeatures(filePath);
        yield return null;

        foreach (var feature in features)
        {
            if (gtype == null) gtype = feature.Geometry;

            if (feature.Geometry is MultiPolygon multiPolygon)
            {
                foreach (var polygon in multiPolygon.Geometries)
                {
                    if (polygon is Polygon)
                    {
                        AddPolygonToMesh((Polygon)polygon, vertices, triangles, ref vertexOffset);

                        if (vertices.Count > MaxVerticesPerChunk)
                        {
                            CreateMeshChunk(vertices, triangles, chunkIndex++);
                            vertices.Clear();
                            triangles.Clear();
                            vertexOffset = 0;
                        }
                        yield return null;
                    }
                }
            }

            if (feature.Geometry is MultiLineString multiLineString)
            {
                foreach (var lineString in multiLineString.Geometries)
                {
                    if (lineString is LineString)
                    {
                        // LineString uses LineRenderer to display.
                        AddLineStringToMesh((LineString)lineString, vertices, triangles, ref vertexOffset);
                        CreateLineChunk(vertices, chunkIndex++);
                        vertices.Clear();
                        triangles.Clear();
                        vertexOffset = 0;
                    }
                    yield return null;
                }
            }
        }

        // Left over vertices after chunking
        if (vertices.Count > 0)
        {
            if (gtype is MultiPolygon)
            {
                CreateMeshChunk(vertices, triangles, chunkIndex);
            }

            if (gtype is MultiLineString)
            {
                CreateLineChunk(vertices, chunkIndex);
            }
            cc.Clear();
            yield return null;
        }
    }

    protected void UniformChunkGenerate(string filePath)
    {
        cc.Clear();

        var features = Shapefile.ReadAllFeatures(filePath);

        // Generate Bounding Box
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (var feature in features)
        {
            if (gtype == null) gtype = feature.Geometry;

            if (feature.Geometry is MultiPolygon multiPolygon)
            {
                foreach (var polygon in multiPolygon.Geometries)
                {
                    if (polygon is Polygon)
                    {
                        foreach (var coordinate in polygon.Coordinates)
                        {
                            minX = Mathf.Min((float)minX, (float)coordinate.X);
                            minY = Mathf.Min((float)minY, (float)coordinate.Y);
                            maxX = Mathf.Max((float)maxX, (float)coordinate.X);
                            maxY = Mathf.Max((float)maxY, (float)coordinate.Y);
                        }
                    }
                }
            } else
            {
                Debug.LogWarning("Unsupported geometry type for bounding box calculation: " + feature.Geometry.GetType().Name);
                return;
            }
        }

        double cellWidth = (maxX - minX) / N;
        double cellHeight = (maxY - minY) / N;

        // Put Polygons into Grid Cells based on Centroid
        var gridCells = new Dictionary<(int, int), List<Polygon>>();
        foreach (var feature in features)
        {
            if (feature.Geometry is MultiPolygon multiPolygon)
            {
                foreach (var polygon in multiPolygon.Geometries)
                {
                    if (polygon is Polygon)
                    {
                        var centroid = polygon.Centroid.Coordinate;
                        int cellX = (int)((centroid.X - minX) / cellWidth);
                        int cellY = (int)((centroid.Y - minY) / cellHeight);

                        var cellKey = (cellX, cellY);
                        if (!gridCells.ContainsKey(cellKey))
                            gridCells[cellKey] = new List<Polygon>();

                        gridCells[cellKey].Add((Polygon)polygon);
                    }
                }
            }
        }

        // Generate Mesh Chunks
        int chunkIndex = 0;
        foreach (var cell in gridCells)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            int vertexOffset = 0;

            foreach (var polygon in cell.Value)
            {
                AddPolygonToMesh(polygon, vertices, triangles, ref vertexOffset);
            }

            if (vertices.Count > 0)
            {
                CreateMeshChunk(vertices, triangles, chunkIndex++);
            }
        }
    }

    protected virtual Vector3 TransformCoordinate(NetTopologySuite.Geometries.Coordinate coordinate)
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

                Vector2 xz = geom.LatLonToXZ(latLon.x, latLon.y);
                float y = float.IsNaN((float)c.Z) ? geom.minWorldY : geom.AltitudeToY((float)c.Z);
                result = new Vector3(xz.x, y, xz.y);
            }
            else
            {
                result = new Vector3((float)c.X * geom.XZScale, (float)c.Z * geom.YScale, (float)c.Y * geom.XZScale);
            }
            return result;
        });
    }

    protected virtual void AddPolygonToMesh(Polygon polygon, List<Vector3> vertices, List<int> triangles, ref int vertexOffset)
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

    protected virtual void AddLineStringToMesh(LineString lineString, List<Vector3> vertices, List<int> triangles, ref int vertexOffset)
    {
        var coordinates = lineString.Coordinates;
        var allVertices = new Vector3[coordinates.Length];

        Parallel.For(0, coordinates.Length, i =>
        {
            allVertices[i] = TransformCoordinate(coordinates[i]);
        });

        vertices.AddRange(allVertices);
        vertexOffset += coordinates.Length;
    }

    protected virtual void CreateMeshChunk(List<Vector3> vertices, List<int> triangles, int chunkIndex)
    {
        string chunkName = $"{this.gameObject.name}_Chunk_{chunkIndex}";

        //Debug.Log($"Creating chunk {chunkName} with {vertices.Count} vertices and {triangles.Count / 3f} triangles.");

        if (vertices.Count > MaxVerticesPerChunk)
        {
            Debug.LogError($"Chunk {chunkName} has too many vertices ({vertices.Count}). Potential missing of information.");
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.Optimize();

        GameObject meshObject = new GameObject(chunkName);
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
        
        meshFilter.mesh = mesh;
        meshRenderer.material = material;
        meshCollider.sharedMesh = mesh;
        meshObject.isStatic = true;
        meshObject.tag = "Terrain";

        if (useLODCulling) ConfigureLODGroup(meshObject, meshRenderer);

        meshObject.transform.SetParent(this.transform);
    }

    protected virtual void CreateLineChunk(List<Vector3> vertices, int chunkIndex)
    {
        string chunkName = $"{this.gameObject.name}_Chunk_{chunkIndex}";

        GameObject lineObject = new GameObject(chunkName);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = vertices.Count;
        lineRenderer.SetPositions(vertices.ToArray());

        lineRenderer.material = material;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        lineObject.isStatic = true;

        lineObject.transform.SetParent(this.transform);
    }

    protected virtual void ConfigureLODGroup(GameObject meshObject, MeshRenderer meshRenderer)
    {
        LODGroup lodGroup = meshObject.AddComponent<LODGroup>();
        LOD[] lods = new LOD[2];

        lods[0] = new LOD(screenRelativeTransitionHeight, new Renderer[] { meshRenderer });  // Render until camera is % close
        lods[1] = new LOD(0.0f, new Renderer[] { });  // Hide when camera is far

        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
    }
}