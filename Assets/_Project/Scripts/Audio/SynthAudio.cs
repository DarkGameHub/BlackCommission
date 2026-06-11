using UnityEngine;

public static class SynthAudio
{
    const int SampleRate = 22050;

    public static AudioClip Tone(string name, float freq, float duration, float volume = 0.3f, WaveShape shape = WaveShape.Square)
    {
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float env = Envelope(t, duration);
            data[i] = Wave(shape, freq, t) * volume * env;
        }
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip Noise(string name, float duration, float volume = 0.15f)
    {
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        uint seed = 42;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            seed = seed * 1103515245 + 12345;
            float noise = ((seed >> 16) & 0x7FFF) / (float)0x7FFF * 2f - 1f;
            data[i] = noise * volume * Envelope(t, duration);
        }
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip Footstep(string name, float pitch = 1f)
    {
        float duration = 0.12f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        uint seed = (uint)(pitch * 10000);
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            seed = seed * 1103515245 + 12345;
            float noise = ((seed >> 16) & 0x7FFF) / (float)0x7FFF * 2f - 1f;
            float thump = Mathf.Sin(2f * Mathf.PI * 60f * pitch * t) * Mathf.Exp(-t * 35f);
            data[i] = (noise * 0.08f + thump * 0.25f) * Mathf.Exp(-t * 18f);
        }
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip ComputerBeep(string name)
    {
        float duration = 0.08f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float beep = Wave(WaveShape.Square, 880f, t) * 0.12f;
            data[i] = beep * Mathf.Exp(-t * 25f);
        }
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip ComputerBoot(string name)
    {
        float duration = 0.6f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float sweep = 200f + 600f * t / duration;
            float tone = Wave(WaveShape.Square, sweep, t) * 0.08f;
            float click = t < 0.02f ? Wave(WaveShape.Square, 1200f, t) * 0.15f : 0f;
            data[i] = (tone + click) * Envelope(t, duration);
        }
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip EngineIdle(string name, float duration = 3f)
    {
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        uint seed = 77;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            seed = seed * 1103515245 + 12345;
            float noise = ((seed >> 16) & 0x7FFF) / (float)0x7FFF * 2f - 1f;
            float rumble = Mathf.Sin(2f * Mathf.PI * 28f * t) * 0.18f;
            float chug = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.08f;
            float flutter = Mathf.Sin(2f * Mathf.PI * 3.2f * t) * 0.3f + 0.7f;
            data[i] = (rumble + chug + noise * 0.04f) * flutter;
        }
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip EngineStart(string name)
    {
        float duration = 1.8f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        uint seed = 99;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            seed = seed * 1103515245 + 12345;
            float noise = ((seed >> 16) & 0x7FFF) / (float)0x7FFF * 2f - 1f;
            float cranks = t < 0.8f ? Mathf.Sin(2f * Mathf.PI * 12f * t) * 0.2f * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 4f * t)) : 0f;
            float catch_ = t > 0.7f ? Mathf.Sin(2f * Mathf.PI * 35f * t) * 0.15f * Mathf.Clamp01((t - 0.7f) * 3f) : 0f;
            float rumble = t > 1.0f ? Mathf.Sin(2f * Mathf.PI * 28f * t) * 0.18f * Mathf.Clamp01((t - 1.0f) * 2f) : 0f;
            data[i] = (cranks + catch_ + rumble + noise * 0.03f) * 0.8f;
        }
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip DoorCreak(string name)
    {
        float duration = 0.7f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float freq = 180f + Mathf.Sin(t * 8f) * 60f;
            float creak = Wave(WaveShape.Saw, freq, t) * 0.1f;
            float hinge = Mathf.Sin(2f * Mathf.PI * 320f * t) * 0.04f * Mathf.Sin(t * 12f);
            data[i] = (creak + hinge) * Envelope(t, duration);
        }
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip Pickup(string name)
    {
        float duration = 0.15f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float freq = 440f + 660f * t / duration;
            data[i] = Wave(WaveShape.Sine, freq, t) * 0.18f * Mathf.Exp(-t * 12f);
        }
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip MonsterGrowl(string name)
    {
        float duration = 1.2f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        uint seed = 333;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            seed = seed * 1103515245 + 12345;
            float noise = ((seed >> 16) & 0x7FFF) / (float)0x7FFF * 2f - 1f;
            float growl = Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.2f;
            float rumble = Mathf.Sin(2f * Mathf.PI * 72f * t + Mathf.Sin(t * 6f) * 2f) * 0.12f;
            data[i] = (growl + rumble + noise * 0.06f) * Envelope(t, duration) * 0.7f;
        }
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip MonsterChase(string name)
    {
        float duration = 0.4f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float screech = Wave(WaveShape.Saw, 280f + Mathf.Sin(t * 18f) * 120f, t) * 0.12f;
            float pulse = Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.15f;
            data[i] = (screech + pulse) * Envelope(t, duration);
        }
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip FluorescentHum(string name, float duration = 5f)
    {
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float hum = Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.02f;
            float buzz = Mathf.Sin(2f * Mathf.PI * 200f * t) * 0.008f;
            float flicker = 0.85f + Mathf.Sin(t * 0.7f) * 0.15f;
            data[i] = (hum + buzz) * flicker;
        }
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip WindAmbient(string name, float duration = 8f)
    {
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        uint seed = 55555;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            seed = seed * 1103515245 + 12345;
            float noise = ((seed >> 16) & 0x7FFF) / (float)0x7FFF * 2f - 1f;
            float swell = Mathf.Sin(t * 0.3f) * 0.4f + 0.6f;
            float gusts = Mathf.Sin(t * 1.2f) * 0.2f + 0.8f;
            data[i] = noise * 0.03f * swell * gusts;
        }
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip StunSpray(string name)
    {
        float duration = 0.35f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        uint seed = 8888;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            seed = seed * 1103515245 + 12345;
            float noise = ((seed >> 16) & 0x7FFF) / (float)0x7FFF * 2f - 1f;
            float hiss = noise * 0.25f * Mathf.Exp(-t * 5f);
            float pop = t < 0.03f ? Mathf.Sin(2f * Mathf.PI * 600f * t) * 0.3f : 0f;
            data[i] = hiss + pop;
        }
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip FlashlightClick(string name)
    {
        float duration = 0.05f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            data[i] = Mathf.Sin(2f * Mathf.PI * 2200f * t) * 0.2f * Mathf.Exp(-t * 80f);
        }
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Short electrical fizz — played repeatedly while the breaker hold charges.</summary>
    public static AudioClip BreakerCrackle(string name)
    {
        float duration = 0.16f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        uint seed = 4242;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            seed = seed * 1103515245 + 12345;
            float noise = ((seed >> 16) & 0x7FFF) / (float)0x7FFF * 2f - 1f;
            // Sparse spark bursts gated by a fast square + 50Hz mains undertone.
            float gate = Mathf.PerlinNoise(t * 90f, 0.3f) > 0.55f ? 1f : 0.15f;
            float mains = Mathf.Sin(2f * Mathf.PI * 50f * t) * 0.05f;
            data[i] = (noise * 0.22f * gate + mains) * Envelope(t, duration);
        }
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Relay thunk + hum swelling on — the building taking power again.</summary>
    public static AudioClip PowerRestore(string name)
    {
        float duration = 1.4f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        uint seed = 616;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            seed = seed * 1103515245 + 12345;
            float noise = ((seed >> 16) & 0x7FFF) / (float)0x7FFF * 2f - 1f;
            float thunk = t < 0.09f ? Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 30f) * 0.9f : 0f;
            float humOn = Mathf.Clamp01((t - 0.15f) * 1.4f);
            float hum = (Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.10f
                       + Mathf.Sin(2f * Mathf.PI * 200f * t) * 0.04f) * humOn;
            data[i] = (thunk + hum + noise * 0.015f * humOn) * Envelope(t, duration);
        }
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Sheet-metal slam with rattle decay — the debt shutters dropping open.</summary>
    public static AudioClip ShutterSlam(string name)
    {
        float duration = 0.75f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        uint seed = 2718;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            seed = seed * 1103515245 + 12345;
            float noise = ((seed >> 16) & 0x7FFF) / (float)0x7FFF * 2f - 1f;
            float slam = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 22f) * 0.8f;
            // Metallic ring pair + rattling tail.
            float ring = (Mathf.Sin(2f * Mathf.PI * 410f * t) + Mathf.Sin(2f * Mathf.PI * 633f * t))
                * 0.10f * Mathf.Exp(-t * 9f);
            float rattle = noise * 0.12f * Mathf.Exp(-t * 6f) * (Mathf.Sin(2f * Mathf.PI * 13f * t) * 0.5f + 0.5f);
            data[i] = (slam + ring + rattle) * Envelope(t, duration);
        }
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Strain-and-thump of hoisting something heavy onto your shoulder.</summary>
    public static AudioClip HeavyHoist(string name)
    {
        float duration = 0.4f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        uint seed = 9001;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            seed = seed * 1103515245 + 12345;
            float noise = ((seed >> 16) & 0x7FFF) / (float)0x7FFF * 2f - 1f;
            float scrape = noise * 0.10f * Mathf.Clamp01(t * 12f) * Mathf.Exp(-t * 7f);
            float thump = t > 0.18f ? Mathf.Sin(2f * Mathf.PI * 65f * (t - 0.18f)) * Mathf.Exp(-(t - 0.18f) * 28f) * 0.55f : 0f;
            data[i] = (scrape + thump) * Envelope(t, duration);
        }
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Dull heavy thud with a worrying glass ring — the eco column landing hard.</summary>
    public static AudioClip GlassThud(string name)
    {
        float duration = 0.6f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float thud = Mathf.Sin(2f * Mathf.PI * 50f * t) * Mathf.Exp(-t * 26f) * 0.85f;
            // High glass partials, slightly detuned so it rings "sealed jar", not "bell".
            float glass = (Mathf.Sin(2f * Mathf.PI * 1180f * t) * 0.05f
                         + Mathf.Sin(2f * Mathf.PI * 1741f * t) * 0.035f) * Mathf.Exp(-t * 11f);
            data[i] = (thud + glass) * Envelope(t, duration);
        }
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Mechanical lever clank: spring creak into a latched clunk.</summary>
    public static AudioClip LeverClank(string name)
    {
        float duration = 0.34f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float creak = Wave(WaveShape.Saw, 240f + t * 320f, t) * 0.07f * Mathf.Clamp01(1f - t * 4f);
            float clunk = t > 0.16f ? Mathf.Sin(2f * Mathf.PI * 95f * (t - 0.16f)) * Mathf.Exp(-(t - 0.16f) * 34f) * 0.7f : 0f;
            float click = t > 0.16f && t < 0.18f ? Wave(WaveShape.Square, 900f, t) * 0.12f : 0f;
            data[i] = (creak + clunk + click) * Envelope(t, duration);
        }
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Rubber stamp coming down on paper — the settlement getting official.</summary>
    public static AudioClip StampThunk(string name)
    {
        float duration = 0.22f;
        int samples = Mathf.CeilToInt(SampleRate * duration);
        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        float[] data = new float[samples];
        uint seed = 1097;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            seed = seed * 1103515245 + 12345;
            float noise = ((seed >> 16) & 0x7FFF) / (float)0x7FFF * 2f - 1f;
            float thunk = Mathf.Sin(2f * Mathf.PI * 120f * t) * Mathf.Exp(-t * 45f) * 0.7f;
            float paper = noise * 0.06f * Mathf.Exp(-t * 30f);
            data[i] = (thunk + paper) * Envelope(t, duration);
        }
        clip.SetData(data, 0);
        return clip;
    }

    public enum WaveShape { Sine, Square, Saw, Triangle }

    static float Wave(WaveShape shape, float freq, float t)
    {
        float phase = (freq * t) % 1f;
        switch (shape)
        {
            case WaveShape.Square: return phase < 0.5f ? 1f : -1f;
            case WaveShape.Saw: return 2f * phase - 1f;
            case WaveShape.Triangle: return phase < 0.5f ? 4f * phase - 1f : 3f - 4f * phase;
            default: return Mathf.Sin(2f * Mathf.PI * freq * t);
        }
    }

    static float Envelope(float t, float duration)
    {
        float attack = Mathf.Clamp01(t / 0.01f);
        float release = Mathf.Clamp01((duration - t) / 0.05f);
        return attack * release;
    }
}
