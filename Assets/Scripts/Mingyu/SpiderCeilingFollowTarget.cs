using UnityEngine;

public class SpiderCeilingFollowTarget : MonoBehaviour
{
    private enum SpiderState
    {
        CeilingMove,
        Drop,
        Land
    }

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

    [Header("Ground Settings")]
    public LayerMask groundLayer;      // 🚨 Inspector에서 'Default'나 바닥 레이어 꼭 체크하세요!
    public float groundCheckDistance = 1.0f;
    public float groundStickOffset = 0.05f;

    [Header("Drop Settings")]
    [Tooltip("이 거리 안으로 들어오면 Drop 시작")]
    public float dropHorizontalRadius = 1.0f;
    public float dropRotateDuration = 0.3f;

    [Header("Web Line Settings")]
    public LineRenderer webLine;
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

    private SpiderState state = SpiderState.CeilingMove;
    private Vector3 fixedCeilingNormal;

    // Drop 회전
    private bool isDropRotating = false;
    private Quaternion dropStartRot;
    private Quaternion dropTargetRot;
    private float dropRotateTimer = 0f;

    // Web
    private bool isWebActive = false;
    private Vector3 webStartPos;

    // Flag
    private bool isAlerted = false;
    private suin_FlagHub hub;

    // Wander
    private Vector3 wanderDir;
    private float wanderTimer = 0f;
    private bool isReturningHome = false;

    void OnEnable()
    {
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
        if (hub != null)
        {
            hub.OnMoveSlightFlag -= OnAlertFlag;
            hub.OnPlayerSoundFlag -= OnAlertFlag;
            hub.OnWaterSoundFlag -= OnAlertFlag;
        }
    }

    void OnAlertFlag(bool v)
    {
        if (isReturningHome) return;
        if (lockToTarget) return;

        isAlerted = v;
        if (v) noFlagTimer = 0f;
    }

    void Start()
    {
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

    void Update()
    {
        if (lockToTarget)
        {
            isAlerted = true;
            isReturningHome = false;
        }
        else
        {
            noFlagTimer += Time.deltaTime;
            if (!isReturningHome && noFlagTimer >= calmTimeout && spawnPoint != null)
            {
                isReturningHome = true;
                isAlerted = false;
                if (state == SpiderState.Drop) state = SpiderState.CeilingMove;
                if (webLine != null) { webLine.enabled = false; webLine.positionCount = 0; }
                isWebActive = false;
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

    // ───────────────────────────────────────────────────────────────
    // Drop 시작: 가던 방향 유지 + X축 -90도 회전 (아래 보기)
    // ───────────────────────────────────────────────────────────────
    void StartDrop()
    {
        isDropRotating = true;
        dropRotateTimer = 0f;
        dropStartRot = transform.rotation;

        // "현재 회전값 * X축 -90도" (유저분이 원하시는 -y 바라보기)
        dropTargetRot = transform.rotation * Quaternion.Euler(-90f, 0f, 0f);

        if (webLine != null)
        {
            isWebActive = true;
            webLine.enabled = true;
            webLine.positionCount = 2;
            webStartPos = keepWebFromDropStart ? transform.position : targetPoint.position;
            webLine.SetPosition(0, webStartPos);
            webLine.SetPosition(1, transform.position);
        }
        state = SpiderState.Drop;
        
        Debug.Log("🕷️ [Spider] Drop 시작! (-Y 방향 하강)");
    }

    void CeilingIdleWander()
    {
        if (roofMesh == null) { MaintainCeilingAttachment(); return; }
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

    // ───────────────────────────────────────────────────────────────
    // 🔥 [수정됨] 바닥 뚫림 방지 로직 (Ray를 위에서 아래로 쏨)
    // ───────────────────────────────────────────────────────────────
    void DropDown()
    {
        float step = dropSpeed * Time.deltaTime;

        // 1) 회전
        if (isDropRotating)
        {
            dropRotateTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dropRotateTimer / dropRotateDuration);
            transform.rotation = Quaternion.Slerp(dropStartRot, dropTargetRot, t);
            if (t >= 1f) isDropRotating = false;
        }

        // 2) 바닥 감지 (안전 장치: 거미 위치보다 1.5m 위에서부터 쏨)
        // 이렇게 하면 거미가 바닥에 살짝 파묻혀 있어도 위에서 쏜 Ray에 걸립니다.
        float rayStartOffset = 1.5f; 
        Vector3 rayOrigin = transform.position + Vector3.up * rayStartOffset;
        
        // 탐지 거리: 오프셋(1.5) + 안전거리(1.0) + 이동속도 고려(step*2)
        float rayLength = rayStartOffset + groundCheckDistance + (step * 2f);

        // 씬 뷰에서 빨간 선이 바닥에 닿는지 확인하세요!
        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.red);

        RaycastHit hit;
        // 반드시 -y 방향(Vector3.down)으로 쏨
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength, groundLayer))
        {
            // hit.point.y는 바닥의 높이입니다.
            // 현재 거미의 발 위치(transform.position.y)가 바닥 근처에 왔다면 멈춤
            
            // "현재 높이" - "이번 프레임 이동 거리" <= "바닥 높이 + 오프셋"
            if (transform.position.y - step <= hit.point.y + groundStickOffset)
            {
                // 위치를 바닥 표면 위로 딱 고정 (스냅)
                transform.position = new Vector3(transform.position.x, hit.point.y + groundStickOffset, transform.position.z);
                
                // 줄 업데이트
                if (isWebActive && webLine != null)
                {
                    webLine.SetPosition(0, webStartPos);
                    webLine.SetPosition(1, transform.position);
                }

                state = SpiderState.Land;
                Debug.Log($"🕷️ [Spider] 바닥 착지 완료! ({hit.collider.name})");
                return;
            }
        }

        // 3) 이동 (바닥이 아직 멀었으면 계속 하강)
        transform.position += Vector3.down * step;

        if (isWebActive && webLine != null)
        {
            webLine.SetPosition(0, webStartPos);
            webLine.SetPosition(1, transform.position);
        }
    }

    Vector3 GetXZ(Vector3 v) => new Vector3(v.x, 0, v.z);

    public void SetTarget(Transform target) { targetPoint = target; }

    public void ForceLockToTarget(Transform target)
    {
        targetPoint = target;
        lockToTarget = true;
        isAlerted = true;
        isReturningHome = false;
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