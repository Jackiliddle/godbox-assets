using UnityEngine;

public class PathPainter : MonoBehaviour
{
    [Header("Stamp Prefab")]
    [Tooltip("Prefab to place repeatedly (e.g., dirt decal mesh, footprints mesh, path tile).")]
    public GameObject stampPrefab;

    [Tooltip("Optional parent to keep the hierarchy clean.")]
    public Transform stampParent;

    [Header("Painting")]
    [Tooltip("Distance between stamps along the drag (world units).")]
    public float spacing = 0.5f;

    [Tooltip("Raise the stamp slightly off the surface to avoid z-fighting.")]
    public float surfaceOffset = 0.02f;

    [Tooltip("Layer used for painting (automatically set to Ground).")]
    public LayerMask paintMask;

    [Header("Input")]
    public KeyCode paintKey = KeyCode.Mouse0;

    [Header("Orientation")]
    [Tooltip("Align stamp to surface normal.")]
    public bool alignToSurfaceNormal = true;

    [Tooltip("If alignToSurfaceNormal is false, use world up.")]
    public bool forceUpright = false;

    [Header("Variation")]
    public bool randomYaw = true;
    public Vector2 yawRange = new Vector2(0f, 360f);

    public bool randomScale = false;
    public Vector2 scaleRange = new Vector2(0.9f, 1.1f);

    [Header("Camera")]
    [Tooltip("Camera used for raycasts. Defaults to Camera.main.")]
    public Camera cam;

    Vector3 lastStampPos;
    bool hasLast;

    void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        // Only paint on the Ground layer
        paintMask = LayerMask.GetMask("Ground");
    }

    void Update()
    {
        if (stampPrefab == null || cam == null)
            return;

        if (Input.GetKeyDown(paintKey))
        {
            hasLast = false;
            TryStamp();
        }

        if (Input.GetKey(paintKey))
        {
            TryStamp();
        }

        if (Input.GetKeyUp(paintKey))
        {
            hasLast = false;
        }
    }

    void TryStamp()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, paintMask))
            return;

        Vector3 pos = hit.point;

        if (hasLast)
        {
            float d = Vector3.Distance(pos, lastStampPos);
            if (d < spacing) return;

            int steps = Mathf.FloorToInt(d / spacing);
            Vector3 dir = (pos - lastStampPos).normalized;

            for (int i = 1; i <= steps; i++)
            {
                Vector3 stepPos = lastStampPos + dir * (spacing * i);

                Vector3 rayStart = stepPos + Vector3.up * 5f;
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit stepHit, 20f, paintMask))
                {
                    PlaceStamp(stepHit.point, stepHit.normal);
                }
                else
                {
                    PlaceStamp(stepPos, hit.normal);
                }
            }

            lastStampPos = pos;
        }
        else
        {
            PlaceStamp(hit.point, hit.normal);
            lastStampPos = pos;
            hasLast = true;
        }
    }

    void PlaceStamp(Vector3 point, Vector3 normal)
    {
        Vector3 up = forceUpright ? Vector3.up : (alignToSurfaceNormal ? normal : Vector3.up);
        Vector3 pos = point + up.normalized * surfaceOffset;

        Quaternion rot;

        if (alignToSurfaceNormal && !forceUpright)
        {
            rot = Quaternion.FromToRotation(Vector3.up, normal);
        }
        else
        {
            rot = Quaternion.identity;
        }

        if (randomYaw)
        {
            float yaw = Random.Range(yawRange.x, yawRange.y);
            rot = rot * Quaternion.Euler(0f, yaw, 0f);
        }

        GameObject go = Instantiate(stampPrefab, pos, rot, stampParent);

        if (randomScale)
        {
            float s = Random.Range(scaleRange.x, scaleRange.y);
            go.transform.localScale = go.transform.localScale * s;
        }
    }
}