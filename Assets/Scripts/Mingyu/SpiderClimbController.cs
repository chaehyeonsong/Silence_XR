using UnityEngine;

public class SpiderClimbController : MonoBehaviour
{
    private enum SpiderState
    {
        Ground,     // 평지 걷기
        ClimbWall,  // 벽 타고 수직 이동 (forward = 위)
        ClimbUp,    // 꼭대기 턱 넘기기 (위 + 안쪽 대각선 이동)
        DropDown    // 바닥을 바라보며 낙하
    }

    private SpiderState state = SpiderState.Ground;

    [Header("Path Settings")]
    public bool usePath = false;   // A↔B 경로 사용 여부
    public Transform pointA;       // 경로 시작 지점
    public Transform pointB;       // 경로 끝 지점

    // A↔B 왕복용 내부 상태
    private bool movingToB = true;   // true: A→B, false: B→A
    private Vector3 pathDir;         // A→B의 수평 방향
    private float pathLength;        // A↔B 거리 (수평)

    [Header("Speed")]
    public float moveSpeed = 2f;       // 평지 이동 속도
    public float wallClimbSpeed = 2f;  // 벽 타고 올라가는 속도
    public float climbUpSpeed = 2f;    // 턱 넘을 때 속도 (위+안쪽)
    public float dropSpeed = 3f;       // 떨어지는 속도

    [Header("Layers")]
    public LayerMask groundLayer;      // 평지 레이어
    public LayerMask climbLayer;       // 벽(장애물) 레이어

    [Header("Ray Settings")]
    public float groundCheckDistance = 2f;   // 아래로 레이 길이
    public float forwardCheckDistance = 0.5f;  // 앞 레이 길이
    public float forwardRayHeight = 0.3f;      // 앞 레이 쏘는 높이
    public float stickOffset = 0.05f;          // 표면에서 살짝 띄우는 거리

    [Header("Drop Settings")]
    public float landingCheckDistance = 0.3f;  // 착지 감지용 짧은 거리

    private Vector3 wallNormal;   // 현재 붙어있는 벽의 노멀
    private Vector3 climbDir;     // 벽면 따라 올라갈 방향 (위쪽)

    // 아래로 쏠 때는 ground + climb 둘 다 "걸을 수 있는 표면" 취급
    LayerMask WalkableMask => groundLayer | climbLayer;

    void Start()
    {
        Debug.Log("Spider State → " + state);

        if (usePath && pointA != null && pointB != null)
        {
            // A→B 수평 방향 및 거리 계산 (회전·위치 변경은 안 함!)
            Vector3 ab = pointB.position - pointA.position;
            ab.y = 0f;
            pathLength = ab.magnitude;
            if (pathLength > 0.001f)
            {
                pathDir = ab.normalized;
            }
        }
    }

    void Update()
    {
        Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, Color.yellow);
        Debug.DrawRay(transform.position + Vector3.up * forwardRayHeight, transform.forward * forwardCheckDistance, Color.red);

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
        {
            AlignForFall();
        }

        state = newState;
        Debug.Log("Spider State → " + state);
    }

    // 떨어질 때 바닥을 바라보게 회전
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

    // 평지용 자세로 복구 (forward 수평화) — 원래 버전 그대로 유지
    void AlignForGround()
    {
        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.forward;   // 모델이 Z+가 앞

        flatForward.Normalize();
        transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
    }

    // ─────────────────────── 1) 평지 상태 ───────────────────────
    void UpdateGround()
    {
        // 1) 먼저 발밑 바닥부터 확실히 잡고
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, groundCheckDistance, WalkableMask))
        {
            transform.position = groundHit.point + Vector3.up * stickOffset;
        }
        else
        {
            SetState(SpiderState.DropDown);
            return;
        }

        // 2) 그 다음 Z+ 방향(앞)으로 이동 (무지성 직진)
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        // 2-1) A↔B 경로를 사용하는 경우: 왕복 처리
        if (usePath && pointA != null && pointB != null && pathLength > 0.001f)
        {
            // 현재 이동 기준 시작점 / 방향 결정
            Vector3 start = movingToB ? pointA.position : pointB.position;
            Vector3 dir   = movingToB ? pathDir          : -pathDir;

            // start에서 현재 위치까지, 경로 방향으로 얼마나 갔는지
            Vector3 fromStart = transform.position - start;
            float traveled = Vector3.Dot(fromStart, dir);  // 스칼라 거리

            if (traveled >= pathLength)
            {
                // 끝점에 도달 → 그 지점으로 스냅 (y는 현재 유지, 다음 프레임에 ground 스냅으로 맞춰짐)
                Vector3 end = movingToB ? pointB.position : pointA.position;
                transform.position = new Vector3(end.x, transform.position.y, end.z);

                // 방향 반전
                movingToB = !movingToB;

                // 새 방향을 바라보게 회전
                Vector3 newDir = movingToB ? pathDir : -pathDir;
                if (newDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(newDir, Vector3.up);
                }
            }
        }

        // 3) 앞에 벽(climbLayer) 있는지 체크
        Vector3 forwardOrigin = transform.position + Vector3.up * forwardRayHeight;

        if (Physics.Raycast(forwardOrigin, transform.forward, out RaycastHit wallHit, forwardCheckDistance, climbLayer))
        {
            float angle = Vector3.Angle(wallHit.normal, Vector3.up);

            // 수직면(거의 90도)이면 벽으로 판정
            if (angle > 80f)
            {
                // 벽 정보 저장
                wallNormal = wallHit.normal;

                // 벽을 따라 "위로" 올라갈 방향 계산
                climbDir = Vector3.ProjectOnPlane(Vector3.up, wallNormal).normalized;
                if (climbDir.sqrMagnitude < 0.0001f)
                    climbDir = Vector3.up;

                if (Vector3.Dot(climbDir, Vector3.up) < 0)
                    climbDir = -climbDir;

                // forward(Z+) = climbDir(위쪽), up = wallNormal(벽 바깥)
                transform.rotation = Quaternion.LookRotation(climbDir, wallNormal);

                // 벽 표면에 딱 붙이기
                transform.position = wallHit.point + wallNormal * stickOffset;

                SetState(SpiderState.ClimbWall);
            }
        }
    }

    // ─────────────────────── 2) 벽 타기 ───────────────────────
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

    // ─────────────────────── 3) 꼭대기 턱 넘기 ───────────────────────
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

    // ─────────────────────── 4) 떨어지는 상태 ───────────────────────
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
}
