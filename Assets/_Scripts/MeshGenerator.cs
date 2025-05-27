using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Esri;

using ProjNet.CoordinateSystems.Transformations;
using UnityEngine;
using System.Drawing;

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
    public bool useGPUInstancing = false;
    public GameObject instancingPrefab = null;

    [Header("Visual Representation")]
    public Material material;
    public Mesh pointMesh;

    protected CoordinateConverter _converter;
    protected ICoordinateTransformation _transform;

    protected Geometry gtype = null;
    protected ConcurrentDictionary<NetTopologySuite.Geometries.Coordinate, Vector3> cc;

    protected List<Matrix4x4> instanceMatrices;

    protected const int MaxVerticesPerChunk = 65535;
    protected const int MaxBatchSize = 1000;


    protected virtual void Start()
    {
        _converter = new CoordinateConverter();
        _transform = _converter.CreateSJtskToWgs84Transformation();
        cc = new ConcurrentDictionary<NetTopologySuite.Geometries.Coordinate, Vector3>();

        if (material == null) material = new Material(Shader.Find("Standard"));

        if (instancingPrefab == null && useGPUInstancing)
        {
            Debug.LogError("Instancing prefab is not set. Please assign a prefab for GPU instancing. Not using GPU Instancing.");
            useGPUInstancing = false;
        }

        if (useGPUInstancing)
        {
            material.enableInstancing = true;
            if (pointMesh == null) pointMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshFilter>().sharedMesh; // Default point prefab if not set
            instanceMatrices = new List<Matrix4x4>();
        }
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

        if (areaChunked) yield return UniformChunkGenerateCo(filePath);
        else yield return SimpleGenerateCo(filePath);
    }

    protected void SimpleGenerate(string filePath)
    {
        cc.Clear();
        if (useGPUInstancing) instanceMatrices.Clear();
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

            if (feature.Geometry is MultiPoint multiPoint)
            {
                foreach (var point in multiPoint.Geometries)
                {
                    if (point is NetTopologySuite.Geometries.Point)
                    {
                        if (useGPUInstancing)
                            AddPointInstance((NetTopologySuite.Geometries.Point)point);
                        else
                            CreatePointObject(AddPoint((NetTopologySuite.Geometries.Point)point), chunkIndex++);
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
        if (useGPUInstancing) instanceMatrices.Clear();
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

            if (feature.Geometry is MultiPoint multiPoint)
            {
                foreach (var point in multiPoint.Geometries)
                {
                    if (point is NetTopologySuite.Geometries.Point)
                    {
                        if (useGPUInstancing)
                            AddPointInstance((NetTopologySuite.Geometries.Point)point);
                        else
                            CreatePointObject(AddPoint((NetTopologySuite.Geometries.Point)point), chunkIndex++);
                        yield return null;
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
            } 
            else if (feature.Geometry is MultiPoint multiPoint)
            {
                foreach (var point in multiPoint.Geometries)
                {
                    if (point is NetTopologySuite.Geometries.Point)
                    {
                        minX = Mathf.Min((float)minX, (float)point.Coordinate.X);
                        minY = Mathf.Min((float)minY, (float)point.Coordinate.Y);
                        maxX = Mathf.Max((float)maxX, (float)point.Coordinate.X);
                        maxY = Mathf.Max((float)maxY, (float)point.Coordinate.Y);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Unsupported geometry type for bounding box calculation: " + feature.Geometry.GetType().Name);
                return;
            }
        }

        Debug.Log($"Bounding Box: Min({minX}, {minY}), Max({maxX}, {maxY})");

        double cellWidth = (maxX - minX) / N;
        double cellHeight = (maxY - minY) / N;

        // Put Polygons into Grid Cells based on Centroid
        var gridCells = new Dictionary<(int, int), List<Geometry>>(); // Dictionary to hold polygons in grid cells

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
                            gridCells[cellKey] = new List<Geometry>();

                        gridCells[cellKey].Add(polygon);
                    }
                }
            }

            if (feature.Geometry is MultiPoint multiPoint)
            {
                foreach (var point in multiPoint.Geometries)
                {
                    if (point is NetTopologySuite.Geometries.Point)
                    {
                        int cellX = (int)((point.Coordinate.X - minX) / cellWidth);
                        int cellY = (int)((point.Coordinate.Y - minY) / cellHeight);

                        var cellKey = (cellX, cellY);
                        if (!gridCells.ContainsKey(cellKey))
                            gridCells[cellKey] = new List<Geometry>();

                        gridCells[cellKey].Add((Geometry)point);
                    }
                }
            }
        }

        // Generate Mesh Chunks
        int chunkIndex = 0;

        if (gtype is MultiPolygon)
        {
            foreach (var cell in gridCells)
            {
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                int vertexOffset = 0;

                foreach (var polygon in cell.Value)
                {
                    AddPolygonToMesh((Polygon)polygon, vertices, triangles, ref vertexOffset);
                }

                if (vertices.Count > 0)
                {
                    CreateMeshChunk(vertices, triangles, chunkIndex++);
                }
            }

            return;
        }
        
        if (gtype is MultiPoint)
        {
            int cellNum = 0;
            foreach (var cell in gridCells)
            {
                foreach (var point in cell.Value)
                {
                    if (useGPUInstancing)
                        AddPointInstance((NetTopologySuite.Geometries.Point)point);
                    else
                        CreatePointObject(AddPoint((NetTopologySuite.Geometries.Point)point), chunkIndex++);
                }

                if (instanceMatrices.Count > 0)
                {
                    CreatePointGroup(cellNum++);
                    instanceMatrices.Clear();
                }
            }
            return;
        }
    }

    protected IEnumerator UniformChunkGenerateCo(string filePath)
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
            }
            else if (feature.Geometry is MultiPoint multiPoint)
            {
                foreach (var point in multiPoint.Geometries)
                {
                    if (point is NetTopologySuite.Geometries.Point)
                    {
                        minX = Mathf.Min((float)minX, (float)point.Coordinate.X);
                        minY = Mathf.Min((float)minY, (float)point.Coordinate.Y);
                        maxX = Mathf.Max((float)maxX, (float)point.Coordinate.X);
                        maxY = Mathf.Max((float)maxY, (float)point.Coordinate.Y);
                    }
                }
            }
            else if (feature.Geometry is NetTopologySuite.Geometries.Point p)
            {
                minX = Mathf.Min((float)minX, (float)p.Coordinate.X);
                minY = Mathf.Min((float)minY, (float)p.Coordinate.Y);
                maxX = Mathf.Max((float)maxX, (float)p.Coordinate.X);
                maxY = Mathf.Max((float)maxY, (float)p.Coordinate.Y);
            }
            else
            {
                Debug.LogWarning("Unsupported geometry type for bounding box calculation: " + feature.Geometry.GetType().Name);
                yield break;
            }
        }

        Debug.Log($"Bounding Box: Min({minX}, {minY}), Max({maxX}, {maxY})");

        double cellWidth = (maxX - minX) / N;
        double cellHeight = (maxY - minY) / N;

        // Put Polygons into Grid Cells based on Centroid
        var gridCells = new Dictionary<(int, int), List<Geometry>>(); // Dictionary to hold polygons in grid cells

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
                            gridCells[cellKey] = new List<Geometry>();

                        gridCells[cellKey].Add(polygon);
                    }
                }
            }

            if (feature.Geometry is MultiPoint multiPoint)
            {
                foreach (var point in multiPoint.Geometries)
                {
                    if (point is NetTopologySuite.Geometries.Point)
                    {
                        int cellX = (int)((point.Coordinate.X - minX) / cellWidth);
                        int cellY = (int)((point.Coordinate.Y - minY) / cellHeight);

                        var cellKey = (cellX, cellY);
                        if (!gridCells.ContainsKey(cellKey))
                            gridCells[cellKey] = new List<Geometry>();

                        gridCells[cellKey].Add((Geometry)point);
                    }
                }
            }

            if (feature.Geometry is NetTopologySuite.Geometries.Point p)
            {
                int cellX = (int)((p.Coordinate.X - minX) / cellWidth);
                int cellY = (int)((p.Coordinate.Y - minY) / cellHeight);

                var cellKey = (cellX, cellY);
                if (!gridCells.ContainsKey(cellKey))
                    gridCells[cellKey] = new List<Geometry>();

                gridCells[cellKey].Add((Geometry)p);
            }
        }

        // Generate Mesh Chunks
        int chunkIndex = 0;

        if (gtype is MultiPolygon)
        {
            foreach (var cell in gridCells)
            {
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                int vertexOffset = 0;

                foreach (var polygon in cell.Value)
                {
                    AddPolygonToMesh((Polygon)polygon, vertices, triangles, ref vertexOffset);
                }

                if (vertices.Count > 0)
                {
                    CreateMeshChunk(vertices, triangles, chunkIndex++);
                }
                yield return null;
            }

            yield break;
        }

        if (gtype is MultiPoint || gtype is NetTopologySuite.Geometries.Point)
        {
            int cellNum = 0;
            foreach (var cell in gridCells)
            {
                foreach (var point in cell.Value)
                {
                    if (useGPUInstancing)
                        AddPointInstance((NetTopologySuite.Geometries.Point)point);
                    else
                        CreatePointObject(AddPoint((NetTopologySuite.Geometries.Point)point), chunkIndex++);
                }

                if (instanceMatrices.Count > 0)
                {
                    CreatePointGroup(cellNum++);
                    instanceMatrices.Clear();
                }
                yield return null;
            }
            yield break;
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

    protected virtual Vector3 AddPoint(NetTopologySuite.Geometries.Point point)
    {
        return TransformCoordinate(point.Coordinate);
    }

    protected virtual void AddPointInstance(NetTopologySuite.Geometries.Point point)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(AddPoint(point), Quaternion.identity, Vector3.one);
        instanceMatrices.Add(matrix);
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

    protected virtual void CreatePointGroup(int cellNum = 0)
    {
        int chunkIndex = 0;

        while (instanceMatrices.Count > 0)
        {
            int count = Mathf.Min(MaxBatchSize, instanceMatrices.Count);
            var chunk = instanceMatrices.GetRange(0, count);

            // Calculate center and radius
            Vector3 center = Vector3.zero;
            Vector3[] positions = new Vector3[chunk.Count];
            for (int i = 0; i < chunk.Count; i++)
            {
                positions[i] = chunk[i].GetColumn(3);
                center += positions[i];
            }
            center /= positions.Length;

            float maxDistance = 0f;
            for (int i = 0; i < positions.Length; i++)
                maxDistance = Mathf.Max(maxDistance, Vector3.Distance(center, positions[i]));

            // Create instancer
            GameObject gameObject = Instantiate(instancingPrefab, Vector3.zero, Quaternion.identity);
            var instancer = gameObject.GetComponent<PointMeshInstancer>();
            instancer.instances = chunk;
            instancer.mesh = pointMesh;
            instancer.material = material;
            instancer.boundsCenter = center;
            instancer.boundsRadius = maxDistance;

            gameObject.name = $"{this.gameObject.name}_Cell_{cellNum}_Chunk_{chunkIndex}";
            gameObject.transform.SetParent(this.transform);
            gameObject.isStatic = true;

            chunkIndex++;
            instanceMatrices.RemoveRange(0, count);
        }
    }


    private Bounds CalculateBoundsFromMatrices(List<Matrix4x4> matrices)
    {
        if (matrices == null || matrices.Count == 0)
            return new Bounds(Vector3.zero, Vector3.zero);

        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var matrix in matrices)
        {
            Vector3 position = matrix.GetColumn(3);
            min = Vector3.Min(min, position);
            max = Vector3.Max(max, position);
        }

        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    protected virtual void CreatePointObject(Vector3 point, int chunkIndex)
    {
        string chunkName = $"{this.gameObject.name}_Chunk_{chunkIndex}";

        GameObject pointObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pointObject.name = chunkName;
        pointObject.transform.position = point;
        pointObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        pointObject.GetComponent<Renderer>().material = material;
        pointObject.isStatic = true;

        if (useLODCulling)
        {
            MeshRenderer meshRenderer = pointObject.GetComponent<MeshRenderer>();
            ConfigureLODGroup(pointObject, meshRenderer);
        }

        pointObject.transform.SetParent(this.transform);
    }

    protected virtual void ConfigureLODGroup(GameObject meshObject, MeshRenderer meshRenderer)
    {
        LODGroup lodGroup = meshObject.AddComponent<LODGroup>();
        LOD[] lods = new LOD[2];

        lods[0] = new LOD(screenRelativeTransitionHeight, new Renderer[] { meshRenderer });  // Render until camera is % close
        lods[1] = new LOD(0.0f, new Renderer[] { });  // Hide when camera is far

        lodGroup.SetLODs(lods);
        lodGroup.fadeMode = LODFadeMode.CrossFade;
        lodGroup.animateCrossFading = true;
        lodGroup.RecalculateBounds();
    }
}