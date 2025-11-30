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

    /// <summary>
    /// 마지막 alert 이후 calmTimeout 이상 지나면 true
    /// (지금은 좀비/거미가 안 쓰고 있어도 놔두면 됨)
    /// </summary>
    public bool IsCalm
    {
        get { return Time.time - _lastAlertTime >= calmTimeout; }
    }

    // ===== 펄스형 이벤트들 (호출될 때마다 true → pulseDuration 뒤 false) =====
    public event Action<bool> OnMoveSlightFlag;
    public event Action<bool> OnPlayerSoundFlag;
    public event Action<bool> OnWaterSoundFlag;

    private Coroutine moveSlightCo;
    private Coroutine playerSoundCo;
    private Coroutine waterSoundCo;

    // ===== Light 상태 이벤트 (On/Off 상태를 저장하고 변화만 알림) =====
    public event Action<bool> OnLightStateChanged; // true=On, false=Off

    private bool _lightOn;
    public bool LightOn => _lightOn;

    // ===== Player Kill Flag (대상은 모름, 신호만 보냄) =====
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
        // 시작 시점 기준으로 calm 타이머 초기화
        _lastAlertTime = Time.time;
    }

    /// <summary>
    /// alert형 플래그가 true로 들어왔을 때 타이머 리셋
    /// </summary>
    void MarkAlertFired()
    {
        _lastAlertTime = Time.time;
    }

    // ===============================
    // Move Slight Flag
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
            if (moveSlightCo != null)
            {
                StopCoroutine(moveSlightCo);
                moveSlightCo = null;
            }
        }
    }

    IEnumerator ResetMoveSlightFlagAfterDelay()
    {
        yield return new WaitForSeconds(pulseDuration);
        OnMoveSlightFlag?.Invoke(false);
        moveSlightCo = null;
    }

    // ===============================
    // Player Sound Flag
    // ===============================
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
            if (playerSoundCo != null)
            {
                StopCoroutine(playerSoundCo);
                playerSoundCo = null;
            }
        }
    }

    IEnumerator ResetPlayerSoundFlagAfterDelay()
    {
        yield return new WaitForSeconds(pulseDuration);
        OnPlayerSoundFlag?.Invoke(false);
        playerSoundCo = null;
    }

    // ===============================
    // Water Sound Flag
    // ===============================
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
            if (waterSoundCo != null)
            {
                StopCoroutine(waterSoundCo);
                waterSoundCo = null;
            }
        }
    }

    IEnumerator ResetWaterSoundFlagAfterDelay()
    {
        yield return new WaitForSeconds(pulseDuration);
        OnWaterSoundFlag?.Invoke(false);
        waterSoundCo = null;
    }

    // ===============================
    // Light State
    // ===============================
    /// <summary>
    /// Light 상태를 저장하고, "변했을 때만" notify
    /// </summary>
    public void SetLightState(bool isOn)
    {
        if (_lightOn == isOn) return;   // 상태 변화 없으면 알림 X
        _lightOn = isOn;
        OnLightStateChanged?.Invoke(_lightOn);
    }

    // ===============================
    // Player Kill Flag
    // ===============================
    /// <summary>
    /// 누군가 죽어야 하는 상황이라고 알리는 플래그.
    /// 대상은 여기서 고르지 않고, OnPlayerKillFlag 구독자에서 처리.
    /// </summary>
    public void TriggerPlayerKillFlag()
    {
        Debug.Log("🔥 [FlagHub] PlayerKillFlag TRIGGERED (죽음 플래그 발생)");
        OnPlayerKillFlag?.Invoke();
    }
}
