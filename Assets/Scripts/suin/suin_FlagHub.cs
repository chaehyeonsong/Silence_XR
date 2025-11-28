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

    // 각 플래그 이벤트
    public event Action<bool> OnMoveSlightFlag;
    public event Action<bool> OnPlayerSoundFlag;
    public event Action<bool> OnWaterSoundFlag;

    // 내부 타이머
    private Coroutine moveSlightCo;
    private Coroutine playerSoundCo;
    private Coroutine waterSoundCo;

    // 최근 알림 시간이 저장되는 변수
    private float _lastAlertTime;

    // 라이트 이벤트
    public event Action<bool> OnLightStateChanged;
    private bool _lightOn;
    public bool LightOn => _lightOn;

    // 🔥 외부에서 몬스터가 확인하는 Calm 상태 프로퍼티
    public bool IsCalm
    {
        get { return Time.time - _lastAlertTime >= calmTimeout; }
    }

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
        // 시작 시점 기록
        _lastAlertTime = Time.time;
    }

    // ❗ alert 타이머 초기화 함수
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
    // Light Flag
    // ===============================
    public void SetLightState(bool isOn)
    {
        if (_lightOn == isOn) return;

        _lightOn = isOn;
        OnLightStateChanged?.Invoke(_lightOn);
    }
}
