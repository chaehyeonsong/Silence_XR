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

    [Header("Game Over Settings")]
    [Tooltip("이 거리 안에 들어오면 게임오버 발동")]
    public float killTriggerDistance = 1.0f; 
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
        // Start보다 먼저 실행될 수 있으므로 여기서도 체크
        if (suin_FlagHub.instance != null) SubscribeToHub();
    }

    void Start()
    {
        // 1. 구독 확인 (OnEnable에서 못 했을 경우)
        if (hub == null && suin_FlagHub.instance != null) SubscribeToHub();

        // 2. [수정됨] 태어나자마자 "지금 불 켜져 있나?" 확인
        // 불이 이미 켜져 있다면, 이벤트를 기다리지 않고 즉시 추적 모드로 진입합니다.
        if (useAlert && hub != null && hub.LightOn)
        {
            Debug.Log($"🧟 [Zombie] {name}: 시작부터 불이 켜져있음 감지! 즉시 추적.");
            // 강제로 True 신호를 받은 것처럼 처리
            OnAlertFlag(true); 
        }

        // 3. 타겟 설정 확인
        if (targetPoint != null && (isAlerted || lockToTarget))
        {
            SetDestinationToTarget();
        }
    }

    // 허브 이벤트 구독 함수
    void SubscribeToHub()
    {
        hub = suin_FlagHub.instance;
        hub.OnMoveSlightFlag += OnAlertFlag;
        hub.OnPlayerSoundFlag += OnAlertFlag;
        hub.OnWaterSoundFlag += OnAlertFlag;
        hub.OnLightStateChanged += OnAlertFlag;
    }

    void OnDisable()
    {
        if (!useAlert) return;
        if (hub != null)
        {
            hub.OnMoveSlightFlag -= OnAlertFlag;
            hub.OnPlayerSoundFlag -= OnAlertFlag;
            hub.OnWaterSoundFlag -= OnAlertFlag;
            hub.OnLightStateChanged -= OnAlertFlag;
        }
    }

    // ==========================================
    // 🔥 [핵심 수정] 신호 처리 로직
    // ==========================================
    void OnAlertFlag(bool v)
    {
        if (!useAlert) return;
        if (lockToTarget) return; // 이미 죽이러 가는 중이면 다른 신호 무시

        if (v)
        {
            // 신호가 켜짐 (True)
            // [수정] "집에 가는 중(isReturningHome)"이었더라도, 즉시 취소하고 다시 추적합니다.
            isReturningHome = false; 
            isAlerted = true;
            noFlagTimer = 0f; // 타이머 리셋
            
            if (targetPoint != null)
                SetDestinationToTarget();
        }
        else
        {
            // 신호가 꺼짐 (False)
            // 바로 집에 가는 게 아니라, Update에서 타이머가 찰 때까지 기다립니다.
            isAlerted = false;
            if (!v && useRandomWander && agent != null)
            {
                agent.ResetPath();
            }
        }
    }

    void Update()
    {
        if (agent == null) return;

        // 거리 계산
        float dist = 0f;
        if (targetPoint != null)
        {
            dist = Vector3.Distance(transform.position, targetPoint.position);
        }

        // --- 게임오버 체크 ---
        if (targetPoint != null && (lockToTarget || isAlerted))
        {
            if (dist <= killTriggerDistance && !hasTriggeredGameOver)
            {
                hasTriggeredGameOver = true;
                // Debug.Log($"🧟 [Zombie] 잡았다! 거리: {dist:F2}");
                if (suin_FlagHub.instance != null)
                {
                    suin_FlagHub.instance.TriggerPlayerKillFlag();
                }
            }
        }

        // 1. 죽는 플래그 (Lock Mode) - 무조건 추적
        if (lockToTarget)
        {
            isReturningHome = false;
            isAlerted = true;
            noFlagTimer = 0f;
            agent.speed = initialSpeed * chaseSpeedMultiplier;

            if (targetPoint != null)
            {
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
            return; // Lock 모드면 여기서 끝
        }
        else
        {
            agent.speed = initialSpeed;
        }

        // 2. Calm Check (평화 복귀 체크)
        // [수정] 경계 상태(isAlerted)가 아닐 때만 타이머가 흐릅니다.
        // 불이 켜져 있는 동안(isAlerted == true)에는 타이머가 0으로 고정되어 집에 가지 않습니다.
        if (!isAlerted && !isReturningHome)
        {
            noFlagTimer += Time.deltaTime;
            if (noFlagTimer >= calmTimeout && spawnPoint != null)
            {
                isReturningHome = true;
                isAlerted = false;
                agent.ResetPath();
                // Debug.Log("🧟 [Zombie] 너무 조용해서 집으로 돌아갑니다.");
            }
        }
        else if (isAlerted)
        {
            noFlagTimer = 0f; // 경계 중이면 타이머 리셋
        }

        // 3. Return Home (집으로 복귀)
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
                Destroy(gameObject); // 도착하면 사라짐
            }
            return;
        }

        // 4. Alert Chase (일반 추적)
        if (isAlerted && targetPoint != null)
        {
            agent.isStopped = false;
            agent.stoppingDistance = arriveDistance;
            agent.SetDestination(targetPoint.position);
            return;
        }

        // 5. Idle Wander (배회)
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