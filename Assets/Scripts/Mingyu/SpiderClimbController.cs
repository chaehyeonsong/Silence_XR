using UnityEngine;

public class SpiderClimbController : MonoBehaviour
{
    private enum SpiderState
    {
        Ground,     // 평지 걷기
        ClimbWall,  // 벽 타기
        ClimbUp,    // 턱 넘기기
        DropDown    // 낙하
    }

    private SpiderState state = SpiderState.Ground;

    [Header("Start Mode")]
    public bool startOnCeiling = true;   // 천장에서 시작할지 여부

    [Header("Path Settings")]
    public bool usePath = false;         // pointA 방향으로만 이동할지
    public Transform pointA;             // 이동 방향 기준점

    [Header("Target")]
    public Transform targetPoint;        // 스파이더가 향할 타겟

    [Header("Speed")]
    public float moveSpeed = 2f;
    public float wallClimbSpeed = 2f;
    public float climbUpSpeed = 2f;
    public float dropSpeed = 3f;

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask climbLayer;

    [Header("Ray Settings")]
    public float groundCheckDistance = 2f;
    public float forwardCheckDistance = 0.7f;
    public float forwardRayHeight = 0.3f;
    public float stickOffset = 0.05f;

    [Header("Drop Settings")]
    public float landingCheckDistance = 0.3f;

    private Vector3 wallNormal;
    private Vector3 climbDir;

    LayerMask WalkableMask => groundLayer | climbLayer;

    void Start()
    {
        // 시작 방향 설정
        if (usePath && pointA != null)
        {
            Vector3 dir = pointA.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
        else if (!usePath && targetPoint != null)
        {
            UpdateMoveDirToTarget();
        }

        // 천장에서 시작하는 경우: 위로 레이 쏴서 천장에 붙이기
        if (startOnCeiling)
        {
            AttachToCeiling();
            state = SpiderState.DropDown;    // 천장에 붙은 상태에서 떨어지기 시작
        }
        else
        {
            state = SpiderState.Ground;
        }

        Debug.Log("Spider State → " + state);
    }

    void Update()
    {
        Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, Color.yellow);

        switch (state)
        {
            case SpiderState.Ground:
                UpdateGround();
                break;
            case SpiderState.ClimbWall:
                UpdateClimbWall();
                break;
            case SpiderState.ClimbUp:
                UpdateClimbUp();
                break;
            case SpiderState.DropDown:
                UpdateDropDown();
                break;
        }
    }

    void SetState(SpiderState newState)
    {
        if (state == newState) return;

        if (newState == SpiderState.DropDown)
            AlignForFall();

        state = newState;
        Debug.Log("Spider State → " + state);
    }

    void AlignForFall()
    {
        Vector3 fallForward = Vector3.down;
        Vector3 up = transform.forward;
        up.y = 0f;
        if (up.sqrMagnitude < 0.001f)
            up = Vector3.forward;

        up.Normalize();
        transform.rotation = Quaternion.LookRotation(fallForward, up);
    }

    void AlignForGround()
    {
        // 타겟이 있으면 타겟 방향으로 정렬
        if (targetPoint != null)
        {
            UpdateMoveDirToTarget();
            return;
        }

        // 타겟 없으면 그냥 평면 방향으로
        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.forward;
        flatForward.Normalize();
        transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
    }

    // ─────────────────────────── 천장 붙이기 ───────────────────────────
    void AttachToCeiling()
    {
        // 현재 위치에서 위로 레이를 쏴서 천장(climbLayer)을 찾음
        if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit hit, groundCheckDistance, climbLayer))
        {
            // 천장 표면으로 스냅
            transform.position = hit.point + hit.normal * stickOffset;

            // 천장 평면에서의 forward 기준 방향
            Vector3 forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, hit.normal);
            if (forwardOnPlane.sqrMagnitude < 0.001f)
            {
                forwardOnPlane = Vector3.Cross(hit.normal, Vector3.right);
            }
            forwardOnPlane.Normalize();

            // 스파이더의 "등"이 천장에 닿도록 up = -normal
            transform.rotation = Quaternion.LookRotation(forwardOnPlane, -hit.normal);

            Debug.Log("🕷️ Attached to ceiling at: " + hit.point);
        }
        else
        {
            // 위에 천장을 못 찾으면 그냥 뒤집어서 시작
            transform.rotation = Quaternion.LookRotation(transform.forward, -Vector3.up);
            Debug.LogWarning("⚠️ No ceiling found above spider. Just flipped 180°.");
        }
    }

    // ─────────────────────────── 평지 이동 ───────────────────────────
    void UpdateGround()
    {
        // 발 아래 땅에 붙이기
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, groundCheckDistance, WalkableMask))
            transform.position = groundHit.point + Vector3.up * stickOffset;
        else
        {
            SetState(SpiderState.DropDown);
            return;
        }

        // 타겟이 있으면 매 프레임 타겟 방향으로 회전
        if (!usePath && targetPoint != null)
        {
            UpdateMoveDirToTarget();
        }

        // 앞으로 전진
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        // 벽 감지해서 타기 시작
        if (TryDetectWall(out RaycastHit wallHit))
        {
            float wallAngle = Vector3.Angle(wallHit.normal, Vector3.up);
            float approachAngle = Vector3.Angle(transform.forward, -wallHit.normal);

            // 수직면 + 어느 정도 정면으로 접근일 때만 벽으로 인식
            if (wallAngle > 70f && approachAngle < 70f)
            {
                wallNormal = wallHit.normal;
                climbDir = Vector3.ProjectOnPlane(Vector3.up, wallNormal).normalized;
                if (Vector3.Dot(climbDir, Vector3.up) < 0) climbDir = -climbDir;

                transform.rotation = Quaternion.LookRotation(climbDir, wallNormal);
                transform.position = wallHit.point + wallNormal * stickOffset;

                SetState(SpiderState.ClimbWall);
            }
        }
    }

    // ─────────────────────────── 벽 타기 ───────────────────────────
    void UpdateClimbWall()
    {
        if (Physics.Raycast(transform.position, -wallNormal, out RaycastHit hit, 1f, climbLayer))
        {
            transform.position = hit.point + wallNormal * stickOffset;
            transform.position += climbDir * wallClimbSpeed * Time.deltaTime;
        }
        else
        {
            SetState(SpiderState.ClimbUp);
        }
    }

    // ─────────────────────────── 턱 넘기기 ───────────────────────────
    void UpdateClimbUp()
    {
        Vector3 overDir = (Vector3.up - wallNormal).normalized;
        transform.position += overDir * climbUpSpeed * Time.deltaTime;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, groundCheckDistance * 3f, WalkableMask))
        {
            transform.position = groundHit.point + Vector3.up * stickOffset;
            AlignForGround();
            SetState(SpiderState.Ground);
        }
    }

    // ─────────────────────────── 낙하 ───────────────────────────
    void UpdateDropDown()
    {
        transform.position += Vector3.down * dropSpeed * Time.deltaTime;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, landingCheckDistance, WalkableMask))
        {
            transform.position = hit.point + Vector3.up * stickOffset;
            AlignForGround();
            SetState(SpiderState.Ground);
        }
    }

    // ─────────────────────────── 벽 감지 ───────────────────────────
    bool TryDetectWall(out RaycastHit wallHit)
    {
        Vector3 origin = transform.position + Vector3.up * forwardRayHeight;
        Vector3[] dirs =
        {
            transform.forward,
            Quaternion.Euler(0, 15f, 0) * transform.forward,
            Quaternion.Euler(0, -15f, 0) * transform.forward
        };

        foreach (var dir in dirs)
        {
            if (Physics.SphereCast(origin, 0.2f, dir, out wallHit, forwardCheckDistance, climbLayer))
                return true;
        }

        wallHit = default;
        return false;
    }

    // ─────────────────────────── 타겟 방향 정렬 ───────────────────────────
    void UpdateMoveDirToTarget()
    {
        if (targetPoint == null) return;

        Vector3 dir = targetPoint.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        dir.Normalize();
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    // Spawner에서 호출할 메서드
    public void SetTarget(Transform target)
    {
        targetPoint = target;
        UpdateMoveDirToTarget();
    }
}
