using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieNavTarget : MonoBehaviour
{
    [Header("Target")]
    public Transform targetPoint;          // 좀비가 달려갈 목적지
    public float arriveDistance = 0.35f;   // 도착 판정 거리

    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // NavMeshAgent 기본 설정
        agent.stoppingDistance = arriveDistance;
        agent.autoRepath = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    void Start()
    {
        // 만약 프리팹에 미리 targetPoint가 박혀 있으면 여기서도 한 번 세팅
        if (targetPoint != null)
        {
            SetDestinationToTarget();
        }
    }

    // ✅ Spawner에서 호출할 메서드
    public void SetTarget(Transform target)
    {
        targetPoint = target;

        if (targetPoint == null)
        {
            Debug.LogWarning($"⚠️ {name} tried to SetTarget(null)");
            return;
        }

        SetDestinationToTarget();
    }

    // NavMeshAgent 목적지 실제로 설정하는 내부 함수
    void SetDestinationToTarget()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (targetPoint == null)
            return;

        agent.isStopped = false;
        agent.stoppingDistance = arriveDistance;
        agent.SetDestination(targetPoint.position);

        Debug.Log($"🧟 {name} moving to target: {targetPoint.name}");
    }
}
