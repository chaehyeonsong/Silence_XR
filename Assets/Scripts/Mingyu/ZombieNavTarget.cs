using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class ZombieNavTarget : MonoBehaviour
{
    [Header("Kill Flag Lock")]
    public bool lockToTarget = false;        // 죽는 플래그 이후: 타겟에만 고정

    [Header("Movement Speed Settings")]
    [Tooltip("Lock(죽음 플래그) 상태일 때 이동 속도 배율 (기본 속도의 n배)")]
    public float chaseSpeedMultiplier = 2.5f;
    private float initialSpeed;              // 원래 속도 저장용

    [Header("Audio Settings")] 
    public AudioClip chaseSound;             // 달려들 때 재생할 사운드
    private AudioSource audioSource;         

    [Header("Calm Return Settings")]
    public float calmTimeout = 15f;          // flag 없으면 이 시간 뒤 귀환
    private float noFlagTimer = 0f;          // 마지막 flag 이후 경과 시간

    [Header("Target")]
    public Transform targetPoint;            // 좀비가 달려갈 목적지
    public float arriveDistance = 0.35f;     // 도착 판정 거리

    // ==========================================
    // ▼▼▼ 여기에 변수가 선언되어 있습니다 ▼▼▼
    // ==========================================
    [Header("Game Over Settings")]
    [Tooltip("이 거리 안에 들어오면 게임오버 발동")]
    public float killTriggerDistance = 1.0f; // 🔥 인스펙터에 보여야 함
    private bool hasTriggeredGameOver = false; // 중복 호출 방지용

    [Header("Idle Wander Settings (경계 전 상태)")]
    public bool useRandomWander = true;      // Alert 전 랜덤 배회할지 여부

    [Tooltip("wanderAreaMesh가 없을 때 사용할 반경 (초기 위치 기준)")]
    public float wanderRadius = 8f;          
    public float wanderInterval = 8f;        

    [Tooltip("새 wander 목적지가 현재 위치와 최소 이 정도는 떨어지도록 강제")]
    public float minWanderDistance = 4f;     

    [Header("Wander Area (옵션)")]
    public MeshRenderer wanderAreaMesh;      

    [Header("Alert Settings")]
    public bool useAlert = true;             

    [Header("Return Home Settings")]
    public Transform spawnPoint;             
    public float returnArriveDistance = 0.3f;

    private NavMeshAgent agent;
    private suin_FlagHub hub;
    private bool isAlerted = false;          
    private Vector3 wanderCenter;
    private float wanderTimer = 0f;
    private bool isReturningHome = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();

        // NavMeshAgent가 없는 경우를 대비한 안전장치
        if (agent != null)
        {
            agent.stoppingDistance = arriveDistance;
            agent.autoRepath = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            initialSpeed = agent.speed;
        }
        
        wanderCenter = transform.position;
    }

    void OnEnable()
    {
        if (!useAlert) return;
        hub = suin_FlagHub.instance;
        if (hub != null)
        {
            hub.OnMoveSlightFlag += OnAlertFlag;
            hub.OnPlayerSoundFlag += OnAlertFlag;
            hub.OnWaterSoundFlag += OnAlertFlag;
        }
    }

    void OnDisable()
    {
        if (!useAlert) return;
        if (hub != null)
        {
            hub.OnMoveSlightFlag -= OnAlertFlag;
            hub.OnPlayerSoundFlag -= OnAlertFlag;
            hub.OnWaterSoundFlag -= OnAlertFlag;
        }
    }

    void OnAlertFlag(bool v)
    {
        if (!useAlert) return;
        if (isReturningHome) return;
        if (lockToTarget) return;   

        isAlerted = v;

        if (v)
        {
            noFlagTimer = 0f;
            if (targetPoint != null)
                SetDestinationToTarget();
        }
        else if (!v && useRandomWander)
        {
            if(agent != null) agent.ResetPath();
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

        // 1. 죽는 플래그 (Lock Mode)
        if (lockToTarget)
        {
            isReturningHome = false;
            isAlerted = true;
            noFlagTimer = 0f;

            agent.speed = initialSpeed * chaseSpeedMultiplier;

            if (targetPoint != null)
            {
                float dist = Vector3.Distance(transform.position, targetPoint.position);

                // ▼▼▼ 거리 체크 및 게임오버 실행 ▼▼▼
                if (dist <= killTriggerDistance)
                {
                    if (!hasTriggeredGameOver)
                    {
                        hasTriggeredGameOver = true;
                        Debug.Log($"🧟 [Zombie] 잡았다! 거리: {dist:F2} <= {killTriggerDistance} -> 게임오버 요청");

                        if (suin_FlagHub.instance != null)
                        {
                            suin_FlagHub.instance.TriggerPlayerKillFlag();
                        }
                    }
                }
                // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

                if (dist <= arriveDistance)
                {
                    if (!agent.isStopped)
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                        agent.velocity = Vector3.zero;
                    }
                }
                else
                {
                    if (agent.isStopped) agent.isStopped = false;
                    agent.SetDestination(targetPoint.position);
                }
            }
            return; 
        }
        else
        {
            agent.speed = initialSpeed;
        }

        // 2. Calm Check
        noFlagTimer += Time.deltaTime;

        if (!isReturningHome && noFlagTimer >= calmTimeout && spawnPoint != null)
        {
            isReturningHome = true;
            isAlerted = false;
            agent.ResetPath();
        }

        // 3. Return Home
        if (isReturningHome)
        {
            if (spawnPoint == null)
            {
                Destroy(gameObject);
                return;
            }

            agent.isStopped = false;
            agent.stoppingDistance = 0f;
            agent.SetDestination(spawnPoint.position);

            if (!agent.pathPending && agent.remainingDistance <= returnArriveDistance)
            {
                Destroy(gameObject);
            }
            return;
        }

        // 4. Alert Chase
        if (isAlerted && targetPoint != null)
        {
            agent.isStopped = false;
            agent.stoppingDistance = arriveDistance;
            agent.SetDestination(targetPoint.position);
            return;
        }

        // 5. Idle Wander
        if (useRandomWander)
        {
            IdleWander();
        }
        else
        {
            agent.isStopped = true;
        }
    }

    public void SetTarget(Transform target)
    {
        targetPoint = target;
        if (targetPoint == null) return;

        if (isAlerted || lockToTarget)
        {
            SetDestinationToTarget();
        }
    }

    public void ForceLockToTarget(Transform target)
    {
        bool wasLocked = lockToTarget;

        targetPoint = target;
        lockToTarget = true;
        isAlerted = true;
        isReturningHome = false;
        noFlagTimer = 0f;

        if (agent != null && target != null)
        {
            agent.speed = initialSpeed * chaseSpeedMultiplier;
            agent.isStopped = false;
            agent.stoppingDistance = arriveDistance;
            agent.SetDestination(target.position);
        }

        if (!wasLocked && audioSource != null)
        {
            if (chaseSound != null) audioSource.clip = chaseSound;
            audioSource.Play();
        }
        Debug.Log($"🧟 {name} ForceLock 활성화 (타겟: {target.name})");
    }

    void SetDestinationToTarget()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (targetPoint == null) return;

        agent.isStopped = false;
        agent.stoppingDistance = arriveDistance;
        agent.SetDestination(targetPoint.position);
    }

    void IdleWander()
    {
        wanderTimer -= Time.deltaTime;

        bool needNewDest =
            !agent.hasPath ||
            agent.pathStatus != NavMeshPathStatus.PathComplete ||
            (!agent.pathPending && agent.remainingDistance <= arriveDistance) ||
            wanderTimer <= 0f;

        if (!needNewDest) return;

        wanderTimer = wanderInterval * 2f;
        Vector3 rawTarget = transform.position;

        if (wanderAreaMesh != null)
        {
            var b = wanderAreaMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                float rx = Random.Range(b.min.x, b.max.x);
                float rz = Random.Range(b.min.z, b.max.z);
                Vector3 candidate = new Vector3(rx, transform.position.y, rz);

                if (Vector3.Distance(candidate, transform.position) >= minWanderDistance)
                {
                    rawTarget = candidate;
                    break;
                }
            }
        }
        else
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 dir2 = Random.insideUnitCircle.normalized;
                Vector3 candidate = wanderCenter + new Vector3(dir2.x, 0f, dir2.y) * wanderRadius;

                if (Vector3.Distance(candidate, transform.position) >= minWanderDistance)
                {
                    rawTarget = candidate;
                    break;
                }
            }
        }

        if (NavMesh.SamplePosition(rawTarget, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.stoppingDistance = 0f;
            agent.SetDestination(hit.position);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 배회 범위 (초록)
        Gizmos.color = Color.green;
        if (wanderAreaMesh != null)
        {
            var b = wanderAreaMesh.bounds;
            Vector3 size = b.size;
            size.y = 0.01f;
            Gizmos.DrawWireCube(b.center, size);
        }
        else
        {
            Vector3 center = Application.isPlaying ? wanderCenter : transform.position;
            Gizmos.DrawWireSphere(center, wanderRadius);
        }

        // 킬 트리거 범위 (빨강)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, killTriggerDistance);
    }
#endif
}