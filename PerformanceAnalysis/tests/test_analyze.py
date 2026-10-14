import os
import sys
import tempfile
import unittest
from pathlib import Path

# Add project root to sys.path so analyze can be imported cleanly
PROJECT_ROOT = Path(__file__).resolve().parent.parent.parent
sys.path.insert(0, str(PROJECT_ROOT))

from PerformanceAnalysis.analyze import (
    check_thresholds,
    compare_baseline,
    load_json,
    number,
    numeric_values,
    parse_junit,
    save_json,
    summarize_performance,
    summarize_series,
)


class DummyArgs:
    """Helper class to mock command-line arguments for check_thresholds."""
    def __init__(
        self,
        min_fps=60.0,
        max_frame_ms=33.33,
        max_memory_mb=None,
        max_gc_mb=None,
        max_scene_load_sec=None,
        max_fps_drop_percent=10.0,
        baseline=None,
    ):
        self.min_fps = min_fps
        self.max_frame_ms = max_frame_ms
        self.max_memory_mb = max_memory_mb
        self.max_gc_mb = max_gc_mb
        self.max_scene_load_sec = max_scene_load_sec
        self.max_fps_drop_percent = max_fps_drop_percent
        self.baseline = baseline


class TestAnalyze(unittest.TestCase):

    # -------------------------------------------------------------
    # 1. Number and Value Sanitization
    # -------------------------------------------------------------
    def test_number_valid_conversions(self):
        self.assertEqual(number(10), 10.0)
        self.assertEqual(number("42.5"), 42.5)
        self.assertEqual(number("-15.2"), -15.2)

    def test_number_invalid_and_infinite_values(self):
        self.assertIsNone(number(None))
        self.assertIsNone(number("not_a_number"))
        self.assertIsNone(number(float("inf")))
        self.assertIsNone(number(float("-inf")))
        self.assertIsNone(number(float("nan")))

    def test_numeric_values_scalar_and_list(self):
        # Scalar handling
        self.assertEqual(numeric_values(100), [100.0])
        self.assertEqual(numeric_values("50.5"), [50.5])
        self.assertEqual(numeric_values("invalid"), [])

        # List with mixed valid/invalid items
        raw = [10, "20.5", None, "abc", 40]
        expected = [10.0, 20.5, 40.0]
        self.assertEqual(numeric_values(raw), expected)

    # -------------------------------------------------------------
    # 2. Statistical Series Summarization
    # -------------------------------------------------------------
    def test_summarize_series_valid_data(self):
        data = {"FPS": [100.0, 200.0, 300.0, 400.0, 500.0]}
        summary = summarize_series(data, "FPS")
        self.assertIsNotNone(summary)
        self.assertEqual(summary["samples"], 5)
        self.assertEqual(summary["min"], 100.0)
        self.assertEqual(summary["max"], 500.0)
        self.assertEqual(summary["mean"], 300.0)
        self.assertEqual(summary["median"], 300.0)

    def test_summarize_series_missing_or_empty_key(self):
        self.assertIsNone(summarize_series({}, "FPS"))
        self.assertIsNone(summarize_series({"FPS": []}, "FPS"))
        self.assertIsNone(summarize_series({"FPS": ["invalid", None]}, "FPS"))

    def test_summarize_performance_extracts_all_metrics(self):
        payload = {
            "FPS": [120, 140],
            "FrameTim": [8.3, 7.1],
            "TotalMem": [3000000000],
            "GCUsedMemoryBytes": [500000000],
            "ElapsedSec": [0.1, 0.2],
            "SceneLoadDurationSec": [0.45],
        }
        metrics = summarize_performance(payload)
        self.assertIn("FPS", metrics)
        self.assertIn("FrameTim", metrics)
        self.assertIn("TotalMem", metrics)
        self.assertIn("GCUsedMemoryBytes", metrics)
        self.assertIn("ElapsedSec", metrics)
        self.assertIn("SceneLoadDurationSec", metrics)
        self.assertEqual(metrics["SceneLoadDurationSec"]["mean"], 0.45)

    def test_summarize_performance_nested_sceneload_object(self):
        payload = {
            "SceneLoad": {"DurationSec": 0.82}
        }
        metrics = summarize_performance(payload)
        self.assertIn("SceneLoadDurationSec", metrics)
        self.assertEqual(metrics["SceneLoadDurationSec"]["mean"], 0.82)

    # -------------------------------------------------------------
    # 3. JUnit and NUnit Test Report Parsing
    # -------------------------------------------------------------
    def test_parse_junit_standard_format(self):
        xml_content = """<?xml version="1.0" encoding="utf-8"?>
        <testsuites>
            <testsuite name="SuiteA">
                <testcase classname="Tests.PlayMode" name="TestA" />
                <testcase classname="Tests.PlayMode" name="TestB">
                    <failure message="Assertion failed" />
                </testcase>
                <testcase classname="Tests.PlayMode" name="TestC">
                    <error message="Crash" />
                </testcase>
                <testcase classname="Tests.PlayMode" name="TestD">
                    <skipped />
                </testcase>
            </testsuite>
        </testsuites>
        """
        with tempfile.NamedTemporaryFile("w", suffix=".xml", delete=False, encoding="utf-8") as f:
            f.write(xml_content)
            temp_path = f.name

        try:
            results = parse_junit(temp_path)
            self.assertEqual(results["total"], 4)
            self.assertEqual(results["passed"], 1)
            self.assertEqual(results["failed"], 1)
            self.assertEqual(results["errors"], 1)
            self.assertEqual(results["skipped"], 1)
            self.assertEqual(results["pass_rate_percent"], 33.33)
            self.assertEqual(len(results["failure_details"]), 2)
        finally:
            os.remove(temp_path)

    def test_parse_nunit_unity_format(self):
        xml_content = """<?xml version="1.0" encoding="utf-8"?>
        <test-run id="2" testcasecount="2" result="Passed">
            <test-suite type="TestSuite" name="PlayModeTests">
                <test-case id="1001" name="TestPassed" classname="PlayMode" result="Passed" />
                <test-case id="1002" name="TestFailed" classname="PlayMode" result="Failed">
                    <failure>
                        <message><![CDATA[Expected True but was False]]></message>
                    </failure>
                </test-case>
            </test-suite>
        </test-run>
        """
        with tempfile.NamedTemporaryFile("w", suffix=".xml", delete=False, encoding="utf-8") as f:
            f.write(xml_content)
            temp_path = f.name

        try:
            results = parse_junit(temp_path)
            self.assertEqual(results["total"], 2)
            self.assertEqual(results["passed"], 1)
            self.assertEqual(results["failed"], 1)
            self.assertEqual(results["pass_rate_percent"], 50.0)
            self.assertEqual(len(results["failure_details"]), 1)
            self.assertIn("Expected True but was False", results["failure_details"][0]["message"])
        finally:
            os.remove(temp_path)

    # -------------------------------------------------------------
    # 4. Baseline Comparisons and Regression Detection
    # -------------------------------------------------------------
    def test_compare_baseline_calculates_difference_and_percentage(self):
        current = {
            "FPS": {"mean": 120.0},
            "FrameTim": {"mean": 8.33},
        }
        baseline = {
            "FPS": {"mean": 100.0},
            "FrameTim": {"mean": 10.0},
        }
        comparison = compare_baseline(current, baseline)
        self.assertEqual(comparison["FPS"]["change"], 20.0)
        self.assertEqual(comparison["FPS"]["change_percent"], 20.0)
        self.assertEqual(comparison["FrameTim"]["change"], -1.67)
        self.assertEqual(comparison["FrameTim"]["change_percent"], -16.7)

    # -------------------------------------------------------------
    # 5. Threshold Violations
    # -------------------------------------------------------------
    def test_check_thresholds_flags_low_fps_and_high_frame_time(self):
        metrics = {
            "FPS": {"mean": 45.0, "max": 60.0, "min": 30.0},
            "FrameTim": {"mean": 22.0, "max": 40.0, "min": 16.0},
        }
        args = DummyArgs(min_fps=60.0, max_frame_ms=33.33)
        violations = check_thresholds(metrics, args)

        # Both FPS (45 < 60) and max FrameTime (40 > 33.33) should violate
        self.assertEqual(len(violations), 2)
        metrics_violated = [v["metric"] for v in violations]
        self.assertIn("FPS", metrics_violated)
        self.assertIn("FrameTim", metrics_violated)

    def test_check_thresholds_passes_when_within_limits(self):
        metrics = {
            "FPS": {"mean": 90.0, "max": 120.0, "min": 75.0},
            "FrameTim": {"mean": 11.1, "max": 15.0, "min": 8.0},
        }
        args = DummyArgs(min_fps=60.0, max_frame_ms=33.33)
        violations = check_thresholds(metrics, args)
        self.assertEqual(len(violations), 0)

    # -------------------------------------------------------------
    # 6. JSON Storage Utilities (load_json, save_json, UTF-8 BOM)
    # -------------------------------------------------------------
    def test_json_roundtrip_and_utf8_bom_support(self):
        data = {"key": "value", "numbers": [1, 2, 3]}
        with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False, encoding="utf-8-sig") as f:
            temp_path = f.name

        try:
            save_json(temp_path, data)
            loaded = load_json(temp_path)
            self.assertEqual(loaded, data)
        finally:
            os.remove(temp_path)


if __name__ == "__main__":
    unittest.main()
