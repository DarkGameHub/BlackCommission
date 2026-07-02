using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cuts single-step samples out of the long walking take David sourced
/// (<c>Resources/Audio/Sfx/footsteps_walk_raw.mp3</c>, freesound #52640 CC0 — many steps
/// across a kitchen floor) and writes them as <c>footstep_a..d.wav</c> next to it.
/// <see cref="AudioManager"/> loads whichever of the four exist.
///
/// v2 (David: "脚步声很奇怪"): the first pass grabbed the two loudest windows, which came
/// with room rumble and bleed from neighbouring steps. Now: transient candidates are the
/// top RMS peaks ≥0.35 s apart, scored by how QUIET their 150 ms lead-in is (clean attack),
/// best four win; each slice is 0.26 s, high-passed (~150 Hz one-pole) to cut the room
/// rumble, peak-normalized to 0.7 and faded. Run via
/// <c>Tools ▸ Black Commission ▸ Audio ▸ Slice Footsteps</c>.
/// </summary>
public static class FootstepSlicer
{
    const string RawPath = "Assets/_Project/Resources/Audio/Sfx/footsteps_walk_raw.mp3";
    const string OutDir = "Assets/_Project/Resources/Audio/Sfx";
    static readonly string[] Names = { "footstep_a", "footstep_b", "footstep_c", "footstep_d" };

    [MenuItem("Tools/Black Commission/Audio/Slice Footsteps")]
    public static void Slice()
    {
        var importer = (AudioImporter)AssetImporter.GetAtPath(RawPath);
        if (importer == null) { Debug.LogError("[FootstepSlicer] raw clip not found"); return; }
        var settings = importer.defaultSampleSettings;
        if (settings.loadType != AudioClipLoadType.DecompressOnLoad)
        {
            settings.loadType = AudioClipLoadType.DecompressOnLoad; // GetData needs raw PCM
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(RawPath);
        int ch = clip.channels, sr = clip.frequency, n = clip.samples;
        var data = new float[n * ch];
        clip.GetData(data, 0);

        // mono mix
        var mono = new float[n];
        for (int i = 0; i < n; i++)
        {
            float acc = 0f;
            for (int c = 0; c < ch; c++) acc += data[i * ch + c];
            mono[i] = acc / ch;
        }

        // windowed RMS (10 ms)
        int win = sr / 100;
        int frames = n / win;
        var rms = new float[frames];
        for (int f = 0; f < frames; f++)
        {
            float acc = 0f;
            for (int i = 0; i < win; i++) { float v = mono[f * win + i]; acc += v * v; }
            rms[f] = Mathf.Sqrt(acc / win);
        }

        // candidate transients: every local RMS maximum, ranked by loudness, then greedily
        // deduped to ≥0.35 s spacing (threshold-based hunts starve on takes where one stomp
        // dominates — both v2 attempts found <2). Keeps up to 8 for the quality sort below.
        var maxima = new List<int>();
        for (int f = 2; f < frames - 2; f++)
            if (rms[f] >= rms[f - 1] && rms[f] >= rms[f + 1] && rms[f] > 0f) maxima.Add(f);
        maxima.Sort((a, b) => rms[b].CompareTo(rms[a]));
        var candidates = new List<int>();
        const int minGap = 35; // 0.35 s in 10 ms frames
        foreach (int f in maxima)
        {
            bool clash = false;
            foreach (int kept in candidates) if (Mathf.Abs(kept - f) < minGap) { clash = true; break; }
            if (!clash) candidates.Add(f);
            if (candidates.Count >= 8) break;
        }
        if (candidates.Count < 2) { Debug.LogError("[FootstepSlicer] too few transients found"); return; }

        // score: prefer QUIET 150 ms lead-in (clean attack, no bleed from the previous step)
        candidates.Sort((a, b) => LeadNoise(rms, a).CompareTo(LeadNoise(rms, b)));
        int take = Mathf.Min(Names.Length, candidates.Count);

        for (int k = 0; k < take; k++)
            WriteSlice(mono, sr, n, candidates[k] * win, Names[k]);
        for (int k = take; k < Names.Length; k++)
        {
            string stale = $"{OutDir}/{Names[k]}.wav";
            if (File.Exists(stale)) AssetDatabase.DeleteAsset(stale);
        }

        AssetDatabase.Refresh();
        Debug.Log($"[FootstepSlicer] v2: {candidates.Count} transients, wrote {take} clean slices " +
                  $"at {string.Join(", ", candidates.GetRange(0, take).ConvertAll(f => (f * 0.01f).ToString("0.00")))}s.");
    }

    static float LeadNoise(float[] rms, int frame)
    {
        float acc = 0f; int cnt = 0;
        for (int f = Mathf.Max(0, frame - 15); f < frame - 2; f++) { acc += rms[f]; cnt++; }
        return cnt > 0 ? acc / cnt : float.MaxValue;
    }

    static void WriteSlice(float[] mono, int sr, int totalSamples, int peakSample, string name)
    {
        int start = Mathf.Max(0, peakSample - (int)(0.03f * sr));
        int len = Mathf.Min((int)(0.26f * sr), totalSamples - start);
        int fade = (int)(0.010f * sr);

        // one-pole high-pass (~150 Hz) kills the room rumble that read as "怪"
        var buf = new float[len];
        float rc = 1f / (2f * Mathf.PI * 150f), dt = 1f / sr, a = rc / (rc + dt);
        float prevX = mono[start], prevY = 0f;
        for (int i = 0; i < len; i++)
        {
            float x = mono[start + i];
            float y = a * (prevY + x - prevX);
            prevX = x; prevY = y;
            buf[i] = y;
        }

        // peak normalize to 0.7, then fades
        float peak = 1e-5f;
        for (int i = 0; i < len; i++) peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
        float gain = 0.7f / peak;
        var pcm = new short[len];
        for (int i = 0; i < len; i++)
        {
            float env = 1f;
            if (i < fade) env = i / (float)fade;
            int tail = len - i;
            if (tail < fade * 5) env *= tail / (float)(fade * 5);
            pcm[i] = (short)Mathf.Clamp(buf[i] * gain * env * 32767f, short.MinValue, short.MaxValue);
        }

        string path = $"{OutDir}/{name}.wav";
        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            int dataBytes = pcm.Length * 2;
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataBytes);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            bw.Write(16); bw.Write((short)1); bw.Write((short)1);
            bw.Write(sr); bw.Write(sr * 2); bw.Write((short)2); bw.Write((short)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataBytes);
            foreach (short v in pcm) bw.Write(v);
        }
    }
}
