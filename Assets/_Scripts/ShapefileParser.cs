using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Esri;

using UnityEngine;


public class ShapefileParser : MonoBehaviour
{
    public string shapefilePath;

    // Start is called before the first frame update
    void Start()
    {
        Parse();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Parse()
    {
        int i = 0;
        foreach (var feature in Shapefile.ReadAllFeatures(shapefilePath))
        {
            foreach (var attrName in feature.Attributes.GetNames())
            {
                //Debug.Log(attrName + ": " + feature.Attributes[attrName]);
            }

            Display(feature.Geometry);
            i++;
        }
        Debug.Log("Total features: " + i);
    }

    public void Display(Geometry geometry)
    {
        if (geometry is Point)
        {
            //HandleGeometryPoint((Point)geometry);
        }
        else if (geometry is MultiPoint)
        {
            //HandleGeometryMultiPoint((MultiPoint)geometry);
        }
        else if (geometry is LineString)
        {
            //HandleGeometryLineString((LineString)geometry);
        }
        else if (geometry is MultiLineString)
        {
            //HandleGeometryMultiLineString((MultiLineString)geometry);
        }
        else if (geometry is Polygon)
        {
            HandleGeometryPolygon((Polygon)geometry);
        }
        else if (geometry is MultiPolygon)
        {
            HandleGeometryMultiPolygon((MultiPolygon)geometry);
        }
    }

    public void HandleGeometryMultiPolygon(MultiPolygon multiPolygon)
    {
        GameObject combinedPolyObj = new GameObject("MultiPolygon");
        MeshFilter meshFilter = combinedPolyObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = combinedPolyObj.AddComponent<MeshRenderer>();

        List<Vector3> allVertices = new List<Vector3>();
        List<int> allIndices = new List<int>();

        int vertexOffset = 0;
        float scaleFactor = 0.01f; // Adjust as necessary

        // Calculate the centroid of the entire MultiPolygon
        Vector3 centroid = Vector3.zero;
        int totalCoords = 0;

        // Calculate centroid based on all exterior ring coordinates
        foreach (Polygon polygon in multiPolygon.Geometries)
        {
            foreach (var coord in polygon.ExteriorRing.Coordinates)
            {
                centroid += new Vector3((float)coord.X, (float)coord.Z, (float)coord.Y);
                totalCoords++;
            }
        }
        centroid /= totalCoords; // Compute average (centroid)

        // Set the transform of the GameObject to the centroid
        combinedPolyObj.transform.position = centroid * scaleFactor;

        // Adjust vertices to be relative to the centroid
        foreach (Polygon polygon in multiPolygon.Geometries)
        {
            List<Vector3> vertices = new List<Vector3>();

            // Process exterior ring
            foreach (var coord in polygon.ExteriorRing.Coordinates)
            {
                // Calculate position relative to the centroid
                Vector3 position = new Vector3(
                    (float)coord.X,
                    (float)coord.Z,
                    (float)coord.Y
                ) - centroid;

                vertices.Add(position * scaleFactor);
            }

            // Triangulate polygon
            for (int i = 1; i < vertices.Count - 1; i++)
            {
                allIndices.Add(vertexOffset);
                allIndices.Add(vertexOffset + i);
                allIndices.Add(vertexOffset + i + 1);
            }

            allVertices.AddRange(vertices);
            vertexOffset += vertices.Count;

            // Handle holes (interior rings)
            foreach (var interiorRing in polygon.InteriorRings)
            {
                foreach (var coord in interiorRing.Coordinates)
                {
                    // Calculate position relative to the centroid
                    Vector3 position = new Vector3(
                        (float)coord.X,
                        (float)coord.Z,
                        (float)coord.Y
                    ) - centroid;

                    vertices.Add(position * scaleFactor);
                }
            }
        }

        // Create the mesh
        Mesh mesh = new Mesh();
        mesh.SetVertices(allVertices);
        mesh.SetTriangles(allIndices, 0);
        mesh.Optimize();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }
    public void HandleGeometryPolygon(Polygon polygon)
    {
        GameObject polyObj = new GameObject("Polygon");
        MeshFilter meshFilter = polyObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = polyObj.AddComponent<MeshRenderer>();

        List<Vector3> vertices = new List<Vector3>();

        float offsetX = 1000000f; // Example offset for shifting the origin
        float offsetY = 1000000f; // Example offset for shifting the origin

        foreach (var coord in polygon.ExteriorRing.Coordinates)
        {
            // Apply scaling/translation if needed
            float scaleFactor = 0.01f; // Adjust as necessary
            Vector3 position = new Vector3((float)coord.X - offsetX, (float)coord.Z, (float)coord.Y - offsetY) * scaleFactor;
            vertices.Add(position);
        }

        // Handle holes if the polygon has them (InteriorRing)
        foreach (var interiorRing in polygon.InteriorRings)
        {
            foreach (var coord in interiorRing.Coordinates)
            {
                // Add interior hole coordinates
                float scaleFactor = 0.01f; // Adjust as necessary
                Vector3 position = new Vector3((float)coord.X - offsetX, (float)coord.Z, (float)coord.Y - offsetY) * scaleFactor;
                vertices.Add(position);
            }
        }

        // Simple triangulation logic (assuming the polygon is simple)
        List<int> indices = new List<int>();
        for (int i = 1; i < vertices.Count - 1; i++)
        {
            indices.Add(0); // First vertex
            indices.Add(i); // Second vertex
            indices.Add(i + 1); // Third vertex
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0);
        mesh.Optimize();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }


}