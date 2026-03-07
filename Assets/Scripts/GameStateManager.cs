using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.InputSystem;

public class GameStateManager : MonoBehaviour
{
    public enum GamePhase { Build, Live }
    public GamePhase currentPhase = GamePhase.Build;

    [Header("Dependencies")]
    public SandTopographyManager topographyManager;
    public GameObject PhysicalSandMeshObject;
    public GameObject EcosystemManager;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (currentPhase == GamePhase.Build)
            {
                Debug.Log("T Pressed: Awakening the Ecosystem...");
                ActivateEcosystem();
            }
        }
    }

    private void ActivateEcosystem()
    {
        // 1. Bake the NavMesh on the frozen mesh
        if (PhysicalSandMeshObject != null)
        {
            var navSurface = PhysicalSandMeshObject.GetComponent<NavMeshSurface>();
            if (navSurface != null) navSurface.BuildNavMesh();
        }

        // 2. Snap any existing buildings
        SnapBuildingsToSand();

        // 3. Wake up the Spawners
        if (EcosystemManager != null)
        {
            MonoBehaviour[] allSpawners = EcosystemManager.GetComponents<MonoBehaviour>();
            foreach (var s in allSpawners) s.enabled = true;
        }

        currentPhase = GamePhase.Live;
        Debug.Log("The World is now Alive!");
    }

    private void SnapBuildingsToSand()
    {
        SnapToSand[] allBuildings = Object.FindObjectsByType<SnapToSand>(FindObjectsSortMode.None);
        foreach (var b in allBuildings) b.SnapToCurrentMesh();
    }
}