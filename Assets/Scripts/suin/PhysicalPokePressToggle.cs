using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class PhysicalPokePressToggle : MonoBehaviour
{
    [Header("Targets")]
    public Renderer lampRenderer;
    public Light targetLight;

    [Header("Emission Settings")]
    public string emissionProperty = "_EmissionColor";
    public Color onEmissionColor = Color.white;
    public Color offEmissionColor = Color.black;
    public bool controlEmissionKeyword = true;

    [Header("Initial State")]
    public bool startOn = true;

    [Header("Interaction Settings")]
    [Tooltip("연속 입력 방지 시간 (초)")]
    public float toggleCooldown = 0.5f;
    public string playerTag = "Player"; // 손이나 플레이어 태그

    // 내부 변수
    XRSimpleInteractable _ix;
    MaterialPropertyBlock _mpb;
    int _emissionID;
    bool _isOn;
    float _lastToggleTime;

    void Awake()
    {
        _ix = GetComponent<XRSimpleInteractable>();
        _emissionID = Shader.PropertyToID(emissionProperty);
        _mpb = new MaterialPropertyBlock();

        if (GetComponent<Collider>() == null)
            Debug.LogError($"❌ [PokeToggle] {name}에 Collider가 없습니다!");
    }

    private void OnEnable()
    {
        if (_ix != null)
        {
            _ix.selectEntered.AddListener(OnInteract);
            _ix.hoverEntered.AddListener(OnInteract);
        }

        if (suin_FlagHub.instance != null)
        {
            // 2. 내 설정(startOn)을 허브에 강제로 주입합니다. (초기화)
            // 이렇게 하면 게임 시작 시점의 좀비 상태와 내 전등 상태가 100% 일치합니다.
            _isOn = startOn;
            suin_FlagHub.instance.ForceLightState(_isOn);
        }
        else
        {
            _isOn = startOn;
            Debug.LogWarning("⚠️ [PokeToggle] FlagHub가 없습니다! 단독으로 작동합니다.");
        }

        // 3. 비주얼 적용
        ApplyVisuals();
    }

    private void OnDisable()
    {
        if (_ix != null)
        {
            _ix.selectEntered.RemoveListener(OnInteract);
            _ix.hoverEntered.RemoveListener(OnInteract);
        }
    }

    void Start()
    {
        // 1. 시작하자마자 허브를 찾습니다.
        if (suin_FlagHub.instance != null)
        {
            // 2. 내 설정(startOn)을 허브에 강제로 주입합니다. (초기화)
            // 이렇게 하면 게임 시작 시점의 좀비 상태와 내 전등 상태가 100% 일치합니다.
            _isOn = startOn;
            suin_FlagHub.instance.ForceLightState(_isOn);
        }
        else
        {
            _isOn = startOn;
            Debug.LogWarning("⚠️ [PokeToggle] FlagHub가 없습니다! 단독으로 작동합니다.");
        }

        // 3. 비주얼 적용
        ApplyVisuals();
    }

    // XR 인터랙션
    void OnInteract(BaseInteractionEventArgs args) => TryToggle();

    // 물리 충돌
    void OnTriggerEnter(Collider other)
    {
        if (string.IsNullOrEmpty(playerTag) || other.CompareTag(playerTag))
            TryToggle();
    }

    void TryToggle()
    {
        // 쿨타임 체크 (더블 클릭 방지)
        if (Time.time - _lastToggleTime < toggleCooldown) return;
        _lastToggleTime = Time.time;

        if (suin_FlagHub.instance != null)
        {
            // [핵심 로직 변경]
            // 내 변수(_isOn)를 뒤집지 말고, '허브의 현재 상태'를 가져와서 반대로 뒤집습니다.
            bool currentHubState = suin_FlagHub.instance.LightOn;
            bool newState = !currentHubState;

            // 1. 허브에게 강제로 새 상태를 알립니다. (좀비 호출)
            suin_FlagHub.instance.ForceLightState(newState);

            // 2. 내 상태를 업데이트합니다.
            _isOn = newState;
            
            Debug.Log($"👇 [PokeToggle] 스위치 누름! (허브 상태: {currentHubState} → {newState})");
        }
        else
        {
            // 허브가 없으면 그냥 내꺼 반전
            _isOn = !_isOn;
        }

        // 3. 눈에 보이는 전등 상태 변경
        ApplyVisuals();
    }

    void ApplyVisuals()
    {
        if (targetLight) targetLight.enabled = _isOn;

        if (lampRenderer)
        {
            lampRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_emissionID, _isOn ? onEmissionColor : offEmissionColor);
            lampRenderer.SetPropertyBlock(_mpb);

            if (controlEmissionKeyword)
            {
                var shared = lampRenderer.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    var mat = shared[i];
                    if (!mat) continue;
                    if (_isOn) mat.EnableKeyword("_EMISSION");
                    else       mat.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}