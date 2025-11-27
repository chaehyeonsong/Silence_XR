using System;
using System.Collections;
using UnityEngine;

public class suin_FlagHub : MonoBehaviour
{
    public static suin_FlagHub instance;

    [Header("Pulse Flag Duration")]
    [Tooltip("Move/PlayerSound/Water 플래그가 유지되는 시간 (초)")]
    public float pulseDuration = 1.5f;

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

    // ===== 펄스형 이벤트들 (호출될 때마다 notify) =====
    public event Action<bool> OnMoveSlightFlag;
    public event Action<bool> OnPlayerSoundFlag;
    public event Action<bool> OnWaterSoundFlag;

    // 각 플래그당 타이머 코루틴
    private Coroutine moveSlightCo;
    private Coroutine playerSoundCo;
    private Coroutine waterSoundCo;

    public void SetMoveSlightFlag(bool v)
    {
        OnMoveSlightFlag?.Invoke(v);

        if (v)
        {
            // 기존 타이머 있으면 리셋
            if (moveSlightCo != null) StopCoroutine(moveSlightCo);
            moveSlightCo = StartCoroutine(ResetMoveSlightFlagAfterDelay());
        }
        else
        {
            // 직접 false를 쏜 경우 타이머 정리
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

    public void SetPlayerSoundFlag(bool v)
    {
        OnPlayerSoundFlag?.Invoke(v);

        if (v)
        {
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

    public void SetWaterSoundFlag(bool v)
    {
        OnWaterSoundFlag?.Invoke(v);

        if (v)
        {
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

    // ===== Light는 "상태 저장 + 변화 알림" =====
    public event Action<bool> OnLightStateChanged; // true=On, false=Off

    bool _lightOn;                 // 현재 라이트 상태 저장
    public bool LightOn => _lightOn;  // 밖에서 읽기 가능

    /// <summary>
    /// Light 상태를 저장하고, "변했을 때만" notify
    /// </summary>
    public void SetLightState(bool isOn)
    {
        if (_lightOn == isOn) return;   // 상태 변화 없으면 알림 X
        _lightOn = isOn;
        OnLightStateChanged?.Invoke(_lightOn);
    }
}
