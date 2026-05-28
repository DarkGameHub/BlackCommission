using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Inspector Clips (optional overrides)")]
    [SerializeField] AudioClip[] robotDialogueClips;
    [SerializeField] AudioClip robotRamClip;
    [SerializeField] AudioClip pumpStartupClip;
    [SerializeField] AudioClip[] survivorCalloutClips;
    [SerializeField] AudioClip survivorCalmClip;
    [SerializeField] AudioClip waterRiseClip;
    [SerializeField] AudioClip evacBroadcastClip;
    [SerializeField] AudioClip robotStunnedClip;
    [SerializeField] AudioClip[] phaseBroadcastClips;

    AudioSource sfxSource;
    AudioSource ambientSource;

    // Synth-generated clips (lazy init)
    AudioClip synthFootstepA;
    AudioClip synthFootstepB;
    AudioClip synthComputerBeep;
    AudioClip synthComputerBoot;
    AudioClip synthEngineStart;
    AudioClip synthEngineIdle;
    AudioClip synthDoorCreak;
    AudioClip synthPickup;
    AudioClip synthMonsterGrowl;
    AudioClip synthMonsterChase;
    AudioClip synthFluorescentHum;
    AudioClip synthWindAmbient;
    AudioClip synthFlashlightClick;
    AudioClip synthPlayerDowned;
    AudioClip synthPlayerRevived;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.loop = true;
        ambientSource.spatialBlend = 0f;
        ambientSource.volume = 0.4f;

        GenerateSynthClips();
    }

    void GenerateSynthClips()
    {
        synthFootstepA = SynthAudio.Footstep("synth_footstep_a", 1.0f);
        synthFootstepB = SynthAudio.Footstep("synth_footstep_b", 0.85f);
        synthComputerBeep = SynthAudio.ComputerBeep("synth_computer_beep");
        synthComputerBoot = SynthAudio.ComputerBoot("synth_computer_boot");
        synthEngineStart = SynthAudio.EngineStart("synth_engine_start");
        synthEngineIdle = SynthAudio.EngineIdle("synth_engine_idle", 4f);
        synthDoorCreak = SynthAudio.DoorCreak("synth_door_creak");
        synthPickup = SynthAudio.Pickup("synth_pickup");
        synthMonsterGrowl = SynthAudio.MonsterGrowl("synth_monster_growl");
        synthMonsterChase = SynthAudio.MonsterChase("synth_monster_chase");
        synthFluorescentHum = SynthAudio.FluorescentHum("synth_fluorescent_hum", 6f);
        synthWindAmbient = SynthAudio.WindAmbient("synth_wind_ambient", 8f);
        synthFlashlightClick = SynthAudio.FlashlightClick("synth_flashlight_click");
        synthPlayerDowned = SynthAudio.Tone("synth_player_downed", 180f, 0.6f, 0.25f, SynthAudio.WaveShape.Saw);
        synthPlayerRevived = SynthAudio.Tone("synth_player_revived", 520f, 0.3f, 0.2f, SynthAudio.WaveShape.Sine);
    }

    // ─── Footsteps ───

    public void PlayFootstep(Vector3 position)
    {
        AudioClip clip = Random.value > 0.5f ? synthFootstepA : synthFootstepB;
        AudioSource.PlayClipAtPoint(clip, position, 0.35f);
    }

    // ─── Computer ───

    public void PlayComputerOpen(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(synthComputerBoot, position, 0.5f);
    }

    public void PlayComputerBeep(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(synthComputerBeep, position, 0.4f);
    }

    // ─── Van ───

    public void PlayEngineStart(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(synthEngineStart, position, 0.6f);
    }

    public void PlayEngineIdle()
    {
        if (ambientSource.clip == synthEngineIdle && ambientSource.isPlaying) return;
        ambientSource.clip = synthEngineIdle;
        ambientSource.volume = 0.25f;
        ambientSource.Play();
    }

    public void StopEngineIdle()
    {
        if (ambientSource.clip == synthEngineIdle)
            ambientSource.Stop();
    }

    // ─── Doors ───

    public void PlayDoorCreak(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(synthDoorCreak, position, 0.5f);
    }

    // ─── Items ───

    public void PlayPickup(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(synthPickup, position, 0.45f);
    }


    public void PlayFlashlightClick(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(synthFlashlightClick, position, 0.4f);
    }

    // ─── Monster ───

    public void PlayMonsterGrowl(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(synthMonsterGrowl, position, 0.65f);
    }

    public void PlayMonsterChaseAlert(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(synthMonsterChase, position, 0.7f);
    }

    // ─── Player State ───

    public void PlaySurvivorCallout(Vector3 position)
    {
        if (survivorCalloutClips != null && survivorCalloutClips.Length > 0)
        {
            var clip = survivorCalloutClips[Random.Range(0, survivorCalloutClips.Length)];
            if (clip != null) { AudioSource.PlayClipAtPoint(clip, position); return; }
        }
        AudioSource.PlayClipAtPoint(synthPlayerDowned, position, 0.55f);
    }

    public void PlaySurvivorCalm(Vector3 position)
    {
        if (survivorCalmClip != null) { AudioSource.PlayClipAtPoint(survivorCalmClip, position); return; }
        AudioSource.PlayClipAtPoint(synthPlayerRevived, position, 0.45f);
    }

    // ─── Ambient ───

    public void PlayOfficeAmbient()
    {
        ambientSource.clip = synthFluorescentHum;
        ambientSource.volume = 0.15f;
        ambientSource.Play();
    }

    public void PlaySchoolAmbient()
    {
        ambientSource.clip = synthWindAmbient;
        ambientSource.volume = 0.12f;
        ambientSource.Play();
    }

    public void StopAmbient()
    {
        ambientSource.Stop();
    }

    // ─── Legacy Methods (kept for existing callers) ───

    public void PlayRobotDialogue(Vector3 position)
    {
        if (robotDialogueClips == null || robotDialogueClips.Length == 0) return;
        var clip = robotDialogueClips[Random.Range(0, robotDialogueClips.Length)];
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position);
    }

    public void PlayRobotRam(Vector3 position)
    { if (robotRamClip != null) AudioSource.PlayClipAtPoint(robotRamClip, position); }

    public void PlayPumpStartup(Vector3 position)
    { if (pumpStartupClip != null) AudioSource.PlayClipAtPoint(pumpStartupClip, position); }

    public void PlayEvacBroadcast()
    { if (evacBroadcastClip != null && sfxSource != null) sfxSource.PlayOneShot(evacBroadcastClip); }

    public void PlayRobotStunned(Vector3 position)
    { if (robotStunnedClip != null) AudioSource.PlayClipAtPoint(robotStunnedClip, position); }

    public void PlayPhaseBroadcast(int phaseIndex)
    {
        if (phaseBroadcastClips == null || phaseIndex < 0 || phaseIndex >= phaseBroadcastClips.Length) return;
        if (phaseBroadcastClips[phaseIndex] != null && sfxSource != null)
            sfxSource.PlayOneShot(phaseBroadcastClips[phaseIndex]);
    }
}
