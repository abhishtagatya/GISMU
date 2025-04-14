using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MeshGenerator
{
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        GenerateMesh(filePath, useUniformCentroidChunking);
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }
}
