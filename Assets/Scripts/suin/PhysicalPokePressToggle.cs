using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRSimpleInteractable))]
public class PhysicalPokePressToggle : MonoBehaviour
{
[Header("Targets")]
    [Tooltip("Emission을 제어할 램프 Mesh Renderer")]
    public Renderer lampRenderer;

    [Tooltip("토글할 씬 Light")]
    public Light targetLight;

    [Header("Emission Settings")]
    [Tooltip("머티리얼 Emission 컬러 프로퍼티명 (URP/HDRP/Lit 공통: _EmissionColor)")]
    public string emissionProperty = "_EmissionColor";
    [Tooltip("켜짐(ON) 때의 Emission 컬러 (밝기 포함)")]
    public Color onEmissionColor = Color.white;      // 필요하면 Color.white * 2.0f 처럼 세기 증가
    [Tooltip("꺼짐(OFF) 때의 Emission 컬러")]
    public Color offEmissionColor = Color.black;

    [Tooltip("Emission 키워드를 함께 관리 (켜짐 시 Enable, 꺼짐 시 Disable)")]
    public bool controlEmissionKeyword = true;

    [Header("Initial State")]
    [Tooltip("시작 시 켜짐 상태인지")]
    public bool startOn = true;

    // internal
    XRSimpleInteractable _ix;
    MaterialPropertyBlock _mpb;
    int _emissionID;
    bool _isOn;

    void Awake()
    {
        _ix = GetComponent<XRSimpleInteractable>();
        _emissionID = Shader.PropertyToID(emissionProperty);
        _mpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        _ix.selectEntered.AddListener(OnSelectEntered);
        // 초기 상태 적용
        _isOn = startOn;
        ApplyState();
    }

    void OnDisable()
    {
        _ix.selectEntered.RemoveListener(OnSelectEntered);
    }

    void OnSelectEntered(SelectEnterEventArgs _)
    {
        // 포크로 "눌렀을 때" 한 번씩 토글
        _isOn = !_isOn;
        ApplyState();
    }

    void ApplyState()
    {
        // 1) Light 토글
        if (targetLight) targetLight.enabled = _isOn;

        // 2) Emission 토글
        if (lampRenderer)
        {
            lampRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_emissionID, _isOn ? onEmissionColor : offEmissionColor);
            lampRenderer.SetPropertyBlock(_mpb);

            if (controlEmissionKeyword)
            {
                // 키워드는 sharedMaterials에 걸려있어야 확실히 적용/해제됨
                var shared = lampRenderer.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    var mat = shared[i];
                    if (!mat) continue;
                    if (_isOn) mat.EnableKeyword("_EMISSION");
                    else       mat.DisableKeyword("_EMISSION");
                }
            }

            // (옵션) 내장 파이프라인에서 GI 갱신이 필요하면 아래 사용
            // DynamicGI.SetEmissive(lampRenderer, _isOn ? onEmissionColor : offEmissionColor);
        }
    }
}