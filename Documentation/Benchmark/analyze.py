"""Inspect raw benchmark frames; optional comparison checks identity and workload.

python Documentation/Benchmark/analyze.py <run-directory> [<reference-run-directory>]
No dependencies beyond Python's standard library. Editor runs are never device evidence.
"""
import csv
import json
import math
import pathlib
import statistics
import sys


def percentile(values, fraction):
    return sorted(values)[max(0, math.ceil(len(values) * fraction) - 1)]


def inspect(directory):
    directory = pathlib.Path(directory)
    report = json.loads((directory / "summary.json").read_text(encoding="utf-8-sig"))
    with (directory / "frames.csv").open(encoding="utf-8-sig", newline="") as handle:
        rows = [{key: float(value) for key, value in row.items()} for row in csv.DictReader(handle)]
    if len(rows) != report["frames"]:
        raise ValueError("CSV frame count disagrees with summary")
    if not rows or any(row["frame_ms"] <= 0 for row in rows):
        raise ValueError("Missing or non-positive frame intervals")
    if any(b["wall_seconds"] <= a["wall_seconds"] or b["game_seconds"] < a["game_seconds"]
           for a, b in zip(rows, rows[1:])):
        raise ValueError("Non-monotonic timestamps")
    measured = [r for r in rows if r["game_seconds"] >= report["warmupSeconds"]]
    if not measured:
        raise ValueError("No samples after warmup")
    if len(measured) != report["measuredFrames"]:
        raise ValueError("Warmup sample count disagrees with summary")
    p95 = percentile([r["frame_ms"] for r in measured], 0.95)
    if abs(p95 - report["p95FrameMs"]) > 0.01:
        raise ValueError("Raw p95 disagrees with summary")
    print(f"\n{directory}\n{report['protocol']} | {report['status']} | "
          f"{'EDITOR - functional check only' if report['editor'] else report['device']}")
    print(f"Revision: {report['revision']} | Fingerprint: {report['fingerprint']}")
    print(f"Frames: {len(rows)}; measured: {len(measured)}; "
          f"mean {report['meanFrameMs']:.2f} ms; p95 {p95:.2f} ms; p99 {report['p99FrameMs']:.2f} ms")
    print(f"Kills: {report['kills']}; spawned: {report['spawns']}; "
          f"time-weighted enemies: {report['meanEnemies']:.2f}; peak: {report['peakEnemies']}")
    print("\nGame seconds | p95 ms | mean enemies (time weighted) | GC bytes/frame (available samples)")
    for start, end in [(15, 30), (30, 60), (60, 90), (90, 120), (120, 150), (150, 180)]:
        window = [r for r in measured if start <= r["game_seconds"] < end]
        if not window:
            continue
        times = [r["frame_ms"] for r in window]
        mean_enemies = sum(r["active_enemies"] * r["frame_ms"] for r in window) / sum(times)
        gc = [r["gc_bytes"] for r in window if r["gc_bytes"] >= 0]
        gc_text = f"{statistics.mean(gc):.1f}" if gc else "unavailable"
        print(f"{start:3}-{end:<3} | {percentile(times, .95):7.2f} | {mean_enemies:8.2f} | {gc_text}")
    return report


def compare(current, reference):
    print("\nComparison eligibility:")
    issues = []
    for field in ["protocol", "seed", "difficulty", "enemyCap", "wavePlan", "device", "os", "unity", "gpu", "graphicsApi", "quality",
                  "resolution", "targetFps", "warmupSeconds", "development"]:
        if current[field] != reference[field]:
            issues.append(f"Different {field}: {reference[field]} -> {current[field]}")
    if current["editor"] or reference["editor"]:
        issues.append("Editor data is not device performance evidence")
    if current["status"] != "completed" or reference["status"] != "completed":
        issues.append("An incomplete/invalid run is present")
    for field in ["spawns", "kills", "meanEnemies"]:
        before, after = reference[field], current[field]
        change = abs(after - before) / max(1, abs(before))
        if change > .05:
            issues.append(f"Workload {field} differs >5% ({before:.2f} -> {after:.2f}); inspect raw windows")
    if issues:
        print("\n".join("- " + issue for issue in issues))
        print("No headline improvement computed. 5% is a review flag, not a determinism guarantee.")
    else:
        for field in ["meanFrameMs", "p95FrameMs", "p99FrameMs"]:
            before, after = reference[field], current[field]
            print(f"{field}: {before:.2f} -> {after:.2f} ms "
                  f"({(before - after) / before * 100:+.1f}% reduction)")
        print("Still check thermal/power state and repeatability across all three runs.")


if __name__ == "__main__":
    if len(sys.argv) not in (2, 3):
        raise SystemExit(__doc__)
    try:
        current = inspect(sys.argv[1])
        if len(sys.argv) == 3:
            compare(current, inspect(sys.argv[2]))
    except (OSError, ValueError, KeyError) as error:
        raise SystemExit(f"Invalid benchmark data: {error}")
