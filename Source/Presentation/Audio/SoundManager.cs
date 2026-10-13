using UnityEngine;

/// <summary>음악·효과음의 음량과 풀 대여를 관리한다. 음량은 실행 중 씬이 바뀌어도 유지한다.</summary>
public sealed class SoundManager : MonoBehaviour
{
    [SerializeField] private SoundEffectPlayer soundEffectPrefab;
    [SerializeField, Min(0)] private int initialCount = 8;
    [SerializeField, Min(1)] private int capacity = 16;
    [SerializeField, Range(0f, 1f)] private float effectsVolume = 1f;
    [SerializeField] private AudioSource musicSource;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    private PrefabPoolManager _pool;
    private static bool _hasSessionVolume;
    private static float _sessionMusicVolume;
    private static float _sessionEffectsVolume;
    public float MusicVolume => musicVolume;
    public float EffectsVolume => effectsVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionVolume() => _hasSessionVolume = false;

    private void Awake()
    {
        // 최초 씬의 Inspector 기본값을 사용하고, 이후 씬에서는 사용자가 조절한 값을 이어받는다.
        if (!_hasSessionVolume)
        {
            _sessionMusicVolume = musicVolume;
            _sessionEffectsVolume = effectsVolume;
        }
        SetMusicVolume(_sessionMusicVolume);
        SetEffectsVolume(_sessionEffectsVolume);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        _sessionMusicVolume = musicVolume;
        _hasSessionVolume = true;
        if (musicSource != null) musicSource.volume = musicVolume;
    }
    public void SetEffectsVolume(float volume)
    {
        effectsVolume = Mathf.Clamp01(volume);
        _sessionEffectsVolume = effectsVolume;
        _hasSessionVolume = true;
        for (int i = 0; i < transform.childCount; i++)
            if (transform.GetChild(i).TryGetComponent<SoundEffectPlayer>(out var player) && player.gameObject.activeSelf)
                player.SetMasterVolume(effectsVolume);
    }
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null) return;
        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        if (clip != null) musicSource.Play();
    }

    public void ConfigurePool(PrefabPoolManager pool)
    {
        _pool = pool;
        if (_pool != null && soundEffectPrefab != null)
            _pool.Prewarm(soundEffectPrefab.gameObject, initialCount, capacity);
    }

    /// <summary>미연결·음소거·풀 상한 도달 시 재생만 생략한다. 전투 진행에는 관여하지 않는다.</summary>
    public bool PlayEffect(AudioClip clip, float volume = 1f)
    {
        float finalVolume = Mathf.Clamp01(volume) * effectsVolume;
        if (!isActiveAndEnabled || clip == null || finalVolume <= 0f || _pool == null || soundEffectPrefab == null) return false;
        GameObject soundObject = _pool.Rent(soundEffectPrefab.gameObject, transform);
        if (soundObject == null) return false;
        soundObject.GetComponent<SoundEffectPlayer>().Play(_pool, clip, volume, effectsVolume);
        return true;
    }

    private void OnDisable()
    {
        if (musicSource != null) musicSource.Stop();
        if (_pool == null) return;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.TryGetComponent<SoundEffectPlayer>(out _))
                _pool.Return(child.gameObject, moveToStorage: false);
        }
    }
}
