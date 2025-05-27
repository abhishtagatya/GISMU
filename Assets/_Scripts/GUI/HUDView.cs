using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class HUDView : AView
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GeospatialManager geospatialManager;

    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI speedInfoText;
    [SerializeField] private TextMeshProUGUI altitudeInfoText;
    [SerializeField] private TextMeshProUGUI distanceInfoText;
    [SerializeField] private TextMeshProUGUI locationInfoText;
    [SerializeField] private UnityEngine.UI.RawImage compassImage;

    [SerializeField] private float updateInterval = 1f; // Update speed every second
    [SerializeField] private float requestInterval = 1f; // Update geocoding request every 5 seconds

    private Vector2 prevCoordinate = Vector2.zero;
    private float prevTime = 0f;
    private float prevRequestTime = 0f;

    private const float EarthRadius = 6371000f; // meters

    public override void Initialize()
    {
        // Initialize the HUD elements here if needed
    }

    void Update()
    {
        // Update the HUD elements with the player's coordinates
        if (player != null && geospatialManager != null)
        {
            Vector2 playerCoordinates = geospatialManager.XZToLatLon(player.position.x, player.position.z);
            float playerAltitude = geospatialManager.YToAltitude(player.position.y);
            float playerDistanceToMesh = GetDistanceToMesh();

            altitudeInfoText.text = $"{playerAltitude:F0}";

            if (playerDistanceToMesh == float.MaxValue)
            {
                distanceInfoText.text = "N/A"; // No ground detected
            }
            else
            {
                distanceInfoText.text = $"{playerDistanceToMesh:F0}";
            }

            if (Time.time - prevTime >= updateInterval)
            {
                float playerSpeed = GetPlayerSpeed(playerCoordinates, geospatialManager.XZScale);
                speedInfoText.text = $"{playerSpeed:F0}";

                float bearing = GetCompassBearingFromForward(player.forward);
                compassImage.rectTransform.localEulerAngles = new Vector3(0, 0, -bearing);
            }

            if (Time.time - prevRequestTime >= requestInterval)
            {
                // Update the geocoding request
                StartCoroutine(ReverseGeocoder.GetAddressFromCoordinates(playerCoordinates.x, playerCoordinates.y, OnGeocodeSuccess, OnGeocodeError));
                prevRequestTime = Time.time;
            }
        }
    }

    void OnGeocodeSuccess(NominatimResponse response)
    {
        if (response != null && response.address != null)
        {
            string address = $"{response.address.road}";
            locationInfoText.text = address;
        }
        else
        {
            Debug.LogWarning("Geocoding response is null or missing address.");
        }
    }

    void OnGeocodeError(string error)
    {
        Debug.LogError("Geocoding error: " + error);
        locationInfoText.text = "N/A";
    }

    float GetDistanceToMesh()
    {
        Ray ray = new Ray(player.position, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            return player.position.y - hit.point.y;
        }

        // Return a large number if no ground was hit
        return float.MaxValue;
    }

    float HaversineDistance(Vector2 coord1, Vector2 coord2)
    {
        float lat1Rad = Mathf.Deg2Rad * coord1.x;
        float lon1Rad = Mathf.Deg2Rad * coord1.y;
        float lat2Rad = Mathf.Deg2Rad * coord2.x;
        float lon2Rad = Mathf.Deg2Rad * coord2.y;

        float dLat = lat2Rad - lat1Rad;
        float dLon = lon2Rad - lon1Rad;

        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                  Mathf.Cos(lat1Rad) * Mathf.Cos(lat2Rad) *
                  Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);

        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        return EarthRadius * c;
    }
    float GetCompassBearingFromForward(Vector3 forward)
    {
        if (geospatialManager == null || player == null)
            return 0f;

        // Current lat/lon
        Vector2 currentLatLon = geospatialManager.XZToLatLon(player.position.x, player.position.z);

        // Future position in world space (forward direction, projected on XZ plane)
        Vector3 futurePosition = player.position + forward.normalized * 1f; // 1 meter forward
        Vector2 futureLatLon = geospatialManager.XZToLatLon(futurePosition.x, futurePosition.z);

        // Convert to radians
        float lat1 = Mathf.Deg2Rad * currentLatLon.x;
        float lon1 = Mathf.Deg2Rad * currentLatLon.y;
        float lat2 = Mathf.Deg2Rad * futureLatLon.x;
        float lon2 = Mathf.Deg2Rad * futureLatLon.y;

        float dLon = lon2 - lon1;

        float y = Mathf.Sin(dLon) * Mathf.Cos(lat2);
        float x = Mathf.Cos(lat1) * Mathf.Sin(lat2) -
                  Mathf.Sin(lat1) * Mathf.Cos(lat2) * Mathf.Cos(dLon);

        float bearingRad = Mathf.Atan2(y, x);
        float bearingDeg = (Mathf.Rad2Deg * bearingRad + 360f) % 360f;

        return bearingDeg;
    }


    float GetPlayerSpeed(Vector2 currentCoord, float worldScale)
    {
        float currentTime = Time.time;
        float distance = HaversineDistance(prevCoordinate, currentCoord) * worldScale; // in meters
        float speed = distance / (currentTime - prevTime);

        prevCoordinate = currentCoord;
        prevTime = currentTime;

        return speed * 3.6f;
    }
}
