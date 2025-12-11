using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
public class UniversalBreakable : MonoBehaviour
{
    [Header("Break Condition")]
    [Tooltip("이 속도 이상으로 충돌하면 깨짐")]
    public float breakVelocityThreshold = 3.0f;

    [Tooltip("손에 들고 있을 땐 안 깨지게 할지 여부 (XRGrabInteractable 자동 감지)")]
    public bool onlyBreakWhenReleased = true;

    [Header("Mesh Setup")]
    [Tooltip("깨지기 전 통짜 메쉬 오브젝트 (Flask 루트나 Flask_Intact 같은 것)")]
    public GameObject intactVisual;

    [Tooltip("파편들이 들어있는 부모 트랜스폼 (비워두면 이 오브젝트의 자식들을 shard로 사용)")]
    public Transform shardsRoot;

    [Header("Shard Behaviour")]
    [Tooltip("파편에 개별 Rigidbody 물리를 줄지 여부 (안 주면 제자리에서 멈춰있는 파편 느낌)")]
    public bool enableShardPhysics = false;

    [Tooltip("파편에 중력을 사용할지 여부 (enableShardPhysics가 true일 때만 의미 있음)")]
    public bool shardUseGravity = true;

    [Tooltip("파편에 살짝 줄 랜덤 이동 속도 크기 (0이면 거의 그대로, 0.5~1이면 약간 흔들리는 정도)")]
    public float shardRandomVelocity = 0.2f;

    [Tooltip("파편이 추가로 회전할 랜덤 각속도 크기")]
    public float shardRandomAngularVelocity = 1.0f;

    [Header("Impact Sound (non-breaking)")]
    [Tooltip("깨지지 않을 때 충돌 소리를 재생할 최소 속도")]
    public float impactSoundVelocityThreshold = 1.0f;

    public AudioClip impactClip;
    [Tooltip("Base volume for impact sound, multiplied by collision speed")]
    [Range(0f, 1f)] public float impactVolume = 1.0f;

    [Tooltip("연속 충돌 시 소리가 너무 자주 나지 않도록 막는 쿨다운 (초)")]
    public float impactSoundCooldown = 0.1f;

    private float _lastImpactSoundTime = -999f;

    
    [Header("Sound")]
    public AudioClip breakClip;
    [Range(0f, 1f)] public float breakVolume = 1.0f;

    [Header("Auto Cleanup")]
    [Tooltip("파편들을 일정 시간 후 삭제할지 여부 (선택)")]
    public bool autoDestroyShards = false;
    public float shardLifetime = 0f;

    [Header("GameController")]
    [Tooltip("Add GameController here")]
    public GameController gameController;

    // 내부 상태
    [SerializeField] private bool hasBroken = false;
    private bool isHeld = false;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;

    private List<GameObject> shardObjects = new List<GameObject>();
    private List<Rigidbody> shardBodies = new List<Rigidbody>();

    private Collider[] intactColliders;
    private Renderer[] intactRenderers;

    private const string ShardPrefix = "Shard";
    private const string LiquidMaskPrefix = "LiquidMask";

    private readonly List<GameObject> liquidMaskObjects = new List<GameObject>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        gameController.flaskBreakDelay = shardLifetime;

        // XRGrab 자동 감지
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        // 통짜 메쉬 오브젝트 비워두면 → 루트 오브젝트를 그대로 사용
        if (intactVisual == null)
        {
            intactVisual = this.gameObject;
        }

        // shardsRoot 비워두면 → 자기 transform을 사용 (직접 자식들이 shard)
        if (shardsRoot == null)
        {
            shardsRoot = this.transform;
        }

        // 통짜 메쉬의 렌더러/콜라이더 캐시
        intactColliders = intactVisual.GetComponents<Collider>();
        intactRenderers = intactVisual.GetComponents<Renderer>();

        // Shard들 / LiquidMask들 미리 모아두기
        SetupShards();
        CacheLiquidMasks();
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void SetupShards()
    {
        shardObjects.Clear();
        shardBodies.Clear();

        if (shardsRoot == null)
        {
            Debug.LogWarning("[UniversalBreakable] shardsRoot가 설정되지 않음.");
            return;
        }

        foreach (Transform child in shardsRoot)
        {
            var go = child.gameObject;

            // 통짜 메쉬와 같은 오브젝트면 제외
            if (go == intactVisual)
                continue;

            // 이름이 "Shard"로 시작하는 것만 파편으로 사용
            if (!go.name.StartsWith(ShardPrefix))
                continue;

            shardObjects.Add(go);

            // 파편에는 MeshFilter/MeshRenderer가 이미 들어있다고 가정
            // Rigidbody는 여기서 붙이거나, 미리 붙여놔도 됨
            Rigidbody srb = go.GetComponent<Rigidbody>();
            if (srb == null)
            {
                srb = go.AddComponent<Rigidbody>();
            }

            srb.isKinematic = true; // 깨지기 전까지는 부모랑 같이 움직이도록
            srb.useGravity = false;

            shardBodies.Add(srb);

            // 처음에는 파편 안 보이게
            go.SetActive(false);
        }
    }

    private void CacheLiquidMasks()
    {
        liquidMaskObjects.Clear();
        // 계층 전체에서 이름이 LiquidMask로 시작하는 오브젝트들을 찾음 (비활성 포함)
        var allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            if (t.gameObject.name.StartsWith(LiquidMaskPrefix))
            {
                liquidMaskObjects.Add(t.gameObject);
            }
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;
    }

    private void OnCollisionEnter(Collision collision) // This only plays bump sound 
    {
        if (hasBroken) return;

        if (onlyBreakWhenReleased && isHeld)
            return;

        float impactSpeed = collision.relativeVelocity.magnitude;

        // 먼저 깨질지 여부 결정
        bool willBreak = impactSpeed >= breakVelocityThreshold;

        // 깨지지 않는 충돌 + 일정 속도 이상이면 "통통" 충돌 사운드 재생
        if (!willBreak && impactClip != null && impactVolume > 0f)
        {
            if (impactSpeed >= impactSoundVelocityThreshold)
            {
                if (Time.time - _lastImpactSoundTime >= impactSoundCooldown)
                {
                    PlayImpactSound(collision, impactSpeed);
                    _lastImpactSoundTime = Time.time;
                }
            }
        }

        // 그 다음 깨짐 판정
        //if (willBreak)
        //{
        //   Break(collision);
        //}

        // GameController will decide when flasks break
    }
    
    private void PlayImpactSound(Collision collision, float speed)
    {
        if (impactClip == null || impactVolume <= 0f)
            return;

        Vector3 pos = transform.position;
        if (collision != null && collision.contactCount > 0)
            pos = collision.GetContact(0).point;

        AudioSource.PlayClipAtPoint(impactClip, pos, impactVolume * speed);
    }



    //public void ForceBreak()
    //{
    //    if (hasBroken) return;
    //    Break(null);
    //}

    public void Break() //GameController calls this function
    {
        if (hasBroken) return;
        hasBroken = true;

        // 1) 소리
        //PlayBreakSound(collision); // GameController plays break sound regardless of position

        // 2) 원본(통짜) 숨기기 (Renderer/Collider만 비활성화)
        HideIntactVisual();

        // 3) LiquidMask* 오브젝트 전부 끄기
        DisableLiquidMasks();

        // XR 상호작용 끊기
        if (grabInteractable != null)
        {
            grabInteractable.enabled = false;
        }

        // 4) 파편 활성화
        ActivateShards();

        // 필요하면 원래 Rigidbody 멈추기
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void HideIntactVisual()
    {
        if (intactRenderers != null)
        {
            foreach (var r in intactRenderers)
            {
                if (r != null) r.enabled = false;
            }
        }

        if (intactColliders != null)
        {
            foreach (var c in intactColliders)
            {
                if (c != null) c.enabled = false;
            }
        }
    }

    private void DisableLiquidMasks()
    {
        foreach (var go in liquidMaskObjects)
        {
            if (go != null)
                go.SetActive(false);
        }
    }

    private void PlayBreakSound(Collision collision)
    {
        if (breakClip == null || breakVolume <= 0f)
            return;

        Vector3 pos = transform.position;
        if (collision != null && collision.contactCount > 0)
            pos = collision.GetContact(0).point;

        AudioSource.PlayClipAtPoint(breakClip, pos, breakVolume);
    }

    private void ActivateShards()
    {
        for (int i = 0; i < shardObjects.Count; i++)
        {
            var shard = shardObjects[i];
            var srb = shardBodies[i];

            if (shard == null || srb == null)
                continue;

            shard.SetActive(true);

            if (enableShardPhysics)
            {
                srb.isKinematic = false;
                srb.useGravity = shardUseGravity;

                // 원래 플라스크의 속도 상속 + 약간의 랜덤 이동
                srb.velocity = rb.velocity
                               + Random.insideUnitSphere * shardRandomVelocity;

                // 각 shard마다 서로 다른 랜덤 방향으로 회전하도록
                srb.angularVelocity = rb.angularVelocity
                                      + Random.insideUnitSphere * shardRandomAngularVelocity;
            }
            else
            {
                // 그냥 그 자리에 멈춰있는 파편 느낌
                srb.isKinematic = true;
                srb.useGravity = false;
                srb.velocity = Vector3.zero;
                srb.angularVelocity = Vector3.zero;
            }

            if (autoDestroyShards)
            {
                Destroy(shard, shardLifetime);
            }
        }
    }
}
