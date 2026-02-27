using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource; // 배경음악용 오디오 소스
    public AudioSource sfxSource; // 효과음용 오디오 소스
    public AudioSource uiSource;  // 조작음용 오디오 소스

    [Header("Default Settings")]
    [Range(0f, 1f)] public float defaultMasterVolume = 1f;
    [Range(0f, 1f)] public float defaultBGMVolume = 0.5f;
    [Range(0f, 1f)] public float defaultSFXVolume = 0.5f;
    [Range(0f, 1f)] public float defaultUIVolume = 0.5f;

    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string UI_VOLUME_KEY = "UIVolume";

    private float _masterVolume;
    private float _bgmVolume;
    private float _sfxVolume;
    private float _uiVolume;

    private void Awake()
    {
        // 싱글턴 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 오디오 소스가 할당되지 않았다면 자동으로 생성
        if (bgmSource == null)
        {
            GameObject bgmObj = new GameObject("BGMSource");
            bgmObj.transform.parent = transform;
            bgmSource = bgmObj.AddComponent<AudioSource>();
            bgmSource.loop = true;
        }

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.parent = transform;
            sfxSource = sfxObj.AddComponent<AudioSource>();
        }

        if (uiSource == null)
        {
            GameObject uiObj = new GameObject("UISource");
            uiObj.transform.parent = transform;
            uiSource = uiObj.AddComponent<AudioSource>();
        }

        LoadVolume();
    }

    private void LoadVolume()
    {
        _masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, defaultMasterVolume);
        _bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, defaultBGMVolume);
        _sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaultSFXVolume);
        _uiVolume = PlayerPrefs.GetFloat(UI_VOLUME_KEY, defaultUIVolume);

        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null) bgmSource.volume = _bgmVolume * _masterVolume;
        if (sfxSource != null) sfxSource.volume = _sfxVolume * _masterVolume;
        if (uiSource != null) uiSource.volume = _uiVolume * _masterVolume;
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume = volume;
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
        ApplyVolumes();
    }

    public void SetBGMVolume(float volume)
    {
        _bgmVolume = volume;
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, volume);
        ApplyVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        _sfxVolume = volume;
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
        ApplyVolumes();
    }

    public void SetUIVolume(float volume)
    {
        _uiVolume = volume;
        PlayerPrefs.SetFloat(UI_VOLUME_KEY, volume);
        ApplyVolumes();
    }

    public float GetMasterVolume() => _masterVolume;
    public float GetBGMVolume() => _bgmVolume;
    public float GetSFXVolume() => _sfxVolume;
    public float GetUIVolume() => _uiVolume;

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource.clip == clip) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    public void PlayUISound(AudioClip clip)
    {
        uiSource.PlayOneShot(clip);
    }
}