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

    [Header("Path Settings")]
    public bool usePath = false;   // A 방향으로만 이동
    public Transform pointA;       // 이동 방향 기준점

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
        Debug.Log("Spider State → " + state);

        if (usePath && pointA != null)
        {
            Vector3 dir = pointA.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
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
        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.forward;
        flatForward.Normalize();
        transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
    }

    // ─────────────────────────── 평지 이동 ───────────────────────────
    void UpdateGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, groundCheckDistance, WalkableMask))
            transform.position = groundHit.point + Vector3.up * stickOffset;
        else
        {
            SetState(SpiderState.DropDown);
            return;
        }

        transform.position += transform.forward * moveSpeed * Time.deltaTime;

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

    // ─────────────────────────── 벽 감지 로직 ───────────────────────────
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
            // SphereCast로 감지 범위를 약간 늘림 (비스듬히 접근 보정)
            if (Physics.SphereCast(origin, 0.2f, dir, out wallHit, forwardCheckDistance, climbLayer))
                return true;
        }

        wallHit = default;
        return false;
    }
}
