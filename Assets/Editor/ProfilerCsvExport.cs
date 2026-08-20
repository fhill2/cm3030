using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

public static class ProfilerCsvExport
{
    private const int MaxFrames = 500;

    private class Entry
    {
        public string Name;
        public double TotalMs;
        public double SelfMs;
        public double MaxFrameSelfMs;
        public double GcBytes;
        public double Calls;
    }

    [MenuItem("Tools/Profiler/Export Main Thread To CSV")]
    public static void Export()
    {
        int first = ProfilerDriver.firstFrameIndex;
        int last = ProfilerDriver.lastFrameIndex;
        if (first < 0 || last < first)
        {
            Debug.LogWarning("[ProfilerCsvExport] No profiler frames in memory. Record a session or use Profiler window > Load on a saved .data capture, then run this again.");
            return;
        }
        if (last - first + 1 > MaxFrames)
            first = last - MaxFrames + 1;

        var totals = new Dictionary<string, Entry>();
        var frames = new StringBuilder("FrameIndex,MainThreadMs\n");
        int processed = 0;

        for (int f = first; f <= last; f++)
        {
            var view = GetMainThreadView(f);
            if (view == null) continue;

            int root = view.GetRootItemID();
            double frameMs = view.GetItemColumnDataAsDouble(root, HierarchyFrameDataView.columnTotalTime);
            frames.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:0.000}", f, frameMs));

            Walk(view, root, totals);
            processed++;
        }

        if (processed == 0)
        {
            Debug.LogWarning("[ProfilerCsvExport] Found frames but no valid Main Thread view.");
            return;
        }

        var rows = new List<Entry>(totals.Values);
        rows.Sort((a, b) => b.SelfMs.CompareTo(a.SelfMs));

        var sb = new StringBuilder("MarkerName,SelfMsSum,TotalMsSum,MaxSelfMsInOneFrame,GcAllocBytesSum,CallsSum\n");
        foreach (var e in rows)
        {
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:0.000},{2:0.000},{3:0.000},{4:0},{5:0}",
                Escape(e.Name), e.SelfMs, e.TotalMs, e.MaxFrameSelfMs, e.GcBytes, e.Calls));
        }

        string dir = Directory.GetParent(Application.dataPath).FullName;
        string stamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string summaryPath = Path.Combine(dir, "profiler-summary-" + stamp + ".csv");
        string framesPath = Path.Combine(dir, "profiler-frames-" + stamp + ".csv");
        File.WriteAllText(summaryPath, sb.ToString());
        File.WriteAllText(framesPath, frames.ToString());

        Debug.Log("[ProfilerCsvExport] Exported " + processed + " frames (range " + first + "-" + last + ").\n" +
                  "Markers sorted by self time: " + summaryPath + "\n" +
                  "Per-frame main thread ms: " + framesPath);
    }

    private static HierarchyFrameDataView GetMainThreadView(int frame)
    {
        for (int t = 0; t < 8; t++)
        {
            var v = ProfilerDriver.GetHierarchyFrameDataView(frame, t, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnDontSort, false);
            if (!v.valid) break;
            if (v.threadName == "Main Thread") return v;
        }
        return null;
    }

    private static void Walk(HierarchyFrameDataView view, int id, Dictionary<string, Entry> acc)
    {
        string name = view.GetItemName(id);
        double self = view.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnSelfTime);
        double total = view.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnTotalTime);
        double gc = view.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnGcMemory);
        double calls = view.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnCalls);

        if (!acc.TryGetValue(name, out var e))
        {
            e = new Entry { Name = name };
            acc[name] = e;
        }
        e.TotalMs += total;
        e.SelfMs += self;
        e.GcBytes += gc;
        e.Calls += calls;
        if (self > e.MaxFrameSelfMs) e.MaxFrameSelfMs = self;

        var children = new List<int>(16);
        view.GetItemChildren(id, children);
        for (int i = 0; i < children.Count; i++)
            Walk(view, children[i], acc);
    }

    private static string Escape(string s)
    {
        return s.IndexOfAny(new[] { ',', '"', '\n' }) < 0 ? s : "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
