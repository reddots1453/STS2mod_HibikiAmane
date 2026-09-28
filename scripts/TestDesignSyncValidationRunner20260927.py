"""Runner contract tests use fake child results; the real runner is executed separately."""
import contextlib
import io
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

import ValidateDesignSync20260927 as runner


class ValidationRunnerContracts(unittest.TestCase):
    snapshot = {"sha256": "same", "files": {}, "errors": []}
    head = {"head": "abc", "error": None}

    def summary(self, results, after=None, head_after=None):
        return runner.summarize(results, self.snapshot, after or self.snapshot, self.head, head_after or self.head)

    def test_fixed_plan_no_deployment_game_launch_or_shared_report_writes(self):
        suites = runner.plan()
        self.assertEqual(len(suites), 14)
        self.assertEqual(len({s.id for s in suites}), 14)
        projects = {s.argv[s.argv.index("--project") + 1] for s in suites if "--project" in s.argv}
        self.assertEqual(projects, {"tests/DesignSyncContracts", "tests/LayeredSaveContracts",
                                    "tests/HumilityEffectContracts", "tests/GenerosityOfferingContracts"})
        builds = [s for s in suites if s.kind == "compile_only"]
        self.assertEqual([s.argv[s.argv.index("-c") + 1] for s in builds], ["Release", "Debug"])
        for suite in builds:
            self.assertIn("-p:DeployMod=false", suite.argv)
            self.assertIn("--no-restore", suite.argv)
        self.assertIn("--no-write", suites[-1].argv)
        self.assertTrue(all(not any("sts2.exe" in arg.lower() or "confirm" == arg for arg in s.argv) for s in suites))

    def test_all_offline_pass_never_means_full_goal_passed(self):
        result = self.summary([{"status": "passed"} for _ in runner.plan()])
        self.assertEqual(result["offlineStatus"], "passed")
        self.assertEqual(result["exitCode"], 2)
        self.assertFalse(result["goalCompleted"])
        self.assertEqual(result["gameRuntimeStatus"], "not_run")
        self.assertEqual(set(result["runtimeGroups"].values()), {"not_run"})

    def test_selection_deduplicates_preserves_plan_order_and_rejects_unknown(self):
        suites = runner.plan()
        self.assertEqual(runner.select_suites(suites, None), suites)
        self.assertEqual([s.id for s in runner.select_suites(suites,
            ["generosity_offering", "humility_effects", "humility_effects"])],
            ["humility_effects", "generosity_offering"])
        with self.assertRaisesRegex(ValueError, "typo"):
            runner.select_suites(suites, ["humility_effects", "typo"])

    def test_selected_success_is_not_full_plan_success(self):
        suites = runner.plan()
        selected = runner.select_suites(suites, ["humility_effects"])
        calls = []
        def execute(suite, root, log, timeout):
            calls.append(suite.id)
            return {"id": suite.id, "status": "passed", "exitCode": 0}
        with tempfile.TemporaryDirectory() as tmp, contextlib.redirect_stdout(io.StringIO()):
            directory = Path(tmp)
            report, path = runner.run_all(directory, selected, directory, executor=execute,
                snapshot=lambda _: self.snapshot, get_head=lambda _: self.head, available_suites=suites)
            self.assertEqual(calls, ["humility_effects"])
            summary = report["summary"]
            self.assertEqual(summary["offlineStatus"], "passed_selected")
            self.assertEqual(summary["fullPlanOfflineStatus"], "not_run")
            self.assertEqual(summary["exitCode"], 2)
            self.assertFalse(summary["goalCompleted"])
            self.assertEqual(summary["executionScope"]["mode"], "selected")
            self.assertEqual(summary["executionScope"]["omittedSuites"], [s.id for s in suites if s.id != "humility_effects"])
            self.assertEqual(json.loads((directory / "progress.json").read_text())["executionScope"], summary["executionScope"])
            self.assertEqual(json.loads(path.read_text())["schemaVersion"], 2)

    def test_cli_list_does_not_run_and_unknown_does_not_create_report(self):
        with patch.object(runner, "run_all") as run, patch.object(runner, "unique_directory") as directory:
            with patch("sys.argv", ["validate", "--suite", "humility_effects", "--list"]), contextlib.redirect_stdout(io.StringIO()) as output:
                self.assertEqual(runner.main(), 0)
            listing = json.loads(output.getvalue())
            self.assertEqual(listing["status"], "not_executed")
            self.assertEqual([s["id"] for s in listing["suites"]], ["humility_effects"])
            with patch("sys.argv", ["validate", "--suite", "typo"]), contextlib.redirect_stderr(io.StringIO()):
                with self.assertRaises(SystemExit) as error:
                    runner.main()
                self.assertEqual(error.exception.code, 2)
            run.assert_not_called()
            directory.assert_not_called()

    def test_failure_missing_tool_timeout_and_interrupt_are_never_passed(self):
        for status in ("failed", "error", "timeout", "interrupted", "not_run"):
            with self.subTest(status=status):
                result = self.summary([{"status": status}])
                self.assertNotEqual(result["offlineStatus"], "passed")
                self.assertNotEqual(result["exitCode"], 0)
        self.assertEqual(self.summary([{"status": "interrupted"}])["exitCode"], 130)
        self.assertEqual(self.summary([])["offlineStatus"], "incomplete")

    def test_source_or_head_drift_and_missing_metadata_fail(self):
        for after, head in ((dict(self.snapshot, sha256="changed"), self.head),
                            (self.snapshot, {"head": "def"}),
                            (dict(self.snapshot, errors=["unreadable source"]), self.head),
                            (self.snapshot, {"head": None})):
            result = self.summary([{"status": "passed"}], after, head)
            self.assertEqual(result["offlineStatus"], "failed")
            self.assertEqual(result["exitCode"], 1)

    def test_execute_captures_native_nonzero_missing_command_and_timeout(self):
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            suite = runner.Suite("probe", "static", ("missing-tool",))
            for outcome, expected in ((subprocess.CompletedProcess([], 7), "failed"),
                                      (FileNotFoundError("missing"), "error"),
                                      (subprocess.TimeoutExpired("probe", 1), "timeout"),
                                      (KeyboardInterrupt(), "interrupted")):
                with patch.object(runner.subprocess, "run") as run:
                    if isinstance(outcome, BaseException):
                        run.side_effect = outcome
                    else:
                        run.return_value = outcome
                    result = runner.execute(suite, directory, directory / "probe.log", 1)
                    self.assertEqual(result["status"], expected)
                    self.assertEqual(run.call_args.kwargs["cwd"], directory)
                    self.assertFalse(run.call_args.kwargs["check"])

    def test_failed_gate_does_not_hide_later_results_and_json_roundtrips(self):
        calls = []
        def execute(suite, root, log, timeout):
            calls.append(suite.id)
            return {"id": suite.id, "status": "failed" if suite.id == "first" else "passed", "exitCode": 9 if suite.id == "first" else 0}
        with tempfile.TemporaryDirectory() as tmp, contextlib.redirect_stdout(io.StringIO()):
            directory = Path(tmp)
            report, path = runner.run_all(directory, [runner.Suite("first", "static", ()), runner.Suite("last", "static", ())],
                                          directory, executor=execute, snapshot=lambda _: self.snapshot, get_head=lambda _: self.head)
            self.assertEqual(calls, ["first", "last"])
            self.assertEqual(report, json.loads(path.read_text(encoding="utf-8")))
            self.assertEqual(report["summary"]["passedSuites"], 1)
            self.assertEqual(report["summary"]["exitCode"], 1)
            self.assertEqual(len(json.loads((directory / "progress.json").read_text())["results"]), 2)

    def test_interrupt_marks_remaining_unrun_and_preserves_report(self):
        calls = []
        def execute(suite, root, log, timeout):
            calls.append(suite.id)
            return {"id": suite.id, "status": "interrupted", "exitCode": None}
        with tempfile.TemporaryDirectory() as tmp, contextlib.redirect_stdout(io.StringIO()):
            directory = Path(tmp)
            report, _ = runner.run_all(directory, [runner.Suite("first", "static", ()), runner.Suite("last", "static", ())],
                                       directory, executor=execute, snapshot=lambda _: self.snapshot, get_head=lambda _: self.head)
            self.assertEqual(calls, ["first"])
            self.assertEqual(report["results"][1]["status"], "not_run")
            self.assertEqual(report["summary"]["exitCode"], 130)

    def test_fingerprint_includes_untracked_source_but_not_generated_outputs(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            for name in ("DesignDoc.md", "PLAN_FRAMEWORK.md", "docs/DESIGN_TRACEABILITY.md", "MaidenSuccubus.csproj", "src/New.cs", "tests/obj/generated.cs"):
                path = root / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("same", encoding="utf-8")
            before = runner.fingerprint(root)
            self.assertFalse(before["errors"])
            (root / "tests/obj/generated.cs").write_text("changed")
            self.assertEqual(before["sha256"], runner.fingerprint(root)["sha256"])
            (root / "src/New.cs").write_text("changed")
            self.assertNotEqual(before["sha256"], runner.fingerprint(root)["sha256"])

    def test_unique_output_does_not_replace_prior_artifacts(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            first, second = runner.unique_directory(root), runner.unique_directory(root)
            self.assertNotEqual(first, second)
            self.assertTrue(first.is_relative_to(root / "obj" / "design-sync-validation"))
            self.assertTrue(first.is_dir() and second.is_dir())


if __name__ == "__main__":
    unittest.main()
