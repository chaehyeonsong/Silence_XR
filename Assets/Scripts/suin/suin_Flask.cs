using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public class suin_Flask : MonoBehaviour
{
    public bool grabbed { get; private set; } = false;

    [Header("Sound Routing (ReactiveSound 사용)")]
    public suin_ReactiveSound reactive;

    [Header("Grab Sound")]
    public bool playGrabSound = true;
    public string grabEntryName = "grab";
    [Range(0f, 2f)] public float grabVolumeScale = 1f;

    [Header("Motion Sound")]
    public bool enableMotionSound = true;
    public string motionEntryName = "motion";

    [Tooltip("잡혀 있을 때만 모션 사운드 허용")]
    public bool requireGrabbed = true;

    [Tooltip("모션 사운드 재트리거 최소 간격(초)")]
    [Min(0f)] public float motionCooldown = 0.12f;

    [Tooltip("잡은 직후 모션 사운드 무시 시간(초)")]
    [Min(0f)] public float warmupAfterSelect = 0.06f;
    
    public enum ThresholdMode { DisplacementPerFrame, VelocityPerSecond }
    public ThresholdMode thresholdMode = ThresholdMode.VelocityPerSecond;

    [Tooltip("프레임 간 선형 이동 임계 (mm)")]
    public float linearThresholdMM = 1.0f;
    [Tooltip("프레임 간 회전 임계 (deg)")]
    public float angularThresholdDeg = 0.8f;

    [Tooltip("선형 속도 임계 (mm/s)")]
    public float linearSpeedThreshMMps = 80f;
    [Tooltip("각속도 임계 (deg/s)")]
    public float angularSpeedThreshDegps = 30f;

    [Header("Hysteresis & Smoothing")]
    [Tooltip("상한을 넘으면 발화, 하한 미만이면 다시 무음 상태로(히스테리시스)")]
    public float hysteresisRatio = 0.6f; // 하한 = 상한 * ratio
    [Range(0f, 1f)]
    [Tooltip("지수평활 알파(0은 강한평활, 1은 평활없음)")]
    public float emaAlpha = 0.35f;

    [Header("Volume Scaling")]
    public bool scaleVolumeByExcess = true;
    [Range(0f, 1f)] public float volumeSensitivity = 0.15f;
    public float maxMotionVolumeScale = 1.5f;

    [Header("Debug")]
    public bool showDebug = false;

    private XRGrabInteractable _grab;
    private Vector3 _prevPos;
    private Quaternion _prevRot;
    private bool _hasPrev;
    private float _lastPlayTime = -999f;
    private float _lastSelectTime = -999f;

    // EMA 상태
    private float _emaLinMMps = 0f;
    private float _emaAngDegps = 0f;
    private bool _inLoudState = false; // 히스테리시스 상태

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        if (!reactive) reactive = GetComponent<suin_ReactiveSound>();
    }

    void OnEnable()
    {
        _grab.selectEntered.AddListener(OnSelectEntered);
        _grab.selectExited.AddListener(OnSelectExited);
        _hasPrev = false;
        _inLoudState = false;
    }

    void OnDisable()
    {
        _grab.selectEntered.RemoveListener(OnSelectEntered);
        _grab.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        grabbed = true;
        _hasPrev = false; // 기준 리셋
        _lastSelectTime = Time.time;
        _inLoudState = false;

        if (playGrabSound && reactive && !string.IsNullOrEmpty(grabEntryName))
        {
            reactive.TryPlayByName(grabEntryName, grabVolumeScale);
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        grabbed = false;
        _hasPrev = false;
        _inLoudState = false;
    }

    void Update()
    {
        if (!enableMotionSound || !reactive || string.IsNullOrEmpty(motionEntryName))
            return;
        if (requireGrabbed && !grabbed) return;

        // 잡은 직후 웜업 시간 동안 무시
        if (grabbed && Time.time - _lastSelectTime < warmupAfterSelect) return;

        var t = transform;
        Vector3 pos = t.position;
        Quaternion rot = t.rotation;

        if (!_hasPrev)
        {
            _prevPos = pos; _prevRot = rot; _hasPrev = true;
            _emaLinMMps = 0f; _emaAngDegps = 0f;
            return;
        }

        float dt = Mathf.Max(Time.deltaTime, 1e-5f);
        float dPosMM = Vector3.Distance(_prevPos, pos) * 1000f; // m → mm
        float dRotDeg = Quaternion.Angle(_prevRot, rot);
        float linMMps = dPosMM / dt;
        float angDegps = dRotDeg / dt;

        // 평활
        _emaLinMMps = Mathf.Lerp(_emaLinMMps, linMMps, emaAlpha);
        _emaAngDegps = Mathf.Lerp(_emaAngDegps, angDegps, emaAlpha);

        bool overUpper, belowLower;
        if (thresholdMode == ThresholdMode.DisplacementPerFrame)
        {
            float upperLin = linearThresholdMM;
            float upperAng = angularThresholdDeg;
            float lowerLin = upperLin * hysteresisRatio;
            float lowerAng = upperAng * hysteresisRatio;

            overUpper = (dPosMM >= upperLin) || (dRotDeg >= upperAng);
            belowLower = (dPosMM <= lowerLin) && (dRotDeg <= lowerAng);
        }
        else // VelocityPerSecond
        {
            float upperLin = linearSpeedThreshMMps;
            float upperAng = angularSpeedThreshDegps;
            float lowerLin = upperLin * hysteresisRatio;
            float lowerAng = upperAng * hysteresisRatio;

            overUpper = (_emaLinMMps >= upperLin) || (_emaAngDegps >= upperAng);
            belowLower = (_emaLinMMps <= lowerLin) && (_emaAngDegps <= lowerAng);
        }

        // 히스테리시스 상태 갱신
        if (overUpper) _inLoudState = true;
        else if (belowLower) _inLoudState = false;

        // 사운드 트리거(쿨다운 적용, loud 상태에서만 1회성 트리거)
        if (_inLoudState && Time.time - _lastPlayTime >= motionCooldown)
        {
            float volScale = 1f;

            if (scaleVolumeByExcess)
            {
                float score;
                if (thresholdMode == ThresholdMode.DisplacementPerFrame)
                {
                    float posEx = Mathf.Max(0f, dPosMM - linearThresholdMM) / Mathf.Max(1e-4f, linearThresholdMM);
                    float rotEx = Mathf.Max(0f, dRotDeg - angularThresholdDeg) / Mathf.Max(1e-4f, angularThresholdDeg);
                    score = posEx + rotEx;
                }
                else
                {
                    float posEx = Mathf.Max(0f, _emaLinMMps - linearSpeedThreshMMps) / Mathf.Max(1e-4f, linearSpeedThreshMMps);
                    float rotEx = Mathf.Max(0f, _emaAngDegps - angularSpeedThreshDegps) / Mathf.Max(1e-4f, angularSpeedThreshDegps);
                    score = posEx + rotEx;
                }

                volScale = Mathf.Clamp(1f + score * volumeSensitivity, 0.1f, maxMotionVolumeScale);
            }

            if (reactive.TryPlayByName(motionEntryName, volScale))
            {
                suin_FlagHub.instance.SetWaterSoundFlag(true);
                _lastPlayTime = Time.time;
                if (showDebug)
                {
                    Debug.Log(
                        thresholdMode == ThresholdMode.DisplacementPerFrame
                        ? $"[Flask] Motion sound (ΔPos={dPosMM:F1}mm, ΔRot={dRotDeg:F1}°) vol={volScale:F2}"
                        : $"[Flask] Motion sound (v={_emaLinMMps:F0}mm/s, ω={_emaAngDegps:F0}°/s) vol={volScale:F2}"
                    );
                }
            }
        }

        _prevPos = pos;
        _prevRot = rot;
    }
}
