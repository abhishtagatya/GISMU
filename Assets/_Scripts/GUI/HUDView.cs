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
    [SerializeField] private TextMeshProUGUI coordinateText;
    [SerializeField] private TextMeshProUGUI altitudeText;

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
            coordinateText.text = $"Lat: {playerCoordinates.x:F6}, Lon: {playerCoordinates.y:F6}";
            
            float playerAltitude = GetDistanceToMesh();
            altitudeText.text = $"Distance: {playerAltitude:F2} m";
        }
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

}
