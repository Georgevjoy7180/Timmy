
import argparse
import json
import math
import statistics
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def load_json(path):
    with open(path, "r", encoding="utf-8-sig") as file:
        return json.load(file)


def save_json(path, data):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "w", encoding="utf-8") as file:
        json.dump(data, file, indent=2)


def number(value):
    """Convert a finite numeric value to float, or return None."""
    try:
        result = float(value)
        return result if math.isfinite(result) else None
    except (TypeError, ValueError):
        return None


def numeric_values(value):
    """Accept either a scalar or a list of numeric samples."""
    if not isinstance(value, list):
        value = [value]

    return [
        result for item in value
        if (result := number(item)) is not None
    ]


def summarize_series(data, key):
    values = numeric_values(data.get(key))
    if not values:
        return None

    return {
        "samples": len(values),
        "min": round(min(values), 3),
        "max": round(max(values), 3),
        "mean": round(statistics.mean(values), 3),
        "median": round(statistics.median(values), 3),
    }


def summarize_performance(data):
    metrics = {}

    # Preserve these names to match the Unity collector's JSON.
    for key in (
        "FPS",
        "FrameTim",
        "TotalMem",
        "GCUsedMemoryBytes",
        "ElapsedSec",
        "SceneLoadDurationSec",
    ):
        result = summarize_series(data, key)
        if result is not None:
            metrics[key] = result

    # Also accept scene-load durations stored in a nested object.
    scene = data.get("SceneLoad")
    if isinstance(scene, dict):
        result = summarize_series(
            {"duration": scene.get("DurationSec")}, "duration"
        )
        if result is not None:
            metrics["SceneLoadDurationSec"] = result

    return metrics


def parse_junit(path):
    root = ET.parse(path).getroot()

    cases = root.findall(".//testcase")
    if not cases:
        cases = root.findall(".//test-case")

    results = {
        "total": 0,
        "passed": 0,
        "failed": 0,
        "errors": 0,
        "skipped": 0,
        "failure_details": [],
    }

    for case in cases:
        results["total"] += 1
        name = case.get("name", "Unnamed test")
        classname = case.get("classname", "")
        label = f"{classname}.{name}" if classname else name

        failure = case.find("failure")
        error = case.find("error")
        skipped = case.find("skipped")
        result_attr = case.get("result")

        if failure is not None:
            results["failed"] += 1
            msg = failure.get("message") or (failure.findtext("message") if failure.find("message") is not None else "") or ""
            results["failure_details"].append({
                "test": label,
                "type": "failure",
                "message": msg,
            })
        elif error is not None:
            results["errors"] += 1
            msg = error.get("message") or (error.findtext("message") if error.find("message") is not None else "") or ""
            results["failure_details"].append({
                "test": label,
                "type": "error",
                "message": msg,
            })
        elif skipped is not None:
            results["skipped"] += 1
        elif result_attr is not None:
            # Handle native NUnit result attributes (Passed, Failed, Skipped, Inconclusive)
            if result_attr == "Passed":
                results["passed"] += 1
            elif result_attr == "Failed":
                results["failed"] += 1
                failure_elem = case.find("failure")
                msg = ""
                if failure_elem is not None:
                    msg = failure_elem.findtext("message") or failure_elem.get("message") or ""
                results["failure_details"].append({
                    "test": label,
                    "type": "failure",
                    "message": msg,
                })
            elif result_attr in ("Skipped", "Ignored"):
                results["skipped"] += 1
            else:
                results["errors"] += 1
        else:
            results["passed"] += 1

    executed = results["total"] - results["skipped"]
    results["pass_rate_percent"] = round(
        100 * results["passed"] / executed, 2
    ) if executed else 0.0

    return results


def metric_value(metrics, key, statistic="mean"):
    metric = metrics.get(key)
    if not isinstance(metric, dict):
        return None
    return number(metric.get(statistic))


def compare_baseline(current, baseline):
    comparisons = {}

    for key, current_stats in current.items():
        if key not in baseline:
            continue

        old_stats = baseline[key]
        if not isinstance(old_stats, dict):
            continue

        old = number(old_stats.get("mean"))
        new = number(current_stats.get("mean"))

        if old is None or new is None:
            continue

        change = new - old
        change_percent = (
            100 * change / abs(old) if old != 0 else None
        )

        comparisons[key] = {
            "baseline_mean": old,
            "current_mean": new,
            "change": round(change, 3),
            "change_percent": (
                round(change_percent, 2)
                if change_percent is not None else None
            ),
        }

    return comparisons


def check_thresholds(metrics, args):
    violations = []

    def check(key, statistic, limit, comparison, label):
        value = metric_value(metrics, key, statistic)

        if value is None:
            return

        violated = (
            value < limit if comparison == "min"
            else value > limit
        )

        if violated:
            violations.append({
                "metric": key,
                "statistic": statistic,
                "actual": value,
                "limit": limit,
                "rule": comparison,
                "message": (
                    f"{label}: {value:.3f}; threshold {limit:.3f}"
                ),
            })

    if args.min_fps is not None:
        check("FPS", "mean", args.min_fps, "min", "Average FPS too low")

    if args.max_frame_ms is not None:
        check(
            "FrameTim", "max", args.max_frame_ms, "max",
            "Maximum frame time too high"
        )

    if args.max_memory_mb is not None:
        # TotalMem is assumed to be reported in bytes.
        check(
            "TotalMem", "max", args.max_memory_mb * 1_000_000,
            "max", "Total memory too high"
        )

    if args.max_gc_mb is not None:
        check(
            "GCUsedMemoryBytes", "max", args.max_gc_mb * 1_000_000,
            "max", "GC-used memory too high"
        )

    if args.max_scene_load_sec is not None:
        check(
            "SceneLoadDurationSec", "max",
            args.max_scene_load_sec, "max",
            "Scene load too slow"
        )

    if args.max_fps_drop_percent is not None and args.baseline and Path(args.baseline).exists():
        comparison = compare_baseline(
            metrics, load_json(args.baseline)
        )
        fps = comparison.get("FPS")
        if fps and fps["change_percent"] is not None:
            drop = -fps["change_percent"]
            if drop > args.max_fps_drop_percent:
                violations.append({
                    "metric": "FPS",
                    "message": (
                        f"FPS regression: {drop:.2f}% drop; "
                        f"allowed {args.max_fps_drop_percent:.2f}%"
                    ),
                })

    return violations


def main():
    parser = argparse.ArgumentParser(
        description="Analyze Unity test and performance reports."
    )
    parser.add_argument("--performance", required=True)
    parser.add_argument("--junit", required=True)
    parser.add_argument("--baseline")
    parser.add_argument("--save-baseline", action="store_true")
    parser.add_argument("--output", default="output/summary.json")

    parser.add_argument("--min-fps", type=float, default=60.0)
    parser.add_argument("--max-frame-ms", type=float, default=33.33)
    parser.add_argument("--max-memory-mb", type=float)
    parser.add_argument("--max-gc-mb", type=float)
    parser.add_argument("--max-scene-load-sec", type=float)
    parser.add_argument("--max-fps-drop-percent", type=float, default=10.0)

    args = parser.parse_args()

    try:
        performance_data = load_json(args.performance)
        metrics = summarize_performance(performance_data)
        tests = parse_junit(args.junit)

        baseline = None
        comparisons = {}

        if args.baseline and Path(args.baseline).exists():
            baseline = load_json(args.baseline)
            comparisons = compare_baseline(metrics, baseline)

        violations = check_thresholds(metrics, args)

        summary = {
            "tests": tests,
            "performance": metrics,
            "baseline_comparison": comparisons,
            "threshold_violations": violations,
            "status": (
                "FAIL" if tests["failed"] or tests["errors"]
                or violations else "PASS"
            ),
        }

        save_json(args.output, summary)

        if args.save_baseline:
            save_json(
                args.baseline or "baseline.json", metrics
            )

        print("\n=== UNITY QA ANALYSIS ===")
        print(f"Status: {summary['status']}")
        print(
            f"Tests: {tests['total']} total | "
            f"{tests['passed']} passed | "
            f"{tests['failed']} failed | "
            f"{tests['errors']} errors | "
            f"{tests['skipped']} skipped"
        )
        print(f"Pass rate: {tests['pass_rate_percent']:.2f}%")

        for key, stats in metrics.items():
            print(
                f"{key}: mean={stats['mean']}, "
                f"min={stats['min']}, max={stats['max']}"
            )

        for key, result in comparisons.items():
            print(
                f"Baseline {key}: "
                f"{result['change_percent']}% change"
            )

        for violation in violations:
            print(f"THRESHOLD VIOLATION: {violation['message']}")

        for failure in tests["failure_details"]:
            print(
                f"TEST {failure['type'].upper()}: "
                f"{failure['test']} - {failure['message']}"
            )

        print(f"Report saved to: {args.output}")

        return 1 if summary["status"] == "FAIL" else 0

    except (OSError, ValueError, ET.ParseError, TypeError) as exc:
        print(f"Analysis error: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
