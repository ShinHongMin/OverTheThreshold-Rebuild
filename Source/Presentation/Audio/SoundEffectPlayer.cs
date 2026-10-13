using UnityEngine;

/// <summary>효과음 하나를 재생하고 종료되면 공용 프리팹 풀에 반환한다.</summary>
[RequireComponent(typeof(AudioSource))]
public sealed class SoundEffectPlayer : MonoBehaviour, IPooledView
{
    private AudioSource _audioSource;
    private PrefabPoolManager _pool;
    private bool _isPlaying;
    private float _clipVolume;

    public void OnRent()
    {
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
        _isPlaying = false;
    }

    public void Play(PrefabPoolManager pool, AudioClip clip, float volume, float masterVolume = 1f)
    {
        _pool = pool;
        _audioSource.clip = clip;
        _clipVolume = Mathf.Clamp01(volume);
        SetMasterVolume(masterVolume);
        _audioSource.loop = false;
        _audioSource.Play();
        _isPlaying = true;
    }

    public void SetMasterVolume(float volume)
    {
        if (_audioSource != null) _audioSource.volume = _clipVolume * Mathf.Clamp01(volume);
    }

    private void Update()
    {
        if (!_isPlaying || AudioListener.pause) return;
        if (_audioSource.clip != null && _audioSource.clip.loadState == AudioDataLoadState.Loading) return;
        if (!_audioSource.isPlaying) _pool.Return(gameObject);
    }

    public void OnReturn()
    {
        _isPlaying = false;
        _audioSource.Stop();
        _audioSource.clip = null;
        _audioSource.volume = 1f;
        _pool = null;
    }

    // 부모 비활성화 중에는 계층을 옮기지 않고 대여 상태만 반환한다.
    private void OnDisable()
    {
        if (_pool != null) _pool.Return(gameObject, moveToStorage: false);
    }
}
