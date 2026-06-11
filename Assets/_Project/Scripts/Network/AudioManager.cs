using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    /// <summary>
    /// The manager historically lived only in the HQ scene; playing the tower scene
    /// directly (preview / tests) left the whole game silent. Self-bootstrap instead.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("AudioManager (Auto)");
        go.AddComponent<AudioManager>();
    }

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
    AudioSource ambientSource;   // scene ambience (room tone / wind)
    AudioSource engineSource;    // van engine — separate so transit doesn't fight the scene bed

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
    AudioClip synthBreakerCrackle;
    AudioClip synthPowerRestore;
    AudioClip synthShutterSlam;
    AudioClip synthHeavyHoist;
    AudioClip synthGlassThud;
    AudioClip synthLeverClank;
    AudioClip synthStampThunk;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Persist across scene loads. Without this, the AudioManager living in HQ
        // gets destroyed when the school scene loads, but the static Instance still
        // references it — then any AudioSource field access throws MissingReferenceException
        // (which is exactly what happens during van transit -> PlayEngineIdle).
        DontDestroyOnLoad(gameObject);

        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.loop = true;
        ambientSource.spatialBlend = 0f;
        ambientSource.volume = 0.4f;

        engineSource = gameObject.AddComponent<AudioSource>();
        engineSource.loop = true;
        engineSource.spatialBlend = 0f;
        engineSource.volume = 0.25f;

        GenerateSynthClips();

        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplySceneAmbient(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (Instance == this) Instance = null;
    }

    // ─── Scene ambience auto-switch ───
    // HQ = fluorescent office hum; the tower = wind through the raw plate. Engine
    // idle lives on its own source, so transit overlaps the new scene's bed cleanly.

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;
        ApplySceneAmbient(scene.name);
    }

    void ApplySceneAmbient(string sceneName)
    {
        if (ambientSource == null) return;
        if (sceneName == "HQ") PlayOfficeAmbient();
        else if (sceneName.StartsWith("Tower")) PlayTowerAmbient();
        else ambientSource.Stop();
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
        synthBreakerCrackle = SynthAudio.BreakerCrackle("synth_breaker_crackle");
        synthPowerRestore = SynthAudio.PowerRestore("synth_power_restore");
        synthShutterSlam = SynthAudio.ShutterSlam("synth_shutter_slam");
        synthHeavyHoist = SynthAudio.HeavyHoist("synth_heavy_hoist");
        synthGlassThud = SynthAudio.GlassThud("synth_glass_thud");
        synthLeverClank = SynthAudio.LeverClank("synth_lever_clank");
        synthStampThunk = SynthAudio.StampThunk("synth_stamp_thunk");
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
        if (engineSource == null) return;
        if (engineSource.clip == synthEngineIdle && engineSource.isPlaying) return;
        engineSource.clip = synthEngineIdle;
        engineSource.Play();
    }

    public void StopEngineIdle()
    {
        if (engineSource == null) return;
        engineSource.Stop();
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

    // ─── Tower mission beats ───

    /// <summary>Electrical fizz while the breaker hold is charging (call ~every 0.4s).</summary>
    public void PlayBreakerCrackle(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(synthBreakerCrackle, position, 0.5f);
    }

    /// <summary>Relay thunk + hum swell at the breaker when power returns.</summary>
    public void PlayPowerRestored(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(synthPowerRestore, position, 0.8f);
    }

    /// <summary>Sheet-metal slam at a debt shutter dropping open.</summary>
    public void PlayShutterSlam(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(synthShutterSlam, position, 0.7f);
    }

    /// <summary>Hoisting a heavy carriable onto both hands.</summary>
    public void PlayHeavyPickup(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(synthHeavyHoist, position, 0.6f);
    }

    /// <summary>Hard landing of a fragile heavy object (completeness damage moment).</summary>
    public void PlayHeavyImpact(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(synthGlassThud, position, 0.85f);
    }

    /// <summary>The van depart lever latching.</summary>
    public void PlayLever(Vector3 position)
    {
        AudioSource.PlayClipAtPoint(synthLeverClank, position, 0.6f);
    }

    /// <summary>Stamp-on-paper: a settlement becoming official (UI, non-positional).</summary>
    public void PlayStamp()
    {
        if (sfxSource != null) sfxSource.PlayOneShot(synthStampThunk, 0.7f);
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
        if (ambientSource == null) return;
        ambientSource.clip = synthFluorescentHum;
        ambientSource.volume = 0.15f;
        ambientSource.Play();
    }

    public void PlayTowerAmbient()
    {
        if (ambientSource == null) return;
        ambientSource.clip = synthWindAmbient;
        ambientSource.volume = 0.12f;
        ambientSource.Play();
    }

    public void StopAmbient()
    {
        if (ambientSource == null) return;
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
