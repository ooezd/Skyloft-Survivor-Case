// Run with Unity MCP eval_file after stopping/loading the diagnostic capture.
// Exports inclusive marker totals per main/render frame. Nested markers overlap: do not add them.
var frames = new System.Collections.Generic.List<object>();
int first = UnityEditorInternal.ProfilerDriver.firstFrameIndex;
int last = UnityEditorInternal.ProfilerDriver.lastFrameIndex;
double origin;
using (var start = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(first, 0)) origin = start.frameStartTimeMs;
for (int frame = first; frame <= last; frame++)
{
    for (int thread = 0; thread < 2; thread++)
    {
        using (var data = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(frame, thread))
        {
            if (!data.valid) continue;
            var totals = new System.Collections.Generic.Dictionary<string, double>();
            for (int sample = 1; sample < data.sampleCount; sample++)
            {
                string name = data.GetSampleName(sample);
                if (string.IsNullOrEmpty(name)) continue;
                double value;
                totals.TryGetValue(name, out value);
                totals[name] = value + data.GetSampleTimeMs(sample);
            }
            frames.Add(new { frame, thread = data.threadName, captureSeconds = (data.frameStartTimeMs - origin) / 1000.0,
                frameMs = data.frameTimeMs, markers = totals });
        }
    }
}
var result = new { first, last, note = "Inclusive marker totals in ms; nested samples overlap. Time relative to first captured frame, not exact gameplay time. Main and render threads are concurrent.", frames };
string path = System.IO.Path.GetFullPath("Builds/Profiler/markers.json");
System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(result));
return new { first, last, rows = frames.Count, path };
