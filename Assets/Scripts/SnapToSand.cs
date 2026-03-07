using UnityEngine;

public class SnapToSand : MonoBehaviour
{
    public LayerMask sandLayer;

    public void SnapToCurrentMesh()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 10f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 20f, sandLayer))
        {
            transform.position = hit.point;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
    }
}