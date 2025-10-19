using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[System.Serializable]
public class WeaponSoundSet
{
    public string weaponName;
    public List<AudioClip> attackClips;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Background Music")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip defaultBgm;
    [SerializeField] private BgmProfile bgmProfile; // ✅ ScriptableObject chứa BGM cho từng scene
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [SerializeField] private float fadeTime = 0.6f; // ✅ thời gian fade khi đổi nhạc

    [Header("Player Sounds")]
    [SerializeField] private AudioSource playerSource;        // dùng cho loop run
    [SerializeField] private AudioSource playerSfxSource;     // dùng cho dash, hurt, death
    [Range(0f, 1f)] public float playerVolume = 0.5f;

    [Header("Item Sounds")]
    [SerializeField] private AudioSource itemSource;
    [Range(0f, 1f)] public float itemVolume = 0.5f;
    [SerializeField] private AudioClip itemPickupClip;
    [SerializeField] private AudioClip itemDropClip;

    [Header("Player Movement & State Sounds")]
    [SerializeField] private AudioClip runClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip dashClip;
    [SerializeField] private AudioClip hurtClip;
    private bool isRunSoundPlaying = false;

    [Header("Run Settings")]
    [Range(0f, 2f)] [SerializeField] private float runVolumeMultiplier = 1f;
    [Range(0.5f, 2f)] [SerializeField] private float runPitch = 1f;

    [Header("Dash Settings")]
    [Range(0f, 2f)] [SerializeField] private float dashVolumeMultiplier = 1f;
    [SerializeField] private Vector2 dashPitchRange = new Vector2(0.95f, 1.05f);
    [Range(0f, 1f)] [SerializeField] private float dashDuckPercent = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float dashDuckTime = 0.15f;

    [Header("Weapon Sounds")]
    [SerializeField] private List<WeaponSoundSet> weaponSoundSets;
    private WeaponSoundSet currentWeaponSounds;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();

        if (defaultBgm != null)
            PlayBgm(defaultBgm);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void EnsureAudioSources()
    {
        if (!bgmSource) bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        if (!playerSource) playerSource = gameObject.AddComponent<AudioSource>();
        playerSource.playOnAwake = false;
        playerSource.volume = playerVolume * runVolumeMultiplier;
        playerSource.pitch = runPitch;

        if (!playerSfxSource) playerSfxSource = gameObject.AddComponent<AudioSource>();
        playerSfxSource.playOnAwake = false;
        playerSfxSource.loop = false;
        playerSfxSource.volume = playerVolume;

        if (!itemSource) itemSource = gameObject.AddComponent<AudioSource>();
        itemSource.playOnAwake = false;
        itemSource.volume = itemVolume;
    }

    // ========== BGM ==========
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (bgmProfile == null)
        {
            Debug.LogWarning("[AudioManager] Chưa gán BgmProfile!");
            return;
        }

        AudioClip clip = bgmProfile.GetClipForScene(scene.name);
        if (clip != null)
        {
            PlayBgm(clip);
            Debug.Log($"[AudioManager] Đổi nhạc BGM cho scene: {scene.name}");
        }
        else
        {
            Debug.Log($"[AudioManager] Không có nhạc cho scene: {scene.name}");
        }
    }

    public void PlayBgm(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeToNewBgm(clip));
    }
    public void PauseBGM()
    {
        if (bgmSource.isPlaying)
            bgmSource.Pause();
    }
    public void ResumeBGM()
    {
        if (!bgmSource.isPlaying)
            bgmSource.UnPause();
    }

    private IEnumerator FadeToNewBgm(AudioClip newClip)
    {
        float startVolume = bgmSource.volume;

        // fade out
        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= Time.deltaTime / fadeTime * bgmVolume;
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.Play();

        // fade in
        while (bgmSource.volume < bgmVolume)
        {
            bgmSource.volume += Time.deltaTime / fadeTime * bgmVolume;
            yield return null;
        }

        bgmSource.volume = bgmVolume;
    }

    public void StopBgm() => bgmSource.Stop();

    // ========== PLAYER ==========
    public void PlayPlayerRun()
    {
        if (runClip != null && !isRunSoundPlaying)
        {
            playerSource.loop = true;
            playerSource.clip = runClip;
            playerSource.volume = playerVolume * runVolumeMultiplier;
            playerSource.pitch = runPitch;
            playerSource.Play();
            isRunSoundPlaying = true;
        }
    }

    public void StopPlayerRun()
    {
        if (isRunSoundPlaying)
        {
            playerSource.Stop();
            playerSource.clip = null;
            isRunSoundPlaying = false;
        }
    }

    public void PlayPlayerDeath()
    {
        if (deathClip != null)
            playerSfxSource.PlayOneShot(deathClip, playerVolume);
    }

    public void PlayPlayerHurt()
    {
        if (hurtClip != null)
            playerSfxSource.PlayOneShot(hurtClip, playerVolume);
    }

    public void PlayPlayerDash()
    {
        if (dashClip == null) return;
        playerSfxSource.pitch = Random.Range(dashPitchRange.x, dashPitchRange.y);
        playerSfxSource.PlayOneShot(dashClip, playerVolume * dashVolumeMultiplier);
        if (isRunSoundPlaying)
            StartCoroutine(DuckRunRoutine());
    }

    private IEnumerator DuckRunRoutine()
    {
        float original = playerSource.volume;
        float target = playerVolume * runVolumeMultiplier * dashDuckPercent;

        float t = 0f;
        while (t < dashDuckTime)
        {
            t += Time.unscaledDeltaTime;
            playerSource.volume = Mathf.Lerp(original, target, t / dashDuckTime);
            yield return null;
        }

        t = 0f;
        while (t < dashDuckTime)
        {
            t += Time.unscaledDeltaTime;
            playerSource.volume = Mathf.Lerp(target, original, t / dashDuckTime);
            yield return null;
        }
        playerSource.volume = original;
    }

    // ========== ITEM ==========
    public void PlayItemPickup()
    {
        if (itemPickupClip != null)
            itemSource.PlayOneShot(itemPickupClip, itemVolume);
    }

    public void PlayItemDrop()
    {
        if (itemDropClip != null)
            itemSource.PlayOneShot(itemDropClip, itemVolume);
    }

    // ========== WEAPON ==========
    public void SetCurrentWeapon(string weaponName)
    {
        currentWeaponSounds = weaponSoundSets.Find(w => w.weaponName == weaponName);
        if (currentWeaponSounds == null)
            Debug.LogWarning($"[AudioManager] Không tìm thấy âm thanh cho vũ khí: {weaponName}");
    }

    public void PlayWeaponAttack()
    {
        if (currentWeaponSounds == null || currentWeaponSounds.attackClips.Count == 0)
        {
            Debug.LogWarning("[AudioManager] Chưa có âm thanh tấn công cho vũ khí hiện tại!");
            return;
        }

        var clip = currentWeaponSounds.attackClips[Random.Range(0, currentWeaponSounds.attackClips.Count)];
        playerSource.PlayOneShot(clip, playerVolume);
    }

    // ========== VOLUME ==========
    public void SetBgmVolume(float v) { bgmVolume = v; bgmSource.volume = v; }
    public void SetPlayerVolume(float v)
    {
        playerVolume = v;
        playerSource.volume = v * runVolumeMultiplier;
        playerSfxSource.volume = v;
    }
    public void SetItemVolume(float v) { itemVolume = v; itemSource.volume = v; }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }
#endif
}
