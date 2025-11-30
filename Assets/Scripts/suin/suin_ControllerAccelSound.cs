using UnityEngine;

/// <summary>
/// 컨트롤러/헤드의 jerk(가속도 변화율) + 속도 밴드 조건을 동시에 만족할 때 랜덤 부스럭 재생.
/// - jerk 상/하한(히스테리시스) + 속도 밴드(하/상한, 히스테리시스)
/// - 위치 데드존, EMA 평활, 타겟별 쿨다운
/// - 볼륨은 jerk 초과량과 속도 밴드 내 위치로 스케일
/// </summary>
[DisallowMultipleComponent]
public class suin_ControllerAccelSound : MonoBehaviour
{
    [Header("Dependencies")]
    public ControllerMotionTracker tracker; // sourceSet = LeftRightHead 권장

    [Header("Sound")]
    public string soundKey = "random:moving-cloth";
    [Range(0f, 2f)] public float baseVolume = 1f;
    public float cooldown = 0.18f;
    public float maxVolumeScale = 1.6f;
    [Tooltip("볼륨 스케일 가중치( jerk / speed )")]
    [Range(0f, 2f)] public float jerkVolumeWeight = 0.7f;
    [Range(0f, 2f)] public float speedVolumeWeight = 0.5f;

    [Header("Jerk Gate (m/s^3)")]
    public float jerkUpper = 20f;                 // 상한
    [Range(0f, 1f)] public float jerkHysRatio = 0.6f; // 하한 = 상한 * ratio

    [Header("Speed Band Gate (m/s)")]
    [Tooltip("move slight flag threshold")]
    public float slight_flag_threshold = 0.005f;
    [Tooltip("속도 밴드 하한(이상)")]
    public float speedMin = 0.08f;
    [Tooltip("속도 밴드 상한(이하). 0 or 음수면 상한 무시")]
    public float speedMax = 0.40f;
    [Range(0f, 1f)] public float speedHysRatio = 0.85f; // 밴드 히스테리시스(하한/상한 각각에 적용)

    [Header("Noise Guards")]
    [Tooltip("프레임간 이동 데드존 (mm). 이하는 0으로 간주")]
    public float posDeadzoneMM = 0.6f;
    public float warmupAfterResolve = 0.06f;

    [Header("Smoothing (EMA)")]
    [Range(0f, 1f)] public float alphaVel = 0.35f;
    [Range(0f, 1f)] public float alphaAcc = 0.35f;
    
    // 🔹 speed로 볼륨을 얼마나 "미세"하게 흔들지 (예: 0.1 이면 ±10% 이내)
    [Range(0f, 0.5f)]
    public float maxSpeedVolumeDelta = 0.1f;

    
    [Header("Debug")]
    public bool showDebug = false;

    // 0=L, 1=R, 2=H
    private Transform[] _t = new Transform[3];

    // 상태: 위치/속도/가속도
    private Vector3[] _prevPos       = new Vector3[3];
    private Vector3[] _velSmooth     = new Vector3[3];
    private Vector3[] _prevVelSmooth = new Vector3[3];
    private Vector3[] _accSmooth     = new Vector3[3];
    private Vector3[] _prevAccSmooth = new Vector3[3];
    private Vector3[] _prevVelRaw    = new Vector3[3];

    private bool[] _hasPrev = new bool[3];
    private float[] _lastPlay = new float[3] { -999f, -999f, -999f };
    private float _lastResolveTime = -999f;

    // 히스테리시스 상태
    private bool[] _jerkLoud  = new bool[3];
    private bool[] _speedIn   = new bool[3];

    void Start()
    {
        if (!tracker) tracker = FindObjectOfType<ControllerMotionTracker>();
        if (tracker)
        {
            tracker.OnTargetsResolved += HandleResolved;
            HandleResolved(tracker.CurrentLeft, tracker.CurrentRight, tracker.CurrentHead);
        }
    }

    void OnDestroy()
    {
        if (tracker) tracker.OnTargetsResolved -= HandleResolved;
    }

    private void HandleResolved(Transform l, Transform r, Transform h)
    {
        _t[0] = l; _t[1] = r; _t[2] = h;

        for (int i = 0; i < 3; i++)
        {
            _hasPrev[i] = false;
            _velSmooth[i] = Vector3.zero;
            _prevVelSmooth[i] = Vector3.zero;
            _accSmooth[i] = Vector3.zero;
            _prevAccSmooth[i] = Vector3.zero;
            _prevVelRaw[i] = Vector3.zero;
            _jerkLoud[i] = false;
            _speedIn[i] = false;
        }
        _lastResolveTime = Time.time;

        if (showDebug)
            Debug.Log($"[AccelSound] Resolved L:{(l?l.name:"null")} R:{(r?r.name:"null")} H:{(h?h.name:"null")}");
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt < 1e-5f) return;
        if (Time.time - _lastResolveTime < warmupAfterResolve) return;

        for (int i = 0; i < 3; i++)
        {
            var t = _t[i];
            if (!t) continue;

            Vector3 pos = t.position;

            if (!_hasPrev[i])
            {
                _prevPos[i] = pos;
                _hasPrev[i] = true;
                continue;
            }

            // --- 위치 변화 + 데드존 ---
            Vector3 dPos = pos - _prevPos[i];
            float dPosMM = dPos.magnitude * 1000f;
            if (dPosMM < posDeadzoneMM) dPos = Vector3.zero;

            // --- 속도(원시/스무딩) ---
            Vector3 velRaw = dPos / dt;
            Vector3 velSmooth = Vector3.Lerp(_velSmooth[i], velRaw, alphaVel);

            // --- 가속도(스무딩) ---
            // 스무딩된 속도의 차분으로 가속도 구함
            Vector3 accRaw = (velSmooth - _prevVelSmooth[i]) / dt;
            Vector3 accSmooth = Vector3.Lerp(_accSmooth[i], accRaw, alphaAcc);

            // --- jerk (m/s^3): 스무딩된 가속도의 차분 ---
            float jerkMag = ((accSmooth - _prevAccSmooth[i]) / dt).magnitude;

            // --- 속도 크기 ---
            float speed = velSmooth.magnitude;

            if (speed > slight_flag_threshold) // TODO : finetune
            {
                suin_FlagHub.instance.SetMoveSlightFlag(true);
            }

            // ===== JERK 히스테리시스 =====
            float jerkLower = jerkUpper * Mathf.Clamp01(jerkHysRatio);
            if (jerkMag >= jerkUpper) _jerkLoud[i] = true;
            else if (jerkMag <= jerkLower) _jerkLoud[i] = false;

            // ===== SPEED 밴드 히스테리시스 =====
            // 하한/상한에 각각 히스테리시스 적용
            float sMinOn  = speedMin;                         // 들어올 때 하한
            float sMinOff = speedMin * Mathf.Clamp01(speedHysRatio); // 나갈 때 하한(더 낮아야 꺼짐)
            float sMaxOn  = speedMax > 0f ? speedMax : float.PositiveInfinity;
            float sMaxOff = speedMax > 0f ? (speedMax / Mathf.Max(1e-6f, speedHysRatio)) : float.PositiveInfinity;

            bool inBandNow = (speed >= sMinOn) && (speed <= sMaxOn);
            bool outBandNow = (speed <= sMinOff) || (speed >= sMaxOff);

            if (inBandNow) _speedIn[i] = true;
            else if (outBandNow) _speedIn[i] = false;
            // (밴드 경계 사이에서는 상태 유지)

            // ===== 트리거 조건: jerk ON && speed IN =====
            if (_jerkLoud[i] && _speedIn[i] && (Time.time - _lastPlay[i] >= cooldown))
            {
                float vol = ComputeVolume(jerkMag, jerkUpper, speed, speedMin, speedMax);
                float pitchScale = ComputePitchScale(jerkMag, jerkUpper, speed, speedMin, speedMax);

                if (suin_SoundManager.instance.PlayAtSourceWithPitch(soundKey, t, vol, pitchScale, -1f))
                {
                    suin_FlagHub.instance.SetPlayerSoundFlag(true);
                    _lastPlay[i] = Time.time;
                    if (showDebug)
                        Debug.Log($"[AccelSound] {t.name} jerk={jerkMag:F1} speed={speed:F2} vol={vol:F2} pitchScale={pitchScale:F2}");
                    break;
                }
            }


            // --- 상태 갱신 ---
            _prevPos[i] = pos;
            _prevVelSmooth[i] = velSmooth;
            _prevAccSmooth[i] = accSmooth;
            _velSmooth[i] = velSmooth;
            _accSmooth[i] = accSmooth;
            _prevVelRaw[i] = velRaw;
        }
    }

    /// 볼륨 = base * jerkFactor * speedFactor
    /// - jerkFactor : jerk 초과량에 비례 (메인 드라이버)
    /// - speedFactor: 속도 밴드 내 위치에 따른 ±미세 변화
    private float ComputeVolume(float jerk, float jerkUp, float speed, float sMin, float sMax)
    {
        // --- jerk 쪽 (예전과 비슷하게 유지) ---
        float jerkNorm = jerk / Mathf.Max(1e-6f, jerkUp);    // 1 이상이면 threshold 초과
        float excessJerk = Mathf.Max(0f, jerkNorm - 1f);     // 0 이상

        float jerkFactor = 1f + jerkVolumeWeight * excessJerk;

        // --- speed 쪽 (아주 미세하게) ---
        float speedNorm = 0f;
        if (sMax > 0f && sMax > sMin)
            speedNorm = Mathf.Clamp01((speed - sMin) / (sMax - sMin));
        else
            speedNorm = Mathf.Clamp01(speed / Mathf.Max(1e-6f, sMin * 2f));

        // 0~1 → -0.5~+0.5 로 가운데를 기준으로 이동
        float centered = speedNorm - 0.5f;   // -0.5 ~ +0.5

        // speedVolumeWeight * maxSpeedVolumeDelta 만큼만 영향 주기
        // 예) maxSpeedVolumeDelta=0.1, speedVolumeWeight=0.5 → 최대 ±0.05 (±5%) 볼륨 변화
        float speedOffset = centered * 2f * maxSpeedVolumeDelta * speedVolumeWeight;
        float speedFactor = 1f + speedOffset;    // 대략 0.95 ~ 1.05 정도

        // --- 통합 ---
        float scale = jerkFactor * speedFactor;
        scale = Mathf.Clamp(scale, 0.1f, maxVolumeScale);

        return baseVolume * scale;
    }
    
    /// <summary>
    /// jerk/speed 기반 pitch scale 계산
    /// - jerk가 클수록 약간 더 높은 pitch
    /// - speed가 밴드 상단쪽일수록 살짝 더 높게
    /// </summary>
    private float ComputePitchScale(float jerk, float jerkUp, float speed, float sMin, float sMax)
    {
        // 0~1 정규화
        float jerkNorm = Mathf.Clamp01(jerk / Mathf.Max(1e-6f, jerkUp)); 

        float speedNorm = 0f;
        if (sMax > 0f && sMax > sMin)
            speedNorm = Mathf.Clamp01((speed - sMin) / (sMax - sMin));
        else
            speedNorm = Mathf.Clamp01(speed / Mathf.Max(1e-6f, sMin * 2f));

        // 기본 1.0에서 ±0.2 정도만 흔들기 (너무 크면 짜증나게 들림)
        float pitch = 1.0f
                      + 0.20f * jerkNorm      // jerk 세면 더 날카롭게
                      + 0.10f * speedNorm;    // 빠르게 움직이면 약간 더 높게

        // 안전 클램프
        return Mathf.Clamp(pitch, 0.85f, 1.35f);
    }

}
