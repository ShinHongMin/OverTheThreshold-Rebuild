using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>옵션 버튼과 음량 슬라이더. 씬 이동·전투 중단은 연결된 소유자에게 요청한다.</summary>
public sealed class OptionsView : PopupView
{
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider effectsVolumeSlider;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button returnToTitleButton;
    private SoundManager _soundManager;
    private Action _returnToTitle;

    private void Awake()
    {
        resumeButton.onClick.AddListener(Resume);
        returnToTitleButton.onClick.AddListener(ReturnToTitle);
        musicVolumeSlider.onValueChanged.AddListener(ChangeMusicVolume);
        effectsVolumeSlider.onValueChanged.AddListener(ChangeEffectsVolume);
    }
    public void Configure(SoundManager soundManager, Action returnToTitle)
    {
        _soundManager = soundManager;
        _returnToTitle = returnToTitle;
    }
    public override void Show(float duration = 0f)
    {
        base.Show(duration);
        bool hasSound = _soundManager != null;
        musicVolumeSlider.interactable = hasSound;
        effectsVolumeSlider.interactable = hasSound;
        returnToTitleButton.interactable = _returnToTitle != null;
        if (!hasSound) return;
        musicVolumeSlider.SetValueWithoutNotify(_soundManager.MusicVolume);
        effectsVolumeSlider.SetValueWithoutNotify(_soundManager.EffectsVolume);
    }
    private void Resume() => Hide();
    private void ReturnToTitle() => _returnToTitle?.Invoke();
    private void ChangeMusicVolume(float value) { if (_soundManager != null) _soundManager.SetMusicVolume(value); }
    private void ChangeEffectsVolume(float value) { if (_soundManager != null) _soundManager.SetEffectsVolume(value); }
    private void OnDestroy()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
        if (returnToTitleButton != null) returnToTitleButton.onClick.RemoveListener(ReturnToTitle);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveListener(ChangeMusicVolume);
        if (effectsVolumeSlider != null) effectsVolumeSlider.onValueChanged.RemoveListener(ChangeEffectsVolume);
    }
}
