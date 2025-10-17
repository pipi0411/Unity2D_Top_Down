using UnityEngine;
using System.Collections.Generic;

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
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    [Header("Player Sounds")]
    [SerializeField] private AudioSource playerSource;        // dùng cho loop run
    [SerializeField] private AudioSource playerSfxSource;     // dùng cho dash/oneshot
    [Range(0f, 1f)] public float playerVolume = 0.5f;

    [Header("Item Sounds")]
    [SerializeField] private AudioSource itemSource;
    [Range(0f, 1f)] public float itemVolume = 1f;

    [Header("Player Movement & State Sounds")]
    [SerializeField] private AudioClip runClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip dashClip;
    private bool isRunSoundPlaying = false;

    [Header("Run Settings")]
    [Range(0f, 2f)] [SerializeField] private float runVolumeMultiplier = 1f;
    [Range(0.5f, 2f)] [SerializeField] private float runPitch = 1f;

    [Header("Dash Settings")]
    [Range(0f, 2f)] [SerializeField] private float dashVolumeMultiplier = 1f;
    [SerializeField] private Vector2 dashPitchRange = new Vector2(0.95f, 1.05f);
    [Range(0f, 1f)] [SerializeField] private float dashDuckPercent = 0.6f; // hạ % âm run khi dash
    [Range(0f, 1f)] [SerializeField] private float dashDuckTime = 0.15f;   // thời gian hạ và trả lại

    [Header("Weapon Sounds")]
    [SerializeField] private List<WeaponSoundSet> weaponSoundSets;
    private WeaponSoundSet currentWeaponSounds;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure sources exist before use
        EnsureBgmSource();
        EnsurePlayerSource();
        EnsurePlayerSfxSource();
        EnsureItemSource();

        if (defaultBgm != null)
            PlayBgm(defaultBgm);
    }

    private void EnsureBgmSource()
    {
        if (!bgmSource) bgmSource = GetComponent<AudioSource>();
        if (!bgmSource) bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;
    }

    private void EnsurePlayerSource()
    {
        if (!playerSource) playerSource = gameObject.AddComponent<AudioSource>();
        playerSource.playOnAwake = false;
        playerSource.volume = playerVolume * runVolumeMultiplier;
        playerSource.pitch = runPitch;
    }

    private void EnsurePlayerSfxSource()
    {
        if (!playerSfxSource) playerSfxSource = gameObject.AddComponent<AudioSource>();
        playerSfxSource.playOnAwake = false;
        playerSfxSource.loop = false;
        playerSfxSource.volume = playerVolume; // nhân thêm khi PlayOneShot
    }

    private void EnsureItemSource()
    {
        if (!itemSource) itemSource = gameObject.AddComponent<AudioSource>();
        itemSource.playOnAwake = false;
        itemSource.volume = itemVolume;
    }

    // ======== BGM ========
    public void PlayBgm(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBgm() => bgmSource.Stop();

    // ======== PLAYER STATES ========
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
        {
            playerSfxSource.pitch = 1f;
            playerSfxSource.PlayOneShot(deathClip, playerVolume);
        }
    }

    public void PlayPlayerDash()
    {
        if (dashClip == null) return;

        // random pitch nhẹ cho cảm giác tự nhiên
        playerSfxSource.pitch = Random.Range(dashPitchRange.x, dashPitchRange.y);
        playerSfxSource.PlayOneShot(dashClip, playerVolume * dashVolumeMultiplier);

        // tạm hạ tiếng chạy rồi phục hồi (nếu đang chạy)
        if (isRunSoundPlaying)
            StartCoroutine(DuckRunRoutine());
    }

    private System.Collections.IEnumerator DuckRunRoutine()
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

    // ======== PLAYER SFX ========
    public void PlayPlayerSfx(string soundName)
    {
        AudioClip clip = Resources.Load<AudioClip>(soundName);
        if (clip != null)
            playerSource.PlayOneShot(clip, playerVolume);
        else
            Debug.LogWarning($"[AudioManager] Không tìm thấy clip: {soundName} trong Resources!");
    }

    // ======== ITEM ========
    public void PlayItemSfx(AudioClip clip)
    {
        if (clip != null)
            itemSource.PlayOneShot(clip, itemVolume);
    }

    // ======== WEAPON ========
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

    // ======== VOLUME ========
    public void SetBgmVolume(float v) { bgmVolume = v; bgmSource.volume = v; }
    public void SetPlayerVolume(float v)
    {
        playerVolume = v;
        playerSource.volume = v * runVolumeMultiplier;
        playerSfxSource.volume = v;
    }
    public void SetItemVolume(float v) { itemVolume = v; itemSource.volume = v; }

    // Reset static when domain reload bị tắt trong Editor
#if UNITY_EDITOR
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }
#endif
}
