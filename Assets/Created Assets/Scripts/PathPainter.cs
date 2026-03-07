using UnityEngine;


//Keys 1 2 3 TO SWITCH BRUSH, LEFT MOUSE TO PAINT , HOLD AND DRAG TO CONTINUOUSLY PAINT WITH SPACING, RAISE STAMPS OFF SURFACE TO AVOID Z-FIGHTING, OPTIONAL ALIGNMENT TO SURFACE NORMAL, RANDOM YAW AND SCALE VARIATION, AND CLEAN HIERARCHY WITH OPTIONAL PARENTING.

public class PathPainter : MonoBehaviour
{
    public enum PaintType
    {
        Rock,
        Water,
        Dirt
    }

    [System.Serializable]
    public class PaintBrush
    {
        public PaintType type;
        public GameObject prefab;
    }

    [Header("Brushes")]
    public PaintBrush[] brushes;

    [Tooltip("Which brush starts selected.")]
    public PaintType currentPaintType = PaintType.Rock;

    [Tooltip("Optional parent to keep the hierarchy clean.")]
    public Transform stampParent;

    [Header("Painting")]
    [Tooltip("Distance between stamps along the drag (world units).")]
    public float spacing = 0.5f;

    [Tooltip("Raise the stamp slightly off the surface to avoid z-fighting.")]
    public float surfaceOffset = 0.02f;

    [Tooltip("Layer used for painting.")]
    public LayerMask paintMask;

    [Header("Input")]
    public KeyCode paintKey = KeyCode.Mouse0;
    public KeyCode rockKey = KeyCode.Alpha1;
    public KeyCode waterKey = KeyCode.Alpha2;
    public KeyCode dirtKey = KeyCode.Alpha3;

    [Header("Orientation")]
    [Tooltip("Align stamp to surface normal.")]
    public bool alignToSurfaceNormal = true;

    [Tooltip("If true, ignore surface normal and keep upright.")]
    public bool forceUpright = false;

    [Header("Variation")]
    public bool randomYaw = true;
    public Vector2 yawRange = new Vector2(0f, 360f);

    public bool randomScale = false;
    public Vector2 scaleRange = new Vector2(0.9f, 1.1f);

    [Header("Camera")]
    [Tooltip("Camera used for raycasts. Defaults to Camera.main.")]
    public Camera cam;

    private Vector3 lastStampPos;
    private bool hasLast;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        // If you want it forced automatically, use this:
        paintMask = LayerMask.GetMask("Ground");
    }

    private void Update()
    {
        if (cam == null)
            return;

        HandleBrushSwitching();

        GameObject currentPrefab = GetCurrentPrefab();
        if (currentPrefab == null)
            return;

        if (Input.GetKeyDown(paintKey))
        {
            hasLast = false;
            TryStamp(currentPrefab);
        }

        if (Input.GetKey(paintKey))
        {
            TryStamp(currentPrefab);
        }

        if (Input.GetKeyUp(paintKey))
        {
            hasLast = false;
        }
    }

    private void HandleBrushSwitching()
    {
        if (Input.GetKeyDown(rockKey))
        {
            currentPaintType = PaintType.Rock;
            Debug.Log("Selected brush: Rock");
        }

        if (Input.GetKeyDown(waterKey))
        {
            currentPaintType = PaintType.Water;
            Debug.Log("Selected brush: Water");
        }

        if (Input.GetKeyDown(dirtKey))
        {
            currentPaintType = PaintType.Dirt;
            Debug.Log("Selected brush: Dirt");
        }
    }

    private GameObject GetCurrentPrefab()
    {
        foreach (PaintBrush brush in brushes)
        {
            if (brush.type == currentPaintType)
                return brush.prefab;
        }

        Debug.LogWarning($"No prefab assigned for paint type: {currentPaintType}");
        return null;
    }

    private void TryStamp(GameObject prefabToPaint)
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
                    PlaceStamp(prefabToPaint, stepHit.point, stepHit.normal);
                }
                else
                {
                    PlaceStamp(prefabToPaint, stepPos, hit.normal);
                }
            }

            lastStampPos = pos;
        }
        else
        {
            PlaceStamp(prefabToPaint, hit.point, hit.normal);
            lastStampPos = pos;
            hasLast = true;
        }
    }

    private void PlaceStamp(GameObject prefabToPaint, Vector3 point, Vector3 normal)
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
            rot *= Quaternion.Euler(0f, yaw, 0f);
        }

        GameObject go = Instantiate(prefabToPaint, pos, rot, stampParent);

        if (randomScale)
        {
            float s = Random.Range(scaleRange.x, scaleRange.y);
            go.transform.localScale *= s;
        }
    }
}