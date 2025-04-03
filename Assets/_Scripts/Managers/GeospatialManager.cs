using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeospatialManager : MonoBehaviour
{
    public Vector2 mapCenter = new Vector2(0, 0);
    public Vector2 gameCenter = new Vector2(0, 0);
    public float XZScale = 1.0f;
    public float YScale = 1.0f;
    public float minWorldY = 0.0f;

    private CoordinateTranslator ct;

    // Start is called before the first frame update
    void Start()
    {
        ct = new CoordinateTranslator(
            mapCenter,
            gameCenter,
            Mathf.Deg2Rad * 45,
            XZScale,
            YScale,
            (int)minWorldY
        );
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector2 LatLonToXZ(float lat, float lon)
    {
        return ct.LatLonToXZ(lat, lon);
    }

    public Vector2 LatLonToXZ(Vector2 point)
    {
        return ct.LatLonToXZ(point.x, point.y);
    }

    public Vector2 XZToLatLon(float x, float z)
    {
        return ct.XZToLatLon(x, z);
    }

    public float AltitudeToY(float altitude)
    {
        return ct.AltitudeToY(altitude);
    }

    public Vector2 XZToLatLon(Vector2 point)
    {
        return ct.XZToLatLon(point.x, point.y);
    }

    public float HeightToY(float height)
    {
        return ct.HeightToY(height);
    }

    public float YToHeight(float y)
    {
        return ct.YToHeight(y);
    }

    public float YToAltitude(float y)
    {
        return ct.YToAltitude(y);
    }
}
