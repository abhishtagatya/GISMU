using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointMeshInstancer : MonoBehaviour
{
    public float maxRenderDistance = 500f;

    public List<Matrix4x4> instances = new List<Matrix4x4>();
    public Mesh mesh;
    public Material material;

    public Vector3 boundsCenter;
    public float boundsRadius = 50f;

    protected const int MaxBatchSize = 1000;

    private Camera mainCamera;
    private bool isInstancingEnabled = false;

    private bool disabled = false;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        RenderInstance();

        if (Input.GetKeyDown(KeyCode.H))
        {
            disabled = !disabled;
            Debug.Log("Visibility Culling " + (disabled ? "Disabled" : "Enabled"));
        }

    }

    void RenderInstance()
    {
        if (IsVisibleToCamera() || disabled) Graphics.DrawMeshInstanced(mesh, 0, material, instances);
    }

    bool IsVisibleToCamera()
    {
        if (!mainCamera) return true;

        float dist = Vector3.Distance(mainCamera.transform.position, boundsCenter);
        return dist <= boundsRadius + maxRenderDistance;
    }
}
