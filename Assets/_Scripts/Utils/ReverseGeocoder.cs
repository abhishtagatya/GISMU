using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class NominatimAddress
{
    public string road;
    public string hamlet;
    public string town;
    public string city;
    public string state_district;
    public string state;
    public string postcode;
    public string country;
    public string country_code;
}

[System.Serializable]
public class NominatimResponse
{
    public string display_name;
    public NominatimAddress address;
}

public class ReverseGeocoder : MonoBehaviour
{
    public static IEnumerator GetAddressFromCoordinates(float lat, float lon, System.Action<NominatimResponse> onSuccess, System.Action<string> onError = null)
    {
        string url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat}&lon={lon}&zoom=18&addressdetails=1";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("User-Agent", "UnityGeocoderApp/1.0 (your@email.com)");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            Debug.Log("Response: " + json);

            try
            {
                NominatimResponse response = JsonUtility.FromJson<NominatimResponse>(json);
                onSuccess?.Invoke(response);
            }
            catch (System.Exception ex)
            {
                onError?.Invoke("JSON Parse Error: " + ex.Message);
            }
        }
        else
        {
            onError?.Invoke($"API Error: {request.error}");
        }
    }
}
