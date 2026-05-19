using UnityEngine;

/// <summary>
/// Simple audio manager for world-space SFX. Referenced by game systems.
/// Attach AudioSources as children and assign clips in inspector.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Clips")]
    [SerializeField] AudioClip[] robotDialogueClips;  // "检测到地面污染..." etc.
    [SerializeField] AudioClip robotRamClip;
    [SerializeField] AudioClip pumpStartupClip;
    [SerializeField] AudioClip[] survivorCalloutClips;
    [SerializeField] AudioClip survivorCalmClip;
    [SerializeField] AudioClip waterRiseClip;
    [SerializeField] AudioClip evacBroadcastClip;

    AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        sfxSource = GetComponent<AudioSource>();
    }

    public void PlayRobotDialogue(Vector3 position)
    {
        if (robotDialogueClips.Length == 0) return;
        var clip = robotDialogueClips[Random.Range(0, robotDialogueClips.Length)];
        AudioSource.PlayClipAtPoint(clip, position);
    }

    public void PlayRobotRam(Vector3 position)
    { if (robotRamClip != null) AudioSource.PlayClipAtPoint(robotRamClip, position); }

    public void PlayPumpStartup(Vector3 position)
    { if (pumpStartupClip != null) AudioSource.PlayClipAtPoint(pumpStartupClip, position); }

    public void PlaySurvivorCallout(Vector3 position)
    {
        if (survivorCalloutClips.Length == 0) return;
        var clip = survivorCalloutClips[Random.Range(0, survivorCalloutClips.Length)];
        AudioSource.PlayClipAtPoint(clip, position);
    }

    public void PlaySurvivorCalm(Vector3 position)
    { if (survivorCalmClip != null) AudioSource.PlayClipAtPoint(survivorCalmClip, position); }

    public void PlayEvacBroadcast()
    { if (evacBroadcastClip != null && sfxSource != null) sfxSource.PlayOneShot(evacBroadcastClip); }
}
