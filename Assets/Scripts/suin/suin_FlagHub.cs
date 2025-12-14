using System;
using System.Collections;
using UnityEngine;

public class suin_FlagHub : MonoBehaviour
{
    public static suin_FlagHub instance;

    [Header("Pulse Flag Duration")]
    [Tooltip("Move/PlayerSound/Water 플래그가 유지되는 시간 (초)")]
    public float pulseDuration = 1.5f;

    [Header("Calm Timeout")]
    [Tooltip("이 시간 동안 어떤 플래그도 true가 되지 않으면 Calm 상태로 판정")]
    public float calmTimeout = 15f;

    // 최근 alert 시점
    private float _lastAlertTime;

    public bool IsCalm
    {
        get { return Time.time - _lastAlertTime >= calmTimeout; }
    }

    // ===== 펄스형 이벤트들 =====
    public event Action<bool> OnMoveSlightFlag;
    public event Action<bool> OnPlayerSoundFlag;
    public event Action<bool> OnWaterSoundFlag;

    private Coroutine moveSlightCo;
    private Coroutine playerSoundCo;
    private Coroutine waterSoundCo;

    // ===== Light 상태 이벤트 =====
    public event Action<bool> OnLightStateChanged; 
    
    [SerializeField]
    private bool _lightOn;
    public bool LightOn => _lightOn;

    // ===== Player Kill Flag =====
    public event Action OnPlayerKillFlag;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        _lastAlertTime = Time.time;
    }

    void MarkAlertFired()
    {
        _lastAlertTime = Time.time;
    }

    // ===============================
    // Light State (핵심 수정 부분)
    // ===============================

    /// <summary>
    /// 일반적인 상태 변경. 값이 변했을 때만 알림을 보냄.
    /// </summary>
    public void SetLightState(bool isOn)
    {
        if (_lightOn == isOn) return;
        _lightOn = isOn;
        OnLightStateChanged?.Invoke(_lightOn);
    }

    /// <summary>
    /// [추가됨] 초기화용 강제 설정 함수.
    /// 값이 같아도 강제로 이벤트를 발생시켜 좀비에게 현재 상태를 알림.
    /// </summary>
    public void ForceLightState(bool isOn)
    {
        _lightOn = isOn;
        // 강제 호출
        OnLightStateChanged?.Invoke(_lightOn);
    }

    // ===============================
    // 기타 플래그 로직들 (기존 동일)
    // ===============================
    public void SetMoveSlightFlag(bool v)
    {
        OnMoveSlightFlag?.Invoke(v);
        if (v)
        {
            MarkAlertFired();
            if (moveSlightCo != null) StopCoroutine(moveSlightCo);
            moveSlightCo = StartCoroutine(ResetMoveSlightFlagAfterDelay());
        }
        else
        {
            if (moveSlightCo != null) { StopCoroutine(moveSlightCo); moveSlightCo = null; }
        }
    }

    IEnumerator ResetMoveSlightFlagAfterDelay()
    {
        yield return new WaitForSeconds(pulseDuration);
        OnMoveSlightFlag?.Invoke(false);
        moveSlightCo = null;
    }

    public void SetPlayerSoundFlag(bool v)
    {
        OnPlayerSoundFlag?.Invoke(v);
        if (v)
        {
            MarkAlertFired();
            if (playerSoundCo != null) StopCoroutine(playerSoundCo);
            playerSoundCo = StartCoroutine(ResetPlayerSoundFlagAfterDelay());
        }
        else
        {
            if (playerSoundCo != null) { StopCoroutine(playerSoundCo); playerSoundCo = null; }
        }
    }

    IEnumerator ResetPlayerSoundFlagAfterDelay()
    {
        yield return new WaitForSeconds(pulseDuration);
        OnPlayerSoundFlag?.Invoke(false);
        playerSoundCo = null;
    }

    public void SetWaterSoundFlag(bool v)
    {
        OnWaterSoundFlag?.Invoke(v);
        if (v)
        {
            MarkAlertFired();
            if (waterSoundCo != null) StopCoroutine(waterSoundCo);
            waterSoundCo = StartCoroutine(ResetWaterSoundFlagAfterDelay());
        }
        else
        {
            if (waterSoundCo != null) { StopCoroutine(waterSoundCo); waterSoundCo = null; }
        }
    }

    IEnumerator ResetWaterSoundFlagAfterDelay()
    {
        yield return new WaitForSeconds(pulseDuration);
        OnWaterSoundFlag?.Invoke(false);
        waterSoundCo = null;
    }

    public void TriggerPlayerKillFlag()
    {
        Debug.Log("🔥 [FlagHub] PlayerKillFlag TRIGGERED");
        OnPlayerKillFlag?.Invoke();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
}