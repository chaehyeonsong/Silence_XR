using UnityEngine;

public class SpiderCeilingFollowTarget : MonoBehaviour
{
    private enum SpiderState
    {
        CeilingMove,
        Drop,
        Land
    }

    [Header("Drop Rotate Settings")]
    public float dropRotateDuration = 0.3f;

    [Header("Drop Limits")]
    [Tooltip("드롭 시작점 기준 최대 낙하 길이. 여기에 도달하면 Game Over")]
    public float maxDropDistance = 1.5f;

    [Header("Kill Flag Lock")]
    public bool lockToTarget = false;

    [Header("Target Point")]
    public Transform targetPoint;

    [Header("Movement")]
    public float ceilingMoveSpeed = 2f;
    [Tooltip("Lock 상태일 때 이동 속도 배율")]
    public float chaseSpeedMultiplier = 2.5f;
    public float rotateSpeed = 7f;
    public float dropSpeed = 8f;

    [Header("Calm Return Settings")]
    public float calmTimeout = 15f;
    private float noFlagTimer = 0f;

    [Header("Ceiling Settings")]
    public LayerMask ceilingLayer;
    public float ceilingCheckDistance = 0.8f;
    public float ceilingStickOffset = 0.05f;

    [Header("Ground Settings (외부 호환용)")]
    public LayerMask groundLayer;      

    [Header("Drop Settings (외부 호환용)")]
    [Tooltip("이 수평 반경 안으로 들어오면 Drop 시작")]
    public float dropHorizontalRadius = 1.0f; 

    [Header("Web Line Settings")]
    public LineRenderer webLine;
    [Tooltip("드롭 시작점에서 줄을 그릴지 여부 (시각효과용)")]
    public bool keepWebFromDropStart = true;

    [Header("Roof Area")]
    public MeshRenderer roofMesh;
    private Bounds roofBounds;

    [Header("Return Home")]
    public Transform spawnPoint;
    public float returnArriveRadius = 0.2f;

    [Header("Idle Wander")]
    public bool useRandomWander = true;
    public float wanderDirChangeInterval = 3f;

    // 내부 상태 변수들
    private SpiderState state = SpiderState.CeilingMove;
    private Vector3 fixedCeilingNormal;

    // Drop 회전 관련
    private bool isDropRotating = false;
    private Quaternion dropStartRot;
    private Quaternion dropTargetRot;
    private float dropRotateTimer = 0f;

    // Web / Drop 관련
    private bool isWebActive = false;
    private Vector3 webStartPos;   
    private Vector3 dropOrigin;    

    // Flag / 상태
    private bool isAlerted = false;
    private suin_FlagHub hub;

    // Wander
    private Vector3 wanderDir;
    private float wanderTimer = 0f;
    private bool isReturningHome = false;

    // 중복 호출 방지용
    private bool hasTriggeredGameOver = false;

    void OnEnable()
    {
        if (suin_FlagHub.instance != null) SubscribeToHub();
    }

    void Start()
    {
        if (hub == null && suin_FlagHub.instance != null) SubscribeToHub();

        // 시작 시 불 켜짐 체크
        var currentHub = hub != null ? hub : suin_FlagHub.instance;
        if (currentHub != null && currentHub.LightOn)
        {
            Debug.Log("🕷️ [Spider] 시작 시 불 켜짐 감지! 즉시 추격 모드.");
            OnAlertFlag(true);
        }

        if (roofMesh == null)
        {
            GameObject foundRoof = GameObject.Find("Bedroom_roof");
            if (foundRoof != null) roofMesh = foundRoof.GetComponent<MeshRenderer>();
        }

        if (webLine != null)
        {
            webLine.positionCount = 0;
            webLine.enabled = false;
        }

        AttachToCeiling_And_FixNormal();
        if (roofMesh != null) roofBounds = roofMesh.bounds;

        wanderDir = transform.forward;
        wanderTimer = wanderDirChangeInterval;
    }

    void SubscribeToHub()
    {
        hub = suin_FlagHub.instance;
        if (hub != null)
        {
            hub.OnMoveSlightFlag += OnAlertFlag;
            hub.OnPlayerSoundFlag += OnAlertFlag;
            hub.OnWaterSoundFlag += OnAlertFlag;
            hub.OnLightStateChanged += OnAlertFlag;
        }
    }

    void OnDisable()
    {
        if (hub != null)
        {
            hub.OnMoveSlightFlag -= OnAlertFlag;
            hub.OnPlayerSoundFlag -= OnAlertFlag;
            hub.OnWaterSoundFlag -= OnAlertFlag;
            hub.OnLightStateChanged -= OnAlertFlag;
        }
    }

    // ==========================================
    // 🔥 [수정 완료] 신호 처리 로직
    // ==========================================
    void OnAlertFlag(bool v)
    {
        if (lockToTarget) return;

        if (v)
        {
            // [True 신호] 무조건 추적
            isReturningHome = false;
            noFlagTimer = 0f;
            isAlerted = true;
        }
        else
        {
            // [False 신호] 소리나 움직임이 멈춤

            // 🔥 핵심 수정 사항 🔥
            // 불이 켜져 있다면(LightOn) 다른 신호가 꺼져도 경계를 풀지 않음
            if (hub != null && hub.LightOn)
            {
                return;
            }

            // 불도 꺼져 있고 신호도 없어야 경계 해제
            isAlerted = false;
        }
    }

    void Update()
    {
        if (lockToTarget)
        {
            isAlerted = true;
            isReturningHome = false;
        }
        else
        {
            if (!isAlerted && !isReturningHome)
            {
                noFlagTimer += Time.deltaTime;
                if (noFlagTimer >= calmTimeout && spawnPoint != null)
                {
                    isReturningHome = true;
                    isAlerted = false;

                    if (state == SpiderState.Drop) state = SpiderState.CeilingMove;

                    if (webLine != null) { webLine.enabled = false; webLine.positionCount = 0; }
                    isWebActive = false;
                }
            }
            else if (isAlerted)
            {
                noFlagTimer = 0f; 
            }
        }

        switch (state)
        {
            case SpiderState.CeilingMove:
                MoveOnCeiling();
                break;
            case SpiderState.Drop:
                DropDown(); 
                break;
            case SpiderState.Land:
                break;
        }
    }

    void AttachToCeiling_And_FixNormal()
    {
        if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit hit, ceilingCheckDistance, ceilingLayer))
        {
            Vector3 n = hit.normal;
            if (Vector3.Dot(n, Vector3.down) < 0f) n = -n;
            fixedCeilingNormal = n;
            transform.position = hit.point + n * ceilingStickOffset;
            transform.rotation = Quaternion.LookRotation(transform.forward, fixedCeilingNormal);
        }
        else
        {
            fixedCeilingNormal = Vector3.down;
        }
    }

    void MoveOnCeiling()
    {
        if (lockToTarget) isReturningHome = false;

        if (isReturningHome && spawnPoint != null)
        {
            MoveToTarget(spawnPoint.position, ceilingMoveSpeed);
            if (Vector3.Distance(GetXZ(transform.position), GetXZ(spawnPoint.position)) <= returnArriveRadius)
                Destroy(gameObject);
            return;
        }

        if (!isAlerted || targetPoint == null)
        {
            if (useRandomWander && !lockToTarget) CeilingIdleWander();
            else MaintainCeilingAttachment();
            return;
        }

        float currentSpeed = lockToTarget ? (ceilingMoveSpeed * chaseSpeedMultiplier) : ceilingMoveSpeed;
        MoveToTarget(targetPoint.position, currentSpeed);

        float dist = Vector3.Distance(GetXZ(transform.position), GetXZ(targetPoint.position));
        if (dist <= dropHorizontalRadius)
        {
            StartDrop();
        }
    }

    void MoveToTarget(Vector3 targetPos, float speed)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            dir.Normalize();
            Quaternion targetRot = Quaternion.LookRotation(dir, fixedCeilingNormal);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        Vector3 next = transform.position + transform.forward * speed * Time.deltaTime;
        next = ClampToRoofXZ(next);
        transform.position = next;
        MaintainCeilingAttachment();
    }

    void StartDrop()
    {
        isDropRotating = true;
        dropRotateTimer = 0f;
        dropStartRot = transform.rotation;
        dropTargetRot = transform.rotation * Quaternion.Euler(-90f, 0f, 0f);
        dropOrigin = transform.position;

        if (webLine != null)
        {
            isWebActive = true;
            webLine.enabled = true;
            webLine.positionCount = 2;
            webStartPos = keepWebFromDropStart ? transform.position : (targetPoint != null ? targetPoint.position : transform.position);
            webLine.SetPosition(0, webStartPos);
            webLine.SetPosition(1, transform.position);
        }

        state = SpiderState.Drop;
        Debug.Log("🕷️ [Spider] Drop 시작! (-Y 방향 하강)");
    }

    void CeilingIdleWander()
    {
        if (roofMesh == null)
        {
            MaintainCeilingAttachment();
            return;
        }
        roofBounds = roofMesh.bounds;
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f || wanderDir == Vector3.zero)
        {
            wanderTimer = wanderDirChangeInterval * 2f;
            Vector2 r2 = Random.insideUnitCircle.normalized;
            wanderDir = new Vector3(r2.x, 0f, r2.y);
        }

        if (wanderDir != Vector3.zero)
        {
            Quaternion tr = Quaternion.LookRotation(wanderDir, fixedCeilingNormal);
            transform.rotation = Quaternion.Lerp(transform.rotation, tr, rotateSpeed * Time.deltaTime);
        }

        Vector3 next = transform.position + transform.forward * ceilingMoveSpeed * Time.deltaTime;
        next = ClampToRoofXZ(next);
        transform.position = next;
        MaintainCeilingAttachment();
    }

    Vector3 ClampToRoofXZ(Vector3 pos)
    {
        if (roofMesh == null) return pos;
        roofBounds = roofMesh.bounds;
        pos.x = Mathf.Clamp(pos.x, roofBounds.min.x, roofBounds.max.x);
        pos.z = Mathf.Clamp(pos.z, roofBounds.min.z, roofBounds.max.z);
        return pos;
    }

    void MaintainCeilingAttachment()
    {
        if (Physics.Raycast(transform.position, -fixedCeilingNormal, out RaycastHit hit, ceilingCheckDistance, ceilingLayer))
        {
            Vector3 n = hit.normal;
            if (Vector3.Dot(n, Vector3.down) < 0f) n = -n;
            transform.position = hit.point + n * ceilingStickOffset;
            transform.rotation = Quaternion.LookRotation(transform.forward, fixedCeilingNormal);
        }
    }

    void DropDown()
    {
        float step = dropSpeed * Time.deltaTime;

        if (isDropRotating)
        {
            dropRotateTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dropRotateTimer / dropRotateDuration);
            transform.rotation = Quaternion.Slerp(dropStartRot, dropTargetRot, t);
            if (t >= 1f) isDropRotating = false;
        }

        Vector3 proposed = transform.position + Vector3.down * step;
        float nextLen = Vector3.Distance(proposed, dropOrigin);

        if (nextLen >= maxDropDistance)
        {
            Vector3 dir = (proposed - dropOrigin).normalized;
            Vector3 clampedPos = dropOrigin + dir * maxDropDistance;
            transform.position = clampedPos;

            if (isWebActive && webLine != null)
            {
                webLine.SetPosition(0, webStartPos);
                webLine.SetPosition(1, transform.position);
            }

            state = SpiderState.Land;
            if (!hasTriggeredGameOver && hub != null)
            {
                hasTriggeredGameOver = true;
                hub.TriggerPlayerKillFlag(); 
            }
            return;
        }

        transform.position = proposed;

        if (isWebActive && webLine != null)
        {
            webLine.SetPosition(0, webStartPos);
            webLine.SetPosition(1, transform.position);
        }
    }

    Vector3 GetXZ(Vector3 v) => new Vector3(v.x, 0, v.z);

    public void SetTarget(Transform target)
    {
        targetPoint = target;
    }

    public void ForceLockToTarget(Transform target)
    {
        targetPoint = target;
        lockToTarget = true;
        isAlerted = true;
        isReturningHome = false;
        Debug.Log("🕷️ [Spider] 강제 타겟 고정 (Kill Mode)");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (roofMesh != null)
        {
            Gizmos.color = Color.cyan;
            Bounds b = roofMesh.bounds;
            Vector3 center = b.center;
            Vector3 size = b.size;
            size.y = 0.01f;
            Gizmos.DrawWireCube(center, size);
        }
    }
#endif
}