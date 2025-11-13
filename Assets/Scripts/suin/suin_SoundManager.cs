using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class suin_SoundManager : MonoBehaviour
{
    public static suin_SoundManager instance { get; private set; }

    [System.Serializable] public class NamedClip { public string key; public AudioClip clip; }

    [Header("Clips")]
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

    private Dictionary<string, AudioClip> _map;
    private Dictionary<string, float> _lastPlay;
    private Dictionary<string, int> _playingCount;

    // 🔹 보이스 구조체 및 풀
    private class Voice
    {
        public AudioSource src;
        public float freeAt; // 언제 다시 쓸 수 있는지
    }
    private List<Voice> _voices;

    // 🔹 플래그 정의 (API 변경 없이 minCooldown에 약속)
    private const float FLAG_IGNORE_IF_PLAYING = -1f; // 기본값: 재생 중이면 무시
    private const float FLAG_ALLOW_OVERLAP = -2f;     // 재생 중이어도 겹쳐 재생

    void Awake()
    {
        if (instance && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        _map = new Dictionary<string, AudioClip>();
        foreach (var nc in clips)
            if (nc != null && nc.clip && !string.IsNullOrEmpty(nc.key))
                _map[nc.key] = nc.clip;

        _lastPlay = new Dictionary<string, float>();
        _playingCount = new Dictionary<string, int>();

        // 🔹 보이스 풀 초기화
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

    // --- 공용 API (변경 없음) ---
    public bool Play(string key, float volScale = 1f, float minCooldown = -1f)
        => PlayInternal(key, null, Vector3.zero, volScale, minCooldown, Mode.Global);

    public bool PlayAtPosition(string key, Vector3 pos, float volScale = 1f, float minCooldown = -1f)
        => PlayInternal(key, null, pos, volScale, minCooldown, Mode.Position);

    public bool PlayAtSource(string key, Transform source, float volScale = 1f, float minCooldown = -1f)
        => source ? PlayInternal(key, source, Vector3.zero, volScale, minCooldown, Mode.Source) : false;

    public bool PlayAtObject(string key, GameObject go, float volScale = 1f, float minCooldown = -1f)
        => go ? PlayAtSource(key, go.transform, volScale, minCooldown) : false;

    public bool PlayAtObjectName(string key, string objectName, float volScale = 1f, float minCooldown = -1f)
    {
        var go = GameObject.Find(objectName);
        return go ? PlayAtSource(key, go.transform, volScale, minCooldown) : false;
    }

    // --- 내부 ---
    private enum Mode { Global, Position, Source }

    private bool PlayInternal(string key, Transform srcTransform, Vector3 pos, float volScale, float minCooldown, Mode mode)
    {
        if (!_map.TryGetValue(key, out var clip) || !clip) return false;

        float now = Time.unscaledTime;

        // 🔸 (1) 재생 제어 정책
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

        _lastPlay[key] = now;

        float v = Mathf.Clamp01(volume * volScale + Random.Range(-volumeJitter, volumeJitter));
        float p = Mathf.Clamp(pitch + Random.Range(-pitchJitter, pitchJitter), 0.1f, 3f);
        float dur = Mathf.Max(0.01f, clip.length / Mathf.Abs(p));

        // 🔹 재생 카운트 갱신
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

            StartCoroutine(FinishAfter(key, dur, temp));
            return true;
        }
        else
        {
            var voice = AcquireVoice(now, dur, stealOldestIfNone: true);
            if (voice == null) { _playingCount[key]--; return false; }

            var src = voice.src;
            src.outputAudioMixerGroup = outputMixerGroup;
            src.spatialBlend = spatialBlend;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.rolloffMode = rolloff;
            src.pitch = p;
            src.volume = v;

            if (mode == Mode.Position) src.transform.position = pos;

            src.clip = clip;
            src.Play();

            voice.freeAt = now + dur;

            StartCoroutine(FinishAfter(key, dur, null));
            return true;
        }
    }

    // 🔸 (3) 보이스 할당 로직
    private Voice AcquireVoice(float now, float dur, bool stealOldestIfNone)
    {
        Voice best = null;
        float earliest = float.MaxValue;

        foreach (var v in _voices)
        {
            if (now >= v.freeAt) return v; // 바로 사용 가능
            if (v.freeAt < earliest)
            {
                earliest = v.freeAt;
                best = v;
            }
        }
        // 모두 재생 중이면 가장 오래된 보이스 스틸
        return stealOldestIfNone ? best : null;
    }

    // 🔸 (4) 종료 처리
    private IEnumerator FinishAfter(string key, float delay, AudioSource tempToDestroy)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (_playingCount.TryGetValue(key, out int cnt))
        {
            cnt = Mathf.Max(0, cnt - 1);
            if (cnt == 0) _playingCount.Remove(key);
            else _playingCount[key] = cnt;
        }

        if (tempToDestroy != null) Destroy(tempToDestroy);
    }
}
