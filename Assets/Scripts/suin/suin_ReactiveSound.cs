using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class suin_ReactiveSound : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("이 엔트리를 식별할 이름 (예: 'grab', 'motion', 'drop' 등)")]
        public string name = "default";

        [Tooltip("SoundManager에 등록된 sound key")]
        public string key = "DefaultKey";

        [Tooltip("겹쳐 재생 허용 (true면 재생 중이어도 즉시 또 재생)")]
        public bool allowOverlap = false;

        [Tooltip("allowOverlap=false일 때만 의미. 같은 키 최소 간격(초)")]
        [Min(0f)] public float minCooldown = 0.2f;

        [Tooltip("최종 볼륨에 곱해지는 배율 (0~2)")]
        [Range(0f, 2f)] public float volumeMul = 1f;

        [Tooltip("재생 위치 기준(없으면 ReactiveSound의 transform)")]
        public Transform anchor;
    }

    [Header("Entries (상황별 사운드 정의)")]
    public List<Entry> entries = new List<Entry>()
    {
        new Entry { name="grab",   key="FlaskGrab",  allowOverlap=false, minCooldown=0.15f, volumeMul=1f },
        new Entry { name="motion", key="FlaskMove",  allowOverlap=false, minCooldown=0.08f, volumeMul=1f },
    };

    [Tooltip("공통 기본 앵커 (각 Entry의 anchor가 비어있을 때 사용)")]
    public Transform defaultAnchor;

    private suin_SoundManager SM => suin_SoundManager.instance;

    /// <summary>
    /// 엔트리 이름으로 재생. volumeScale은 동적 가중치(예: 속도 기반)로 곱해짐.
    /// </summary>
    public bool TryPlayByName(
        string entryName,
        float volumeScale = 1f,
        Transform overrideAnchor = null)
    {
        if (SM == null || string.IsNullOrEmpty(entryName)) return false;

        var e = FindEntry(entryName);
        if (e == null || string.IsNullOrEmpty(e.key)) return false;

        float vol = Mathf.Clamp01((e.volumeMul) * volumeScale);

        bool allow = e.allowOverlap;
        float flagOrCooldown = allow
            ? -2f                                // 겹쳐 재생
            : (e.minCooldown > 0f ? e.minCooldown : -1f); // 쿨다운 또는 재생중 무시

        var anchor = overrideAnchor ? overrideAnchor : (e.anchor ? e.anchor : (defaultAnchor ? defaultAnchor : transform));
        return SM.PlayAtSource(e.key, anchor, vol, flagOrCooldown);
    }

    // suin_ReactiveSound.cs 안, 클래스 내부에 추가
    public bool TryPlayByNameWithPitch(
        string entryName,
        float volumeScale,
        float pitchScale,
        float extraPitchJitter = 0.03f,
        Transform overrideAnchor = null
    )
    {
        if (SM == null || string.IsNullOrEmpty(entryName)) return false;

        var e = FindEntry(entryName);
        if (e == null || string.IsNullOrEmpty(e.key)) return false;

        float vol = Mathf.Clamp01(e.volumeMul * volumeScale);

        bool allow = e.allowOverlap;
        float flagOrCooldown = allow
            ? -2f                                // 겹쳐 재생
            : (e.minCooldown > 0f ? e.minCooldown : -1f); // 쿨다운 또는 재생중 무시

        var anchor = overrideAnchor
            ? overrideAnchor
            : (e.anchor ? e.anchor : (defaultAnchor ? defaultAnchor : transform));

        return suin_SoundManager.instance.PlayAtSourceWithPitch(
            e.key,
            anchor,
            vol,
            pitchScale,
            flagOrCooldown,
            extraPitchJitter
        );
    }


    /// <summary>
    /// key를 직접 지정해 재생(특정 엔트리와 무관하게). 필요 시 사용.
    /// </summary>
    public bool TryPlayKey(string key, float volumeScale = 1f, bool allowOverlap = false, float minCooldown = 0.2f, Transform anchor = null)
    {
        if (SM == null || string.IsNullOrEmpty(key)) return false;

        float vol = Mathf.Clamp01(volumeScale);
        float flagOrCooldown = allowOverlap ? -2f : (minCooldown > 0f ? minCooldown : -1f);
        var a = anchor ? anchor : (defaultAnchor ? defaultAnchor : transform);
        return SM.PlayAtSource(key, a, vol, flagOrCooldown);
    }

    public Entry FindEntry(string entryName)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (string.Equals(entries[i].name, entryName, System.StringComparison.OrdinalIgnoreCase))
                return entries[i];
        }
        return null;
    }

    // 런타임 편의 함수들
    public void SetEntryKey(string entryName, string newKey)
    {
        var e = FindEntry(entryName);
        if (e != null) e.key = newKey;
    }
    public void SetEntryOverlap(string entryName, bool enable)
    {
        var e = FindEntry(entryName);
        if (e != null) e.allowOverlap = enable;
    }
    public void SetEntryCooldown(string entryName, float seconds)
    {
        var e = FindEntry(entryName);
        if (e != null) e.minCooldown = Mathf.Max(0f, seconds);
    }
    public void SetEntryVolumeMul(string entryName, float mul)
    {
        var e = FindEntry(entryName);
        if (e != null) e.volumeMul = Mathf.Clamp(mul, 0f, 2f);
    }
}
