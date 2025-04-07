using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.IO.CoordinateSystems;

using UnityEngine;

public class CoordinateConverter
{
    // S-JTSK (EPSG:5514)
    private static string SJTSK_WKT = @"
PROJCS[""S-JTSK / Krovak East North"",GEOGCS[""S-JTSK"",DATUM[""System_of_the_Unified_Trigonometrical_Cadastral_Network"",SPHEROID[""Bessel 1841"",6377397.155,299.1528128],TOWGS84[589,76,480,0,0,0,0]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4156""]],PROJECTION[""Krovak""],PARAMETER[""latitude_of_center"",49.5],PARAMETER[""longitude_of_center"",24.8333333333333],PARAMETER[""azimuth"",30.2881397527778],PARAMETER[""pseudo_standard_parallel_1"",78.5],PARAMETER[""scale_factor"",0.9999],PARAMETER[""false_easting"",0],PARAMETER[""false_northing"",0],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AXIS[""Easting"",EAST],AXIS[""Northing"",NORTH],AUTHORITY[""EPSG"",""5514""]]";

    // WGS84 (EPSG:4326)
    private static string WGS84_WKT = @"
GEOGCS[""WGS 84"",DATUM[""WGS_1984"",SPHEROID[""WGS 84"",6378137,298.257223563,AUTHORITY[""EPSG"",""7030""]],AUTHORITY[""EPSG"",""6326""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4326""]]";

    public ICoordinateTransformation CreateSJtskToWgs84Transformation()
    {
        // Parse S-JTSK
        var sjtsk = CoordinateSystemWktReader.Parse(SJTSK_WKT) as ProjectedCoordinateSystem;

        // Parse WGS84
        var wgs84 = CoordinateSystemWktReader.Parse(WGS84_WKT) as GeographicCoordinateSystem;

        if (sjtsk == null || wgs84 == null)
        {
            Debug.LogError("Failed to parse coordinate systems.");
            return null;
        }

        // Create transformation
        var ctFactory = new CoordinateTransformationFactory();
        return ctFactory.CreateFromCoordinateSystems(sjtsk, wgs84);
    }
}