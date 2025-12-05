using System.Collections;
using UnityEngine;

/// <summary>
/// 씬 어디에 있든 상관 없이, 지정한 Mesh 오브젝트 위에서
/// 일정 시간마다 랜덤 위치에서 사운드를 재생하는 매니저.
/// </summary>
public class RandomMeshNotifier : MonoBehaviour
{
    [Header("Target Mesh Object")]
    [Tooltip("랜덤 사운드를 뿌릴 대상 오브젝트의 MeshFilter (예: Bedroom_facing)")]
    public MeshFilter targetMeshFilter;

    [Tooltip("Mesh의 local -> world 변환 기준 Transform (대부분 targetMeshFilter.transform)")]
    public Transform targetTransform;

    [Header("Sound Settings")]
    public string soundKey = "random-notify";

    [Header("Random Time Interval (sec)")]
    public float minInterval = 10f;
    public float maxInterval = 20f;

    [Range(0f, 1f)]
    [Tooltip("각 tick마다 실제로 재생할 확률")]
    public float playProbability = 0.7f;

    // --- 내부 상태 ---
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private float[] cumulativeAreas;
    private float totalArea;

    private Coroutine loopRoutine;

    private void Awake()
    {
        InitMeshData();
    }

    private void OnEnable()
    {
        if (loopRoutine == null)
            loopRoutine = StartCoroutine(NotifyLoop());
    }

    private void OnDisable()
    {
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }
    }

    private void InitMeshData()
    {
        if (targetMeshFilter == null)
        {
            Debug.LogWarning("[RandomMeshNotifierManager] targetMeshFilter가 비어 있습니다.");
            return;
        }

        if (targetTransform == null)
            targetTransform = targetMeshFilter.transform;

        mesh = targetMeshFilter.sharedMesh;
        if (mesh == null)
        {
            Debug.LogWarning("[RandomMeshNotifierManager] targetMeshFilter에 Mesh가 없습니다.");
            return;
        }

        vertices = mesh.vertices;
        triangles = mesh.triangles;

        int triCount = triangles.Length / 3;
        cumulativeAreas = new float[triCount];

        totalArea = 0f;
        for (int i = 0; i < triCount; i++)
        {
            int i0 = triangles[i * 3 + 0];
            int i1 = triangles[i * 3 + 1];
            int i2 = triangles[i * 3 + 2];

            Vector3 v0 = vertices[i0];
            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];

            float area = Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
            totalArea += area;
            cumulativeAreas[i] = totalArea;
        }

        if (totalArea <= 0f)
        {
            Debug.LogWarning("[RandomMeshNotifierManager] Mesh 면적이 0입니다. 메시가 정상인지 확인하세요.");
        }
    }

    private IEnumerator NotifyLoop()
    {
        while (true)
        {
            // 10~20초 사이 대기
            float wait = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(wait);

            // 확률 체크
            if (Random.value > playProbability)
                continue;

            if (suin_SoundManager.instance == null)
                continue;

            if (mesh == null || totalArea <= 0f)
            {
                InitMeshData();
                if (mesh == null || totalArea <= 0f)
                    continue;
            }

            // 메시 표면 위 랜덤 포인트
            Vector3 localPos = SamplePointOnMesh();
            Vector3 worldPos = targetTransform.TransformPoint(localPos);

            // 소리 재생 (pitch/volume 랜덤은 SoundManager 쪽에서 "random-notify"에만 적용하도록 이미 세팅)
            suin_SoundManager.instance.PlayAtPosition(soundKey, worldPos);
        }
    }

    private Vector3 SamplePointOnMesh()
    {
        float r = Random.value * totalArea;

        int triIndex = 0;
        for (int i = 0; i < cumulativeAreas.Length; i++)
        {
            if (r <= cumulativeAreas[i])
            {
                triIndex = i;
                break;
            }
        }

        int i0 = triangles[triIndex * 3 + 0];
        int i1 = triangles[triIndex * 3 + 1];
        int i2 = triangles[triIndex * 3 + 2];

        Vector3 v0 = vertices[i0];
        Vector3 v1 = vertices[i1];
        Vector3 v2 = vertices[i2];

        // barycentric random
        float u = Random.value;
        float v = Random.value;
        if (u + v > 1f)
        {
            u = 1f - u;
            v = 1f - v;
        }

        return v0 + u * (v1 - v0) + v * (v2 - v0);
    }
}
