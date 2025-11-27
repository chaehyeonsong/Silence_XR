using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieNavTarget : MonoBehaviour
{
    [Header("Target")]
    public Transform targetPoint;          // 좀비가 달려갈 목적지
    public float arriveDistance = 0.35f;   // 도착 판정 거리

    [Header("Idle Wander Settings (경계 전 상태)")]
    public bool useRandomWander = true;    // Alert 전 랜덤 배회할지 여부

    [Tooltip("wanderAreaMesh가 없을 때 사용할 반경 (초기 위치 기준)")]
    public float wanderRadius = 5f;        // Fallback 반경
    public float wanderInterval = 2f;      // 새 목적지를 고르는 최소 간격(초)

    [Header("Wander Area (옵션: 이 MeshRenderer bounds 안에서만 배회)")]
    public MeshRenderer wanderAreaMesh;    // 바닥/방 MeshRenderer 넣어주면 됨

    [Header("Alert Settings (플래그 들어오면 추적 시작)")]
    public bool useAlert = true;           // suin_FlagHub 플래그 연동 여부

    private NavMeshAgent agent;

    // 플래그 관련
    private suin_FlagHub hub;
    private bool isAlerted = false;        // 한 번이라도 플래그가 true면 true 유지

    // 배회 관련
    private Vector3 wanderCenter;
    private float wanderTimer = 0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // NavMeshAgent 기본 설정
        agent.stoppingDistance = arriveDistance;
        agent.autoRepath = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // 배회 중심은 초기 위치 기준
        wanderCenter = transform.position;
    }

    void OnEnable()
    {
        if (!useAlert) return;

        hub = suin_FlagHub.instance;
        if (hub != null)
        {
            hub.OnMoveSlightFlag  += OnAlertFlag;
            hub.OnPlayerSoundFlag += OnAlertFlag;
            hub.OnWaterSoundFlag  += OnAlertFlag;
        }
    }

    void OnDisable()
    {
        if (!useAlert) return;

        if (hub != null)
        {
            hub.OnMoveSlightFlag  -= OnAlertFlag;
            hub.OnPlayerSoundFlag -= OnAlertFlag;
            hub.OnWaterSoundFlag  -= OnAlertFlag;
        }
    }

    // 플래그 들어왔을 때 호출
    void OnAlertFlag(bool v)
    {
        if (!useAlert) return;

        if (v)
        {
            isAlerted = true;   // 한 번 true 되면 계속 경계 상태 유지
            if (targetPoint != null)
            {
                SetDestinationToTarget();
            }
        }
    }

    void Start()
    {
        // 미리 타겟 들어있고 이미 Alert 상태면 바로 추적
        if (targetPoint != null && isAlerted)
        {
            SetDestinationToTarget();
        }
    }

    void Update()
    {
        if (agent == null) return;

        // 1) Alert 이후 + 타겟 존재 → 타겟 추적
        if (isAlerted && targetPoint != null)
        {
            agent.isStopped = false;
            agent.stoppingDistance = arriveDistance;
            agent.SetDestination(targetPoint.position);
            return;
        }

        // 2) 아직 Alert 안 된 상태 → 랜덤 배회
        if (useRandomWander)
        {
            IdleWander();
        }
        else
        {
            agent.isStopped = true;
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

        if (isAlerted)
        {
            SetDestinationToTarget();
        }
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

    // ─────────────────────────────────────────────
    // Alert 되기 전: NavMesh 위를 랜덤하게 배회
    // ─────────────────────────────────────────────
    void IdleWander()
    {
        wanderTimer -= Time.deltaTime;

        bool needNewDest =
            !agent.hasPath ||
            agent.pathStatus != NavMeshPathStatus.PathComplete ||
            (!agent.pathPending && agent.remainingDistance <= arriveDistance) ||
            wanderTimer <= 0f;

        if (!needNewDest) return;

        wanderTimer = wanderInterval;

        Vector3 rawTarget;

        if (wanderAreaMesh != null)
        {
            // 🔹 MeshRenderer bounds 안에서 랜덤 위치 선택
            var b = wanderAreaMesh.bounds;
            float rx = Random.Range(b.min.x, b.max.x);
            float rz = Random.Range(b.min.z, b.max.z);
            rawTarget = new Vector3(rx, transform.position.y, rz);
        }
        else
        {
            // 🔹 fallback: 초기 위치 기준 반경 wanderRadius 안
            Vector3 randomDir = Random.insideUnitSphere;
            randomDir.y = 0f;
            randomDir *= wanderRadius;

            rawTarget = wanderCenter + randomDir;
        }

        // NavMesh 위의 가장 가까운 점 샘플링
        if (NavMesh.SamplePosition(rawTarget, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.stoppingDistance = 0f;  // 배회할 땐 딱 찍힌 위치까지
            agent.SetDestination(hit.position);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        if (wanderAreaMesh != null)
        {
            // Mesh bounds 시각화
            var b = wanderAreaMesh.bounds;
            Vector3 size = b.size;
            size.y = 0.01f;
            Gizmos.DrawWireCube(b.center, size);
        }
        else
        {
            // 반경 시각화 (fallback)
            Vector3 center = Application.isPlaying ? wanderCenter : transform.position;
            Gizmos.DrawWireSphere(center, wanderRadius);
        }
    }
#endif
}
