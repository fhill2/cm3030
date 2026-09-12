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

        Dictionary<string, Entry> totals = new Dictionary<string, Entry>();
        StringBuilder frames = new StringBuilder("FrameIndex,MainThreadMs\n");
        int processed = 0;

        for (int frame = first; frame <= last; frame++)
        {
            HierarchyFrameDataView view = GetMainThreadView(frame);
            if (view == null) continue;

            int root = view.GetRootItemID();
            double frameMs = view.GetItemColumnDataAsDouble(root, HierarchyFrameDataView.columnTotalTime);
            frames.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:0.000}", frame, frameMs));

            Walk(view, root, totals);
            processed++;
        }

        if (processed == 0)
        {
            Debug.LogWarning("[ProfilerCsvExport] Found frames but no valid Main Thread view.");
            return;
        }

        List<Entry> rows = new List<Entry>(totals.Values);
        rows.Sort((a, b) => b.SelfMs.CompareTo(a.SelfMs));

        StringBuilder summary = new StringBuilder("MarkerName,SelfMsSum,TotalMsSum,MaxSelfMsInOneFrame,GcAllocBytesSum,CallsSum\n");
        foreach (Entry entry in rows)
        {
            summary.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:0.000},{2:0.000},{3:0.000},{4:0},{5:0}",
                Escape(entry.Name), entry.SelfMs, entry.TotalMs, entry.MaxFrameSelfMs, entry.GcBytes, entry.Calls));
        }

        string outputFolder = Directory.GetParent(Application.dataPath).FullName;
        string stamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string summaryPath = Path.Combine(outputFolder, "profiler-summary-" + stamp + ".csv");
        string framesPath = Path.Combine(outputFolder, "profiler-frames-" + stamp + ".csv");
        File.WriteAllText(summaryPath, summary.ToString());
        File.WriteAllText(framesPath, frames.ToString());

        Debug.Log("[ProfilerCsvExport] Exported " + processed + " frames (range " + first + "-" + last + ").\n" +
                  "Markers sorted by self time: " + summaryPath + "\n" +
                  "Per-frame main thread ms: " + framesPath);
    }

    // Thread index isn't fixed, so this scans until it finds the main one.
    private static HierarchyFrameDataView GetMainThreadView(int frame)
    {
        for (int threadIndex = 0; threadIndex < 8; threadIndex++)
        {
            HierarchyFrameDataView view = ProfilerDriver.GetHierarchyFrameDataView(
                frame, threadIndex, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                HierarchyFrameDataView.columnDontSort, false);
            if (!view.valid) break;
            if (view.threadName == "Main Thread") return view;
        }
        return null;
    }

    // Walks the whole marker tree, summing each marker's times by name across
    // every frame.
    private static void Walk(HierarchyFrameDataView view, int id, Dictionary<string, Entry> totals)
    {
        string name = view.GetItemName(id);
        double self = view.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnSelfTime);
        double total = view.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnTotalTime);
        double gcBytes = view.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnGcMemory);
        double calls = view.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnCalls);

        if (!totals.TryGetValue(name, out Entry entry))
        {
            entry = new Entry { Name = name };
            totals[name] = entry;
        }
        entry.TotalMs += total;
        entry.SelfMs += self;
        entry.GcBytes += gcBytes;
        entry.Calls += calls;
        if (self > entry.MaxFrameSelfMs) entry.MaxFrameSelfMs = self;

        List<int> children = new List<int>(16);
        view.GetItemChildren(id, children);
        for (int i = 0; i < children.Count; i++)
            Walk(view, children[i], totals);
    }

    // Marker names can contain commas, which would break the CSV columns.
    private static string Escape(string value)
    {
        return value.IndexOfAny(new[] { ',', '"', '\n' }) < 0
            ? value
            : "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}