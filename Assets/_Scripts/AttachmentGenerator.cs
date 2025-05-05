using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Esri;

using ProjNet.CoordinateSystems.Transformations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class AttachmentGenerator : MeshGenerator
{
    [Header("Attachment Specification")]
    public bool raycastToTerrain = true;
    public Vector3 offset;

    private int jobState = 0; // Used to control the coroutine execution

    protected new Dictionary<NetTopologySuite.Geometries.Coordinate, Vector3> cc;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        cc = new Dictionary<NetTopologySuite.Geometries.Coordinate, Vector3>();
    }

    protected override void Update()
    {
        base.Update();

        if (jobState == 0)
        {
            Debug.Log("Starting mesh generation...");
            jobState = 1;
            StartCoroutine(GenerateMeshCo(filePath, useUniformCentroidChunking));
            jobState = 2; // Set state to 2 to indicate that the coroutine is running
            Debug.Log("Finished mesh generation...");
        }

        if (jobState == 2)
        {
            if (useGPUInstancing) RenderPointInstance();
        }
    }

    protected override Vector3 TransformCoordinate(NetTopologySuite.Geometries.Coordinate c)
    {
        if (cc.TryGetValue(c, out Vector3 cachedCoordinate))
        {
            return cachedCoordinate; // Return the cached coordinate if it exists
        }

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

            if (raycastToTerrain)
            {
                RaycastHit hit;
                if (Physics.Raycast(new Vector3(xz.x, y, xz.y), Vector3.down, out hit, Mathf.Infinity))
                {
                    if (hit.collider.CompareTag("Terrain"))
                    {
                        y = hit.point.y; // Set the y to the hit point
                    }
                }
            }

            result = new Vector3(xz.x, y, xz.y) + offset;
        }
        else
        {
            result = new Vector3((float)c.X * geom.XZScale, (float)c.Z * geom.YScale, (float)c.Y * geom.XZScale) + new Vector3(offset.x * geom.XZScale, offset.y * geom.YScale, offset.z * geom.XZScale);
        }

        cc.Add(c, result); // Store the transformed coordinate in the dictionary
        return result;
    }

    protected override void AddPolygonToMesh(Polygon polygon, List<Vector3> vertices, List<int> triangles, ref int vertexOffset)
    {
        var exteriorRing = polygon.ExteriorRing.Coordinates;
        var allVertices = new Vector3[exteriorRing.Length];

        for (int i = 0; i < exteriorRing.Length; i++)
        {
            allVertices[i] = TransformCoordinate(exteriorRing[i]);
        }

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

    protected override void AddLineStringToMesh(LineString lineString, List<Vector3> vertices, List<int> triangles, ref int vertexOffset)
    {
        var coordinates = lineString.Coordinates;
        var allVertices = new Vector3[coordinates.Length];

        for (int i = 0; i < coordinates.Length; i++)
        {
            allVertices[i] = TransformCoordinate(coordinates[i]);
        }

        vertices.AddRange(allVertices);
        vertexOffset += coordinates.Length;
    }

    protected override void CreatePointObject(Vector3 point, int chunkIndex)
    {
        string chunkName = $"{this.gameObject.name}_Chunk_{chunkIndex}";


        GameObject pointObject;

        if (pointMesh != null)
        {
            pointObject = new GameObject(chunkName);
            MeshFilter meshFilter = pointObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = pointObject.AddComponent<MeshRenderer>();
            Mesh mesh = Instantiate(pointMesh);
            meshFilter.mesh = mesh;
            meshRenderer.material = material;
        }
        else
        {
           pointObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
           pointObject.name = chunkName;
            pointObject.GetComponent<Renderer>().material = material;

        }
        pointObject.transform.position = point;
        //pointObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        pointObject.isStatic = true;

        pointObject.transform.SetParent(this.transform);
    }
}
