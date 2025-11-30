using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using SoftKitty.LiquidContainer;

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
    
    [Header("Motion Randomness / Jerk Gate")]
    [Tooltip("모션 사운드를 변속(jerk)이 있을 때만 내고 싶으면 체크")]
    public bool gateMotionByJerk = true;

    [Tooltip("정규화된 jerk 점수가 이 값 이상일 때만 모션 사운드 허용")]
    [Range(0f, 2f)]
    public float jerkGateThreshold = 0.35f;

    [Tooltip("모션 사운드 볼륨에 적용할 랜덤 범위 (±비율)")]
    [Range(0f, 0.5f)]
    public float motionVolumeRandomRange = 0.1f;

    
    [Header("Hysteresis & Smoothing")]
    [Tooltip("상한을 넘으면 발화, 하한 미만이면 다시 무음 상태로(히스테리시스)")]
    public float hysteresisRatio = 0.6f; // 하한 = 상한 * ratio
    [Range(0f, 1f)]
    [Tooltip("지수평활 알파(0은 강한평활, 1은 평활없음)")]
    public float emaAlpha = 0.35f;

    [Header("Volume Scaling (Motion)")]
    public bool scaleVolumeByExcess = true;
    [Range(0f, 1f)] public float volumeSensitivity = 0.15f;
    public float maxMotionVolumeScale = 1.5f;

    [Header("Liquid-based Modulation")]
    public LiquidControl liquid;
    [Tooltip("이 이하로 떨어지면 '비었다'고 간주 (모든 liquid 사운드 off)")]
    [Range(0f, 0.2f)] public float emptyWaterLineThreshold = 0.02f;

    [Tooltip("가득 찼을 때 motion pitch scale")]
    [Range(0.5f, 2f)] public float motionPitchFull = 1.0f;
    [Tooltip("거의 비었을 때 motion pitch scale (더 가벼운/높은 소리)")]
    [Range(0.5f, 2f)] public float motionPitchEmpty = 1.3f;

    [Tooltip("거의 비었을 때 볼륨 최소 배율 (가벼운 소리)")]
    [Range(0f, 1f)] public float motionMinFillVolumeMul = 0.3f;

    [Tooltip("모션 사운드용 추가 pitch jitter (랜덤)")]
    [Range(0f, 0.2f)] public float motionExtraPitchJitter = 0.03f;

    [Header("Impact Splash Sound")]
    public bool enableImpactSound = true;
    public string impactEntryName = "impact";

    [Tooltip("이 속도 이상 충돌 시 splash 트리거")]
    public float impactVelocityThreshold = 1.2f;
    [Range(0f, 2f)] public float impactBaseVolume = 1.0f;

    [Tooltip("가득 찼을 때 impact pitch scale")]
    [Range(0.5f, 2f)] public float impactPitchFull = 1.0f;
    [Tooltip("거의 비었을 때 impact pitch scale")]
    [Range(0.5f, 2f)] public float impactPitchEmpty = 1.4f;

    [Range(0f, 0.2f)] public float impactPitchJitter = 0.03f;

    [Header("Pouring Sound")]
    public bool enablePourSound = true;
    public string pourEntryName = "pour";
    [Tooltip("pour 사운드 최소 재생 간격(초)")]
    [Min(0f)] public float pourMinInterval = 0.15f;
    [Tooltip("flowSize가 이 값 이상일 때부터 pour 사운드")]
    [Range(0f, 1f)] public float pourMinFlowForSound = 0.05f;
    [Range(0f, 2f)] public float pourMaxVolume = 1.0f;
    [Range(0.5f, 2f)] public float pourPitchFull = 1.0f;
    [Range(0.5f, 2f)] public float pourPitchEmpty = 1.3f;
    [Range(0f, 0.2f)] public float pourPitchJitter = 0.02f;
    
    
    
    [Header("Debug")]
    public bool showDebug = false;

    private XRGrabInteractable _grab;
    private Vector3 _prevPos;
    private Quaternion _prevRot;
    private bool _hasPrev;
    private float _lastPlayTime = -999f;
    private float _lastSelectTime = -999f;
    private float _lastPourPlayTime = -999f;

    // EMA 상태
    private float _emaLinMMps = 0f;
    private float _emaAngDegps = 0f;
    private bool _inLoudState = false; // 히스테리시스 상태
    
    private float _prevEmaLinMMps = 0f;
    private float _prevEmaAngDegps = 0f;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        if (!reactive) reactive = GetComponent<suin_ReactiveSound>();
        if (!liquid) liquid = GetComponentInChildren<LiquidControl>();
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
        UpdateMotionSound();
        UpdatePouringSound();
    }

    private void UpdateMotionSound()
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
            _prevEmaLinMMps = 0f; _prevEmaAngDegps = 0f;
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

        // --- jerk(변속) 점수 계산 ---
        // EMA 속도 변화량(증가분) 기반
        float linDelta = Mathf.Max(0f, _emaLinMMps - _prevEmaLinMMps);
        float angDelta = Mathf.Max(0f, _emaAngDegps - _prevEmaAngDegps);

        // speed threshold로 정규화해서 0~대략 1 근처 숫자로 만듦
        float linNorm = linDelta / Mathf.Max(1e-4f, linearSpeedThreshMMps);
        float angNorm = angDelta / Mathf.Max(1e-4f, angularSpeedThreshDegps);
        float jerkScore = linNorm + angNorm;        // gate용 원래 점수
        float jerkNorm  = Mathf.Clamp01(jerkScore); // intensity용 0~1 정규화

        // --- speedNorm 계산 (현재 속도 크기 기반) ---
        float speedNorm;
        if (thresholdMode == ThresholdMode.DisplacementPerFrame)
        {
            float posNorm = Mathf.Clamp01(dPosMM / Mathf.Max(1e-4f, linearThresholdMM));
            float rotNorm = Mathf.Clamp01(dRotDeg / Mathf.Max(1e-4f, angularThresholdDeg));
            speedNorm = Mathf.Clamp01(0.5f * posNorm + 0.5f * rotNorm);
        }
        else // VelocityPerSecond
        {
            float posNorm = Mathf.Clamp01(_emaLinMMps / Mathf.Max(1e-4f, linearSpeedThreshMMps));
            float rotNorm = Mathf.Clamp01(_emaAngDegps / Mathf.Max(1e-4f, angularSpeedThreshDegps));
            speedNorm = Mathf.Clamp01(0.5f * posNorm + 0.5f * rotNorm);
        }

        // speed + jerk를 섞어서 "전체 움직임 강도" (0~1) 산출
        float motionIntensity = Mathf.Clamp01(0.6f * speedNorm + 0.4f * jerkNorm);

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
            // 🔹 jerk gate: 변속이 충분할 때만 사운드
            if (gateMotionByJerk && jerkScore < jerkGateThreshold)
            {
                _prevPos = pos;
                _prevRot = rot;
                _prevEmaLinMMps = _emaLinMMps;
                _prevEmaAngDegps = _emaAngDegps;
                return;
            }

            // --- 액체 상태 검사: 비었으면 무음 ---
            float fillNorm = 1f;
            bool hasLiquid = true;
            if (liquid != null)
            {
                fillNorm = Mathf.Clamp01(liquid.WaterLine);
                hasLiquid = fillNorm > emptyWaterLineThreshold;
            }

            if (!hasLiquid)
            {
                _lastPlayTime = Time.time;
                _prevPos = pos;
                _prevRot = rot;
                _prevEmaLinMMps = _emaLinMMps;
                _prevEmaAngDegps = _emaAngDegps;
                return;
            }

            float volScale = 1f;

            // --- 기존 speed 초과량 기반 volume ---
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

            // --- 채워진 정도에 따른 볼륨/피치 보정 ---
            float fillVolumeMul = Mathf.Lerp(motionMinFillVolumeMul, 1f, fillNorm);
            volScale *= fillVolumeMul;

            // ➕ 움직임 강도에 따른 추가 볼륨 스케일
            //   motionIntensity=0 → 0.8, 1 → 1.3 배 정도
            volScale *= Mathf.Lerp(0.8f, 1.3f, motionIntensity);

            // 🔹 볼륨에 ±랜덤 살짝 섞기 (예: motionVolumeRandomRange=0.1 → ±10%)
            if (motionVolumeRandomRange > 0f)
            {
                float randMul = 1f + Random.Range(-motionVolumeRandomRange, motionVolumeRandomRange);
                volScale *= randMul;
            }

            // --- pitch: 물 양 + 움직임 강도 동시에 반영 ---
            float basePitch = Mathf.Lerp(motionPitchEmpty, motionPitchFull, fillNorm);

            // 움직임 강도에 따른 pitch 추가 변화 (±0.2 정도)
            float pitchIntensityRange = 0.2f;
            float intensityBias = motionIntensity - 0.5f; // -0.5 ~ +0.5
            float pitchScale = basePitch + pitchIntensityRange * intensityBias;
            pitchScale = Mathf.Clamp(pitchScale, 0.1f, 3f);

            if (reactive.TryPlayByNameWithPitch(motionEntryName, volScale, pitchScale, motionExtraPitchJitter))
            {
                suin_FlagHub.instance.SetWaterSoundFlag(true);
                _lastPlayTime = Time.time;
                if (showDebug)
                {
                    string speedInfo =
                        (thresholdMode == ThresholdMode.DisplacementPerFrame)
                        ? $"ΔPos={dPosMM:F1}mm, ΔRot={dRotDeg:F1}°"
                        : $"v={_emaLinMMps:F0}mm/s, ω={_emaAngDegps:F0}°/s";

                    Debug.Log(
                        $"[Flask] Motion sound ({speedInfo}) " +
                        $"fill={fillNorm:F2}, speedNorm={speedNorm:F2}, jerkNorm={jerkNorm:F2}, " +
                        $"vol={volScale:F2}, pitch={pitchScale:F2}"
                    );
                }
            }
        }

        _prevPos = pos;
        _prevRot = rot;
        _prevEmaLinMMps = _emaLinMMps;
        _prevEmaAngDegps = _emaAngDegps;
    }

    private void UpdatePouringSound()
    {
        if (!enablePourSound || reactive == null || string.IsNullOrEmpty(pourEntryName))
            return;
        if (liquid == null) return;

        float fillNorm = Mathf.Clamp01(liquid.WaterLine);
        bool hasLiquid = fillNorm > emptyWaterLineThreshold;
        if (!hasLiquid) return;

        bool isPouring = liquid.IsPouring && liquid.FlowSize >= pourMinFlowForSound;
        if (!isPouring) return;

        if (Time.time - _lastPourPlayTime < pourMinInterval)
            return;

        float flowNorm = Mathf.Clamp01(liquid.FlowSize);
        float volScale = pourMaxVolume * flowNorm * fillNorm;
        float pitchScale = Mathf.Lerp(pourPitchEmpty, pourPitchFull, fillNorm);

        if (reactive.TryPlayByNameWithPitch(pourEntryName, volScale, pitchScale, pourPitchJitter))
        {
            suin_FlagHub.instance.SetWaterSoundFlag(true);
            _lastPourPlayTime = Time.time;

            if (showDebug)
            {
                Debug.Log($"[Flask] Pour sound flow={flowNorm:F2} fill={fillNorm:F2} vol={volScale:F2} pitch={pitchScale:F2}");
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enableImpactSound || reactive == null || string.IsNullOrEmpty(impactEntryName))
            return;

        float relSpeed = collision.relativeVelocity.magnitude;
        if (relSpeed < impactVelocityThreshold) return;

        float fillNorm = 1f;
        bool hasLiquid = true;
        if (liquid != null)
        {
            fillNorm = Mathf.Clamp01(liquid.WaterLine);
            hasLiquid = fillNorm > emptyWaterLineThreshold;
        }

        if (!hasLiquid) return; // 비었으면 splash 안 냄

        float speedFactor = Mathf.Clamp01(relSpeed / (impactVelocityThreshold * 2f));
        float volScale = impactBaseVolume * speedFactor * fillNorm;
        float pitchScale = Mathf.Lerp(impactPitchEmpty, impactPitchFull, fillNorm);

        if (reactive.TryPlayByNameWithPitch(impactEntryName, volScale, pitchScale, impactPitchJitter))
        {
            suin_FlagHub.instance.SetWaterSoundFlag(true);
            if (showDebug)
            {
                Debug.Log($"[Flask] Impact splash v={relSpeed:F2}, fill={fillNorm:F2}, vol={volScale:F2}, pitch={pitchScale:F2}");
            }
        }
    }
}
