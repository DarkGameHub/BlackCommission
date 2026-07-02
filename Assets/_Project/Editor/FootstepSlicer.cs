using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cuts two single-step samples out of the long walking take David sourced
/// (<c>Resources/Audio/Sfx/footsteps_walk_raw.mp3</c>, freesound #52640 CC0 — multiple
/// steps across a kitchen floor) and writes them as <c>footstep_a/footstep_b.wav</c>
/// next to it. <see cref="AudioManager"/> then loads those per-step clips instead of
/// the synth footsteps; the whole take can't be used directly because footsteps play
/// one clip per stride.
///
/// Transient detection: RMS over 10 ms windows → the two loudest peaks at least 0.4 s
/// apart, each exported as a 0.32 s window starting 30 ms before its peak with a short
/// fade in/out. Run via <c>Tools ▸ Black Commission ▸ Audio ▸ Slice Footsteps</c>.
/// </summary>
public static class FootstepSlicer
{
    const string RawPath = "Assets/_Project/Resources/Audio/Sfx/footsteps_walk_raw.mp3";
    const string OutDir = "Assets/_Project/Resources/Audio/Sfx";

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

        // mono mix + windowed RMS
        int win = sr / 100; // 10 ms
        int frames = n / win;
        var rms = new float[frames];
        for (int f = 0; f < frames; f++)
        {
            float acc = 0f;
            for (int i = 0; i < win; i++)
            {
                float s = 0f;
                for (int c = 0; c < ch; c++) s += data[(f * win + i) * ch + c];
                s /= ch;
                acc += s * s;
            }
            rms[f] = Mathf.Sqrt(acc / win);
        }

        // two loudest peaks ≥0.4 s apart
        int minGap = (int)(0.4f * 100);
        int p1 = -1, p2 = -1;
        for (int f = 1; f < frames - 1; f++)
            if (p1 < 0 || rms[f] > rms[p1]) p1 = f;
        for (int f = 1; f < frames - 1; f++)
            if (Mathf.Abs(f - p1) >= minGap && (p2 < 0 || rms[f] > rms[p2])) p2 = f;
        if (p1 < 0 || p2 < 0) { Debug.LogError("[FootstepSlicer] could not find two transients"); return; }

        WriteSlice(data, ch, sr, n, p1 * win, "footstep_a");
        WriteSlice(data, ch, sr, n, p2 * win, "footstep_b");
        AssetDatabase.Refresh();
        Debug.Log($"[FootstepSlicer] sliced peaks at {p1 * 0.01f:0.00}s and {p2 * 0.01f:0.00}s → footstep_a/b.wav");
    }

    static void WriteSlice(float[] data, int ch, int sr, int totalSamples, int peakSample, string name)
    {
        int start = Mathf.Max(0, peakSample - (int)(0.03f * sr));
        int len = Mathf.Min((int)(0.32f * sr), totalSamples - start);
        int fade = (int)(0.012f * sr);

        var pcm = new short[len]; // mono out
        for (int i = 0; i < len; i++)
        {
            float s = 0f;
            for (int c = 0; c < ch; c++) s += data[(start + i) * ch + c];
            s /= ch;
            float env = 1f;
            if (i < fade) env = i / (float)fade;
            int tail = len - i;
            if (tail < fade * 4) env *= tail / (float)(fade * 4);
            pcm[i] = (short)Mathf.Clamp(s * env * 32767f, short.MinValue, short.MaxValue);
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
            foreach (short s in pcm) bw.Write(s);
        }
    }
}
