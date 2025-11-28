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
    public float wanderRadius = 8f;        // Fallback 반경
    public float wanderInterval = 8f;      // 새 목적지를 고르는 최소 간격(초)

    [Tooltip("새 wander 목적지가 현재 위치와 최소 이 정도는 떨어지도록 강제")]
    public float minWanderDistance = 4f;   // 너무 짧은 이동 방지

    [Header("Wander Area (옵션: 이 MeshRenderer bounds 안에서만 배회)")]
    public MeshRenderer wanderAreaMesh;    // 바닥/방 MeshRenderer 넣어주면 됨

    [Header("Alert Settings (플래그 들어오면 추적 시작)")]
    public bool useAlert = true;           // suin_FlagHub 플래그 연동 여부

    private NavMeshAgent agent;

    // 플래그 관련
    private suin_FlagHub hub;
    private bool isAlerted = false;        // 현재 Alert 상태 (허브에서 true/false 들어옴)

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

        // ⚠️ speed는 절대 코드에서 건드리지 않음 (Inspector 값 그대로 사용)
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

        isAlerted = v;  // true면 경계모드, false면 다시 idle

        if (isAlerted && targetPoint != null)
        {
            SetDestinationToTarget();
        }
        else if (!isAlerted && useRandomWander)
        {
            // 경계 해제되면 배회로 자연스럽게 돌아가게
            agent.ResetPath();  // 이전 추적 경로 끊기
        }
    }

    void Start()
    {
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

        // 🔹 같은 목적지로 더 오래 가도록: 인터벌을 2배로
        wanderTimer = wanderInterval * 2f;

        Vector3 rawTarget = transform.position;

        if (wanderAreaMesh != null)
        {
            // 🔹 MeshRenderer bounds 안에서 랜덤 위치 선택 (너무 가까우면 다시 뽑기)
            var b = wanderAreaMesh.bounds;

            for (int i = 0; i < 8; i++)   // 최대 8번 정도 시도
            {
                float rx = Random.Range(b.min.x, b.max.x);
                float rz = Random.Range(b.min.z, b.max.z);
                Vector3 candidate = new Vector3(rx, transform.position.y, rz);

                // 현재 위치와 XZ 거리
                Vector2 diffXZ = new Vector2(
                    candidate.x - transform.position.x,
                    candidate.z - transform.position.z
                );

                if (diffXZ.magnitude >= minWanderDistance)
                {
                    rawTarget = candidate;
                    break;
                }
            }

            Debug.Log(
                $"[Zombie IdleWander] {name} area={wanderAreaMesh.name} rawTarget={rawTarget}"
            );
        }
        else
        {
            // 🔹 fallback: 원형 반경 (역시 최소 거리 보장)
            for (int i = 0; i < 8; i++)
            {
                Vector2 dir2 = Random.insideUnitCircle.normalized;
                float radius = wanderRadius;
                Vector3 candidate = wanderCenter + new Vector3(dir2.x, 0f, dir2.y) * radius;

                Vector2 diffXZ = new Vector2(
                    candidate.x - transform.position.x,
                    candidate.z - transform.position.z
                );

                if (diffXZ.magnitude >= minWanderDistance)
                {
                    rawTarget = candidate;
                    break;
                }
            }

            Debug.Log(
                $"[Zombie IdleWander] {name} area=NULL (use radius {wanderRadius}) " +
                $"wanderCenter={wanderCenter} rawTarget={rawTarget}"
            );
        }

        if (NavMesh.SamplePosition(rawTarget, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.stoppingDistance = 0f;
            agent.SetDestination(hit.position);

            Debug.Log($"[Zombie IdleWander] {name} -> wander dest (NavMesh) = {hit.position}");
        }
        else
        {
            Debug.Log($"[Zombie IdleWander] {name} -> failed to find NavMesh near {rawTarget}");
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
