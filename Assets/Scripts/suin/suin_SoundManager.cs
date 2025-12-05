using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class suin_SoundManager : MonoBehaviour
{
    public static suin_SoundManager instance { get; private set; }

    // 🔹 key 하나에 여러 AudioClip을 달 수 있게 변경
    [System.Serializable]
    public class NamedClip
    {
        public string key;
        public List<AudioClip> clips = new List<AudioClip>();
    }

    [Header("Clips (key별로 여러 Clip 지원)")]
    public List<NamedClip> clips = new List<NamedClip>();

    [Header("Audio Settings")]
    public AudioMixerGroup outputMixerGroup; // optional
    public int poolSize = 8;
    public float defaultCooldown = 0.1f;
    [Range(0, 1)] public float spatialBlend = 1.0f;  // 1=3D
    public float minDistance = 0.3f;
    public float maxDistance = 12f;
    public AudioRolloffMode rolloff = AudioRolloffMode.Linear;

    [Header("Pitch/Volume Variance")]
    public float volume = 1.0f;
    public float volumeJitter = 0.0f;
    public float pitch = 1.0f;
    public float pitchJitter = 0.0f;

    // key -> clips[]
    private Dictionary<string, List<AudioClip>> _map;
    // key -> last played time
    private Dictionary<string, float> _lastPlay;
    // key -> how many currently playing
    private Dictionary<string, int> _playingCount;

    // 🔸 random:prefix 그룹 독점 재생 지원 (기존 기능 유지)
    // prefix -> how many currently playing in the group
    private Dictionary<string, int> _playingGroupCount;
    // prefix -> last play time (원하면 그룹 쿨다운에 활용 가능)
    private Dictionary<string, float> _lastPlayGroup;

    private class Voice
    {
        public AudioSource src;
        public float freeAt;
    }
    private List<Voice> _voices;

    public const float FLAG_IGNORE_IF_PLAYING = -1f; // 기본값: 재생 중이면 무시
    public const float FLAG_ALLOW_OVERLAP     = -2f; // 재생 중이어도 겹쳐 재생

    void Awake()
    {
        if (instance && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        BuildMapFromClips();

        _lastPlay = new Dictionary<string, float>();
        _playingCount = new Dictionary<string, int>();

        _playingGroupCount = new Dictionary<string, int>();
        _lastPlayGroup = new Dictionary<string, float>();

        _voices = new List<Voice>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.outputAudioMixerGroup = outputMixerGroup;
            src.spatialBlend = spatialBlend;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.rolloffMode = rolloff;
            _voices.Add(new Voice { src = src, freeAt = 0f });
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        BuildMapFromClips();
    }
#endif

    // 🔹 Inspector의 clips 리스트 → 내부 Dictionary<string, List<AudioClip>>
    private void BuildMapFromClips()
    {
        if (_map == null) _map = new Dictionary<string, List<AudioClip>>();
        _map.Clear();

        foreach (var nc in clips)
        {
            if (nc == null) continue;
            if (string.IsNullOrEmpty(nc.key)) continue;
            if (nc.clips == null || nc.clips.Count == 0) continue;

            // null 아닌 것만 필터링
            var valid = nc.clips.Where(c => c != null).ToList();
            if (valid.Count == 0) continue;

            _map[nc.key] = valid;
        }
    }

    // --- 공용 API (외부에서 쓰는 것은 그대로) ---
    public bool Play(string key, float volScale = 1f, float minCooldown = -1f)
        => PlayInternal(key, null, Vector3.zero, volScale, minCooldown, Mode.Global, 1f, 0f);

    public bool PlayAtPosition(string key, Vector3 pos, float volScale = 1f, float minCooldown = -1f)
        => PlayInternal(key, null, pos, volScale, minCooldown, Mode.Position, 1f, 0f);

    public bool PlayAtSource(string key, Transform source, float volScale = 1f, float minCooldown = -1f)
        => source ? PlayInternal(key, source, Vector3.zero, volScale, minCooldown, Mode.Source, 1f, 0f) : false;

    public bool PlayAtSourceWithPitch(
        string key,
        Transform source,
        float volScale,
        float pitchScale,
        float minCooldown = -1f,
        float extraPitchJitter = 0.05f
    )
    {
        if (!source) return false;
        return PlayInternal(key, source, Vector3.zero, volScale, minCooldown, Mode.Source, pitchScale, extraPitchJitter);
    }

    public bool PlayAtObject(string key, GameObject go, float volScale = 1f, float minCooldown = -1f)
        => go ? PlayAtSource(key, go.transform, volScale, minCooldown) : false;

    public bool PlayAtObjectName(string key, string objectName, float volScale = 1f, float minCooldown = -1f)
    {
        var go = GameObject.Find(objectName);
        return go ? PlayAtSource(key, go.transform, volScale, minCooldown) : false;
    }

    // --- 내부 ---
    private enum Mode { Global, Position, Source }

    private bool PlayInternal(
        string key,
        Transform srcTransform,
        Vector3 pos,
        float volScale,
        float minCooldown,
        Mode mode,
        float pitchScale = 1f,
        float extraPitchJitter = 0f
    )
    {
        string groupPrefix = null;

        // ✅ random:prefix → prefix 그룹에서 "key"를 랜덤 선택 (기존 기능 유지)
        if (!string.IsNullOrEmpty(key) && key.StartsWith("random:"))
        {
            groupPrefix = key.Substring("random:".Length);

            // 현재 그룹이 재생 중이면 전체 차단 (독점 그룹)
            if (!string.IsNullOrEmpty(groupPrefix) &&
                _playingGroupCount.TryGetValue(groupPrefix, out int gcnt) && gcnt > 0)
            {
                return false;
            }

            // 후보 수집 (prefix로 시작하는 key들)
            if (_map == null || _map.Count == 0) return false;
            var candidates = _map.Keys.Where(k => k.StartsWith(groupPrefix)).ToList();
            if (candidates.Count == 0) return false;

            key = candidates[Random.Range(0, candidates.Count)];
        }

        // 🔹 여기서 key 하나에 대해 여러 clip 중 하나를 랜덤 선택
        if (_map == null || !_map.TryGetValue(key, out var clipList) || clipList == null || clipList.Count == 0)
            return false;

        var clip = clipList[Random.Range(0, clipList.Count)];
        if (!clip) return false;

        float now = Time.unscaledTime;

        // 🔸 (1) 재생 제어 정책 (키 단위)
        if (minCooldown == FLAG_ALLOW_OVERLAP)
        {
            // 겹쳐 재생 허용
        }
        else if (minCooldown == FLAG_IGNORE_IF_PLAYING)
        {
            if (_playingCount.TryGetValue(key, out int cnt) && cnt > 0) return false;
            if (_lastPlay.TryGetValue(key, out float t1) && (now - t1) < defaultCooldown) return false;
        }
        else if (minCooldown >= 0f)
        {
            if (_lastPlay.TryGetValue(key, out float t2) && (now - t2) < minCooldown) return false;
        }
        else
        {
            if (_playingCount.TryGetValue(key, out int cnt2) && cnt2 > 0) return false;
            if (_lastPlay.TryGetValue(key, out float t3) && (now - t3) < defaultCooldown) return false;
        }

        // 🔹 그룹 독점 재생 진입 (random prefix일 때만)
        if (!string.IsNullOrEmpty(groupPrefix))
        {
            if (!_playingGroupCount.ContainsKey(groupPrefix)) _playingGroupCount[groupPrefix] = 0;
            _playingGroupCount[groupPrefix]++; // 잠금
            _lastPlayGroup[groupPrefix] = now;
        }

        _lastPlay[key] = now;

        float v = Mathf.Clamp01(volume * volScale + Random.Range(-volumeJitter, volumeJitter));

        float basePitch = pitch * pitchScale;
        float totalJitter = pitchJitter + extraPitchJitter;
        float p = Mathf.Clamp(
            basePitch + Random.Range(-totalJitter, totalJitter),
            0.1f,
            3f
        );
        
        // 💡 "random-notify" 전용 랜덤 pitch / volume
        if (key == "random-notify")
        {
            // volume: 0.6 ~ 1.0 배 사이 랜덤 (너무 작지 않게)
            v = Mathf.Clamp01(v * Random.Range(1.0f, 2.0f));

            // pitch: 0.8 ~ 1.2 사이 랜덤
            p = Random.Range(0.8f, 1.2f);
            
        }


        float dur = Mathf.Max(0.01f, clip.length / Mathf.Abs(p));

        if (!_playingCount.ContainsKey(key)) _playingCount[key] = 0;
        _playingCount[key]++;

        // --- (2) 소스 선택 ---
        if (mode == Mode.Source && srcTransform != null)
        {
            var temp = srcTransform.gameObject.AddComponent<AudioSource>();
            temp.clip = clip;
            temp.outputAudioMixerGroup = outputMixerGroup;
            temp.spatialBlend = spatialBlend;
            temp.minDistance = minDistance;
            temp.maxDistance = maxDistance;
            temp.rolloffMode = rolloff;
            temp.volume = v;
            temp.pitch = p;
            temp.Play();

            StartCoroutine(FinishAfter(key, groupPrefix, dur, temp));
            return true;
        }
        else
        {
            var voice = AcquireVoice(now, dur, stealOldestIfNone: true);
            if (voice == null)
            {
                _playingCount[key]--;
                if (!string.IsNullOrEmpty(groupPrefix))
                {
                    // 보이스 부족으로 실패 → 그룹 잠금 해제
                    _playingGroupCount[groupPrefix] = Mathf.Max(0, _playingGroupCount[groupPrefix] - 1);
                    if (_playingGroupCount[groupPrefix] == 0) _playingGroupCount.Remove(groupPrefix);
                }
                return false;
            }

            var src = voice.src;
            src.outputAudioMixerGroup = outputMixerGroup;
            src.spatialBlend = spatialBlend;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.rolloffMode = rolloff;
            src.pitch = p;
            src.volume = v;

            if (mode == Mode.Position) src.transform.position = pos;
            if (mode == Mode.Source && srcTransform != null) src.transform.position = srcTransform.position;

            src.clip = clip;
            src.Play();

            voice.freeAt = now + dur;

            StartCoroutine(FinishAfter(key, groupPrefix, dur, null));
            return true;
        }
    }

    private Voice AcquireVoice(float now, float dur, bool stealOldestIfNone)
    {
        Voice best = null;
        float earliest = float.MaxValue;

        foreach (var v in _voices)
        {
            if (now >= v.freeAt) return v;
            if (v.freeAt < earliest)
            {
                earliest = v.freeAt;
                best = v;
            }
        }
        return stealOldestIfNone ? best : null;
    }

    private IEnumerator FinishAfter(string key, string groupPrefix, float delay, AudioSource tempToDestroy)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (_playingCount.TryGetValue(key, out int cnt))
        {
            cnt = Mathf.Max(0, cnt - 1);
            if (cnt == 0) _playingCount.Remove(key);
            else _playingCount[key] = cnt;
        }

        if (!string.IsNullOrEmpty(groupPrefix) &&
            _playingGroupCount.TryGetValue(groupPrefix, out int gcnt))
        {
            gcnt = Mathf.Max(0, gcnt - 1);
            if (gcnt == 0) _playingGroupCount.Remove(groupPrefix);
            else _playingGroupCount[groupPrefix] = gcnt;
        }

        if (tempToDestroy != null) Destroy(tempToDestroy);
    }
}
