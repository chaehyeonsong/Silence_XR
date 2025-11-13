using UnityEngine;
using UnityEngine.AI;

public class ZombieNavPatrol : MonoBehaviour
{
    [Header("Waypoints in order: A, B, C, D")]
    public Transform[] points;          // 순찰 포인트들 (A, B, C, D...)
    public float arriveDistance = 0.35f; // 다음 포인트 도착 판정 거리

    private NavMeshAgent agent;
    private int idx = 0;                // 현재 목표 인덱스
    private bool singlePointMode = false; // 포인트 1개일 때 true

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // NavMeshAgent 설정
        agent.stoppingDistance = arriveDistance;
        agent.autoRepath = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    void Start()
    {
        // 포인트가 설정되지 않았으면 종료
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning("⚠️ No patrol points assigned.");
            enabled = false;
            return;
        }

        // 포인트가 하나뿐이면 단일 이동 모드
        if (points.Length == 1)
        {
            singlePointMode = true;
            agent.SetDestination(points[0].position);
            Debug.Log("🧟 Zombie moving to single patrol point: " + points[0].name);
            return;
        }

        // 2개 이상일 때는 순찰 시작
        SetNext();
        Debug.Log("🧟 Zombie patrol started. Total points: " + points.Length);
    }

    void Update()
    {
        // 포인트가 하나일 때는 도착하면 멈추기
        if (singlePointMode)
        {
            if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
            {
                agent.isStopped = true;
                Debug.Log("🧟 Zombie reached the single patrol point.");
            }
            return;
        }

        // 순찰 모드 (2개 이상 포인트)
        if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
        {
            SetNext();
        }

        // 경로가 끊겼거나 유효하지 않으면 복구
        if (agent.isPathStale || agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            agent.ResetPath();
            SetNext();
        }
    }

    void SetNext()
    {
        if (points.Length == 0) return;

        // 다음 목적지 설정
        agent.SetDestination(points[idx].position);
        Debug.Log($"🧭 Moving to patrol point {idx + 1}/{points.Length}: {points[idx].name}");

        // 다음 인덱스로 순환
        idx = (idx + 1) % points.Length;
    }
}
