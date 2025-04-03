using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoordinateTranslator
{
    private static readonly double EARTH_RADIUS = 6371 * 1000; // meters
    private int altShift = 0;

    public Vector2 mapCenter = new Vector2(0, 0);
    public Vector2 gameCenter = new Vector2(0, 0);
    public float rotation;
    public float xzScale;
    public float yScale;

    private int minWorldY = 0;

    public CoordinateTranslator(Vector2 coordCenter, Vector2 gameCenter, float rotation, float xzScale, float yScale, int minWorldY)
    {
        this.mapCenter = new Vector2(Rad(coordCenter.x), Rad(coordCenter.y));
        this.gameCenter = gameCenter;
        this.rotation = rotation;
        this.xzScale = xzScale;
        this.yScale = yScale;

        this.minWorldY = minWorldY;
    }

    public void SetAltShift(int altShift)
    {
        this.altShift = altShift;
    }

    public float HeightToY(float height)
    {
        return height * yScale;
    }

    public float YToHeight(float y)
    {
        return y / yScale;
    }

    public float AltitudeToY(float altitude)
    {
        return HeightToY(altitude + altShift) + minWorldY;
    }

    public float YToAltitude(float y)
    {
        return YToHeight(y - minWorldY) - altShift;
    }

    public Vector2 LatLonToXZ(float lat, float lon)
    {
        lat = Rad(lat); lon = Rad(lon);

        float deltaLon = lon - mapCenter.y;
        float x = (float)(EARTH_RADIUS * Mathf.Cos(lat) * Mathf.Sin(deltaLon));
        float z = (float)(EARTH_RADIUS * (Mathf.Cos(mapCenter.x) * Mathf.Sin(lat) - Mathf.Sin(mapCenter.x) * Mathf.Cos(lat) * Mathf.Cos(deltaLon)));

        float x2 = x * Mathf.Cos(rotation) - z * Mathf.Sin(rotation);
        float z2 = x * Mathf.Sin(rotation) + z * Mathf.Cos(rotation);

        x = x2;
        z = z2;

        x *= xzScale;
        z *= -xzScale; // Add negative sign to match the original Java code

        return new Vector2(x, z) + gameCenter;
    }

    public Vector2 LatLonToXZ(Vector2 point)
    {
        return LatLonToXZ(point.x, point.y);
    }

    public Vector2 XZToLatLon(float x, float z)
    {
        // Reverse the game center offset
        x -= gameCenter.x;
        z -= gameCenter.y;

        // Reverse scaling
        x /= xzScale;
        z /= -xzScale;  // Reverse the sign to match LatLonToXZ

        // Reverse rotation
        float x2 = x * Mathf.Cos(-rotation) - z * Mathf.Sin(-rotation);
        float z2 = x * Mathf.Sin(-rotation) + z * Mathf.Cos(-rotation);
        x = x2;
        z = z2;

        // Convert back to latitude and longitude
        float lat = Mathf.Asin(
            (float)(Mathf.Sin(mapCenter.x) + (z / EARTH_RADIUS) * Mathf.Cos(mapCenter.x))
        );

        float lon = mapCenter.y + Mathf.Asin((float)(x / (EARTH_RADIUS * Mathf.Cos(lat))));

        return new Vector2(Deg(lat), Deg(lon));
    }

    public Vector2 XZToLatLon(Vector2 point)
    {
        return XZToLatLon(point.x, point.y);
    }

    private float Rad(float deg)
    {
        return deg * (Mathf.PI / 180);
    }

    private float Deg(float rad)
    {
        return rad * (180 / Mathf.PI);
    }
}