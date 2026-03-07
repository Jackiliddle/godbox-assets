using UnityEngine;
using UnityEngine.InputSystem;
using Intel.RealSense;
using System.Linq;

public class SandTopographyManager : MonoBehaviour
{
    public SandMeshBuilder meshBuilder;
    public Transform sandboxParent;

    [Header("RealSense Integration")]
    public RsFrameProvider Source;

    [Header("GPU Compute Shader")]
    public ComputeShader sandCalibrator;

    [Header("Rendering")]
    public Mesh sandGrainMesh;
    public Material sandMaterial;

    [Header("Sandbox Cropping (World Space)")]
    public float minX = -0.5f;
    public float maxX = 0.5f;
    public float minZ = -0.5f;
    public float maxZ = 0.5f;

    [Tooltip("The Ceiling. Anything higher than this is instantly deleted. Perfect for hiding arms!")]
    public float maxY = 0.9f;

    [Header("Mesh Stability (Temporal Smoothing)")]
    [Range(0.8f, 0.99f)]
    public float smoothingFactor = 0.95f;
    [Range(0.005f, 0.1f)]
    public float movementThreshold = 0.02f;
    [Tooltip("If an object is moving fast AND is higher than this Y value, the shader ignores it (filters hands).")]
    public float handHeightMin = 0.3f;

    private ComputeBuffer rawBuffer;
    public ComputeBuffer calibratedBuffer { get; private set; }
    private ComputeBuffer smoothedPreviousBuffer;

    private Vector3[] sandVertices;
    private int vertexCount = 0;
    private FrameQueue frameQueue;

    private int calibrateKernel;
    private int smoothKernel;

    public bool isEditMode = true;

    void Start()
    {
        Source.OnStart += OnStartStreaming;
        Source.OnStop += Dispose;

        if (meshBuilder != null)
        {
            meshBuilder.gameObject.SetActive(false);
        }
    }

    private void OnStartStreaming(PipelineProfile profile)
    {
        frameQueue = new FrameQueue(1);
        calibrateKernel = sandCalibrator.FindKernel("CalibrateSand");
        smoothKernel = sandCalibrator.FindKernel("TemporalSmooth");
        Source.OnNewSample += OnNewSample;
    }

    private void OnNewSample(Frame frame)
    {
        if (frameQueue == null || !isEditMode) return;

        if (frame.IsComposite)
        {
            using (var fs = frame.As<FrameSet>())
            using (var points = fs.FirstOrDefault<Points>(Stream.Depth, Format.Xyz32f))
            {
                if (points != null) frameQueue.Enqueue(points);
            }
        }
        else if (frame.Is(Extension.Points))
        {
            frameQueue.Enqueue(frame);
        }
    }

    void LateUpdate()
    {
        // Check for Keyboard space key as a fallback for the OVR Input
        if ((UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            ToggleMeshMode();
        }

        if (isEditMode && frameQueue != null)
        {
            Points points;
            if (frameQueue.PollForFrame<Points>(out points))
            {
                using (points)
                {
                    if (points.VertexData != System.IntPtr.Zero)
                    {
                        if (points.Count != vertexCount || rawBuffer == null)
                        {
                            vertexCount = points.Count;
                            sandVertices = new Vector3[vertexCount];

                            if (rawBuffer != null) rawBuffer.Release();
                            if (calibratedBuffer != null) calibratedBuffer.Release();
                            if (smoothedPreviousBuffer != null) smoothedPreviousBuffer.Release();

                            rawBuffer = new ComputeBuffer(vertexCount, 12);
                            calibratedBuffer = new ComputeBuffer(vertexCount, 12);
                            smoothedPreviousBuffer = new ComputeBuffer(vertexCount, 12);
                        }

                        points.CopyVertices(sandVertices);
                      
                        rawBuffer.SetData(sandVertices);
                        RunComputeShaderSequentially();
                    }
                }
            }
        }

        if (isEditMode && calibratedBuffer != null && sandGrainMesh != null && sandMaterial != null && vertexCount > 0)
        {
            sandMaterial.SetBuffer("CalibratedPoints", calibratedBuffer);
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
            Graphics.DrawMeshInstancedProcedural(sandGrainMesh, 0, sandMaterial, bounds, vertexCount);
        }
    }

    private void RunComputeShaderSequentially()
    {
        if (sandCalibrator == null || rawBuffer == null || calibratedBuffer == null || smoothedPreviousBuffer == null) return;

        sandCalibrator.SetBuffer(calibrateKernel, "RawPoints", rawBuffer);
        sandCalibrator.SetBuffer(calibrateKernel, "CalibratedPoints", calibratedBuffer);
        sandCalibrator.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        sandCalibrator.SetInt("_VertexCount", vertexCount);

        sandCalibrator.SetFloat("_MinX", minX);
        sandCalibrator.SetFloat("_MaxX", maxX);
        sandCalibrator.SetFloat("_MinZ", minZ);
        sandCalibrator.SetFloat("_MaxZ", maxZ);
        sandCalibrator.SetFloat("_MaxY", maxY);

        int threadGroups = Mathf.CeilToInt(vertexCount / 64f);
        sandCalibrator.Dispatch(calibrateKernel, threadGroups, 1, 1);

        sandCalibrator.SetBuffer(smoothKernel, "CalibratedPoints", calibratedBuffer);
        sandCalibrator.SetBuffer(smoothKernel, "SmoothedPointsPrevious", smoothedPreviousBuffer);
        sandCalibrator.SetFloat("_SmoothingFactor", smoothingFactor);
        sandCalibrator.SetFloat("_MovementThreshold", movementThreshold);
        sandCalibrator.SetFloat("_HandHeightMin", handHeightMin);
        sandCalibrator.SetInt("_VertexCount", vertexCount);

        sandCalibrator.Dispatch(smoothKernel, threadGroups, 1, 1);
    }

    public void ToggleMeshMode()
    {
        isEditMode = !isEditMode;

        if (!isEditMode)
        {
            if (meshBuilder != null && calibratedBuffer != null)
            {
                meshBuilder.gameObject.SetActive(true);
                meshBuilder.GeneratePhysicalMesh(calibratedBuffer);
            }
        }
        else
        {
            if (meshBuilder != null)
            {
                meshBuilder.gameObject.SetActive(false);
            }
        }
    }

    private void Dispose()
    {
        Source.OnNewSample -= OnNewSample;

        if (frameQueue != null)
        {
            frameQueue.Dispose();
            frameQueue = null;
        }

        if (rawBuffer != null) { rawBuffer.Release(); rawBuffer = null; }
        if (calibratedBuffer != null) { calibratedBuffer.Release(); calibratedBuffer = null; }
        if (smoothedPreviousBuffer != null) { smoothedPreviousBuffer.Release(); smoothedPreviousBuffer = null; }
    }

    void OnDestroy()
    {
        Dispose();
    }
}