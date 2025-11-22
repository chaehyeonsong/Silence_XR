using System;
using UnityEngine;

public class suin_FlagHub : MonoBehaviour
{
    public static suin_FlagHub instance;

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

    public void SetMoveSlightFlag(bool v)   => OnMoveSlightFlag?.Invoke(v);
    public void SetPlayerSoundFlag(bool v) => OnPlayerSoundFlag?.Invoke(v);
    public void SetWaterSoundFlag(bool v)  => OnWaterSoundFlag?.Invoke(v);

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