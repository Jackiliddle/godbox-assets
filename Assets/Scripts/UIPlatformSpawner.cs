using UnityEngine;

public class UIPlatformSpawner : MonoBehaviour
{
    [Header("Placement Settings")]
    public Transform previewPlatform;

    private GameObject currentPreview;
    private GameObject lastSelectedPrefab;
    public void SpawnFromCategory(GameObject prefab)
    {
        lastSelectedPrefab = prefab;
        RefreshPlatform();
    }

    private void RefreshPlatform()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
        }

        if (lastSelectedPrefab != null && previewPlatform != null)
        {
            currentPreview = Instantiate(lastSelectedPrefab, previewPlatform.position, previewPlatform.rotation);

            Rigidbody rb = currentPreview.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
       
        if (currentPreview != null && other.gameObject == currentPreview)
        {
            currentPreview = null;

            Invoke("RefreshPlatform", 0.1f);
        }
    }
}