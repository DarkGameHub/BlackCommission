using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ProximityVoiceChat : MonoBehaviour
{
    const string UplinkMessage = "AS_VOICE_UP";
    const string DownlinkMessage = "AS_VOICE_DOWN";
    const int SampleRate = 8000;
    const int SamplesPerPacket = 320;
    const float MicRestartDelay = 2f;

    static ProximityVoiceChat instance;

    public static bool VoiceEnabled
    {
        get => PlayerPrefs.GetInt("AS.Voice.Enabled", 1) != 0;
        set => PlayerPrefs.SetInt("AS.Voice.Enabled", value ? 1 : 0);
    }

    public static bool Muted
    {
        get => PlayerPrefs.GetInt("AS.Voice.Muted", 0) != 0;
        set => PlayerPrefs.SetInt("AS.Voice.Muted", value ? 1 : 0);
    }

    public static float MicGain
    {
        get => PlayerPrefs.GetFloat("AS.Voice.MicGain", 1f);
        set => PlayerPrefs.SetFloat("AS.Voice.MicGain", Mathf.Clamp(value, 0f, 2f));
    }

    public static float OutputVolume
    {
        get => PlayerPrefs.GetFloat("AS.Voice.OutputVolume", 1f);
        set => PlayerPrefs.SetFloat("AS.Voice.OutputVolume", Mathf.Clamp(value, 0f, 2f));
    }

    public static float MaxDistance
    {
        get => PlayerPrefs.GetFloat("AS.Voice.MaxDistance", 18f);
        set => PlayerPrefs.SetFloat("AS.Voice.MaxDistance", Mathf.Clamp(value, 4f, 40f));
    }

    public static int MicrophoneDeviceIndex
    {
        get => PlayerPrefs.GetInt("AS.Voice.MicrophoneDeviceIndex", 0);
        set => PlayerPrefs.SetInt("AS.Voice.MicrophoneDeviceIndex", Mathf.Max(0, value));
    }

    public static bool PushToTalk
    {
        get => PlayerPrefs.GetInt("AS.Voice.PushToTalk", 0) != 0;
        set => PlayerPrefs.SetInt("AS.Voice.PushToTalk", value ? 1 : 0);
    }

    // Locally muted speakers (per-listener; not networked). Cleared at process start.
    static readonly HashSet<ulong> mutedClients = new();
    public static bool IsClientMuted(ulong clientId) => mutedClients.Contains(clientId);
    public static void SetClientMuted(ulong clientId, bool muted)
    {
        if (muted) mutedClients.Add(clientId);
        else mutedClients.Remove(clientId);
    }
    public static void ToggleClientMuted(ulong clientId) => SetClientMuted(clientId, !IsClientMuted(clientId));

    static bool PushToTalkHeld()
    {
        var kb = Keyboard.current;
        return kb != null && kb.vKey.isPressed;
    }

    public static string SelectedMicrophoneDeviceName
    {
        get
        {
            string[] devices = Microphone.devices;
            if (devices == null || devices.Length == 0) return "No microphone";
            int index = Mathf.Clamp(MicrophoneDeviceIndex, 0, devices.Length - 1);
            return devices[index];
        }
    }

    AudioClip microphoneClip;
    string microphoneDevice;
    int lastMicPosition;
    float nextMicStartTime;
    CustomMessagingManager registeredMessagingManager;
    readonly float[] sampleBuffer = new float[SamplesPerPacket];
    readonly byte[] encodedBuffer = new byte[SamplesPerPacket];
    readonly Dictionary<ulong, AudioSource> remoteSources = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void EnsureInstance()
    {
        if (instance != null) return;

        var go = new GameObject("MVP_ProximityVoiceChat");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ProximityVoiceChat>();
    }

    void Update()
    {
        RegisterMessageHandlers();
        if (!VoiceEnabled || Muted)
        {
            StopMicrophone();
            return;
        }
        // Push-to-talk: only transmit while the talk key is held.
        if (PushToTalk && !PushToTalkHeld())
        {
            StopMicrophone();
            return;
        }
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            StopMicrophone();
            return;
        }

        EnsureMicrophone();
        CaptureAndSend();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
        StopMicrophone();
        if (registeredMessagingManager != null)
        {
            registeredMessagingManager.UnregisterNamedMessageHandler(UplinkMessage);
            registeredMessagingManager.UnregisterNamedMessageHandler(DownlinkMessage);
            registeredMessagingManager = null;
        }
    }

    void RegisterMessageHandlers()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || network.CustomMessagingManager == null) return;
        if (registeredMessagingManager == network.CustomMessagingManager) return;

        if (registeredMessagingManager != null)
        {
            registeredMessagingManager.UnregisterNamedMessageHandler(UplinkMessage);
            registeredMessagingManager.UnregisterNamedMessageHandler(DownlinkMessage);
        }

        network.CustomMessagingManager.RegisterNamedMessageHandler(UplinkMessage, HandleUplinkVoiceMessage);
        network.CustomMessagingManager.RegisterNamedMessageHandler(DownlinkMessage, HandleDownlinkVoiceMessage);
        registeredMessagingManager = network.CustomMessagingManager;
    }

    void EnsureMicrophone()
    {
        if (Time.unscaledTime < nextMicStartTime) return;
        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            nextMicStartTime = Time.unscaledTime + MicRestartDelay;
            return;
        }

        string desiredDevice = SelectedMicrophoneDeviceName;
        if (microphoneClip != null && Microphone.IsRecording(microphoneDevice) && microphoneDevice == desiredDevice) return;
        if (microphoneClip != null && microphoneDevice != desiredDevice)
            StopMicrophone();

        microphoneDevice = desiredDevice;
        microphoneClip = Microphone.Start(microphoneDevice, true, 1, SampleRate);
        lastMicPosition = 0;
        nextMicStartTime = Time.unscaledTime + MicRestartDelay;
    }

    void CaptureAndSend()
    {
        if (microphoneClip == null) return;

        int position = Microphone.GetPosition(microphoneDevice);
        if (position < 0 || position == lastMicPosition) return;

        int available = position >= lastMicPosition
            ? position - lastMicPosition
            : microphoneClip.samples - lastMicPosition + position;

        while (available >= SamplesPerPacket)
        {
            if (lastMicPosition + SamplesPerPacket > microphoneClip.samples)
            {
                available -= microphoneClip.samples - lastMicPosition;
                lastMicPosition = 0;
                continue;
            }

            microphoneClip.GetData(sampleBuffer, lastMicPosition);
            EncodeSamples(sampleBuffer, encodedBuffer);
            SendVoicePacket(encodedBuffer, SamplesPerPacket);

            lastMicPosition = (lastMicPosition + SamplesPerPacket) % microphoneClip.samples;
            available -= SamplesPerPacket;
        }
    }

    void StopMicrophone()
    {
        if (!string.IsNullOrEmpty(microphoneDevice) && Microphone.IsRecording(microphoneDevice))
            Microphone.End(microphoneDevice);
        microphoneClip = null;
        lastMicPosition = 0;
    }

    static void EncodeSamples(float[] samples, byte[] output)
    {
        float gain = MicGain;
        for (int i = 0; i < output.Length; i++)
        {
            float sample = Mathf.Clamp(samples[i] * gain, -1f, 1f);
            output[i] = (byte)Mathf.RoundToInt((sample * 0.5f + 0.5f) * 255f);
        }
    }

    static void DecodeSamples(byte[] input, float[] output)
    {
        for (int i = 0; i < input.Length; i++)
            output[i] = (input[i] / 255f - 0.5f) * 2f;
    }

    void SendVoicePacket(byte[] payload, int payloadLength)
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsListening || network.CustomMessagingManager == null) return;

        Vector3 position = GetLocalVoicePosition();
        if (network.IsServer)
        {
            RelayVoicePacket(network.LocalClientId, position, payload, payloadLength);
            return;
        }

        using var writer = new FastBufferWriter(sizeof(float) * 3 + sizeof(int) + payloadLength, Unity.Collections.Allocator.Temp);
        writer.WriteValueSafe(position.x);
        writer.WriteValueSafe(position.y);
        writer.WriteValueSafe(position.z);
        writer.WriteValueSafe(payloadLength);
        writer.WriteBytesSafe(payload, payloadLength);
        network.CustomMessagingManager.SendNamedMessage(
            UplinkMessage,
            NetworkManager.ServerClientId,
            writer,
            NetworkDelivery.Unreliable);
    }

    void HandleUplinkVoiceMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (!TryReadVoicePayload(ref reader, out Vector3 position, out byte[] payload)) return;

        PlayVoicePacket(senderClientId, position, payload);
        RelayVoicePacket(senderClientId, position, payload, payload.Length);
    }

    void RelayVoicePacket(ulong senderClientId, Vector3 position, byte[] payload, int payloadLength)
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || network.CustomMessagingManager == null) return;

        using var writer = new FastBufferWriter(sizeof(ulong) + sizeof(float) * 3 + sizeof(int) + payloadLength, Unity.Collections.Allocator.Temp);
        writer.WriteValueSafe(senderClientId);
        writer.WriteValueSafe(position.x);
        writer.WriteValueSafe(position.y);
        writer.WriteValueSafe(position.z);
        writer.WriteValueSafe(payloadLength);
        writer.WriteBytesSafe(payload, payloadLength);

        foreach (ulong clientId in network.ConnectedClientsIds)
        {
            if (clientId == senderClientId) continue;
            network.CustomMessagingManager.SendNamedMessage(DownlinkMessage, clientId, writer, NetworkDelivery.Unreliable);
        }
    }

    void HandleDownlinkVoiceMessage(ulong serverClientId, FastBufferReader reader)
    {
        if (!TryReadRelayedVoicePayload(ref reader, out ulong senderClientId, out Vector3 position, out byte[] payload)) return;
        PlayVoicePacket(senderClientId, position, payload);
    }

    static bool TryReadVoicePayload(ref FastBufferReader reader, out Vector3 position, out byte[] payload)
    {
        position = Vector3.zero;
        payload = null;
        if (reader.Length < sizeof(float) * 3 + sizeof(int)) return false;

        reader.ReadValueSafe(out float x);
        reader.ReadValueSafe(out float y);
        reader.ReadValueSafe(out float z);
        reader.ReadValueSafe(out int length);
        if (length <= 0 || length > SamplesPerPacket) return false;

        payload = new byte[length];
        reader.ReadBytesSafe(ref payload, length);
        position = new Vector3(x, y, z);
        return true;
    }

    static bool TryReadRelayedVoicePayload(ref FastBufferReader reader, out ulong senderClientId, out Vector3 position, out byte[] payload)
    {
        senderClientId = 0;
        position = Vector3.zero;
        payload = null;
        if (reader.Length < sizeof(ulong) + sizeof(float) * 3 + sizeof(int)) return false;

        reader.ReadValueSafe(out senderClientId);
        reader.ReadValueSafe(out float x);
        reader.ReadValueSafe(out float y);
        reader.ReadValueSafe(out float z);
        reader.ReadValueSafe(out int length);
        if (length <= 0 || length > SamplesPerPacket) return false;

        payload = new byte[length];
        reader.ReadBytesSafe(ref payload, length);
        position = new Vector3(x, y, z);
        return true;
    }

    void PlayVoicePacket(ulong senderClientId, Vector3 position, byte[] payload)
    {
        if (!VoiceEnabled || payload == null || payload.Length == 0) return;
        if (NetworkManager.Singleton != null && senderClientId == NetworkManager.Singleton.LocalClientId) return;
        if (IsClientMuted(senderClientId)) return;

        AudioSource source = GetRemoteSource(senderClientId);
        source.transform.position = position;
        source.maxDistance = MaxDistance;
        source.volume = OutputVolume;

        float[] samples = new float[payload.Length];
        DecodeSamples(payload, samples);
        AudioClip clip = AudioClip.Create($"Voice_{senderClientId}", samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        source.PlayOneShot(clip, OutputVolume);
    }

    AudioSource GetRemoteSource(ulong senderClientId)
    {
        if (remoteSources.TryGetValue(senderClientId, out AudioSource source) && source != null)
            return source;

        var go = new GameObject($"VoiceRemote_{senderClientId}");
        go.transform.SetParent(transform, false);
        source = go.AddComponent<AudioSource>();
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1.5f;
        source.maxDistance = MaxDistance;
        source.playOnAwake = false;
        remoteSources[senderClientId] = source;
        return source;
    }

    static Vector3 GetLocalVoicePosition()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network != null && network.LocalClient != null && network.LocalClient.PlayerObject != null)
            return network.LocalClient.PlayerObject.transform.position;

        Camera camera = Camera.main;
        return camera != null ? camera.transform.position : Vector3.zero;
    }
}
