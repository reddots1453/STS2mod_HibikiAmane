"""Fixed no-deploy DS27 validation plan with honest, versioned offline evidence.

No game launch, live commands, deployment, source formatting or old-report ingestion.
Use repeated --suite IDs for a scoped run; omitted suites are explicitly unverified.
Exit 1: failed/inconsistent offline evidence; 2: offline passed, game verification
still missing; 130: interrupted. --list only prints the plan and returns 0.
"""
from __future__ import annotations

import argparse
from dataclasses import dataclass
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import time
import uuid

ROOT = Path(__file__).resolve().parents[1]
SCHEMA_VERSION = 2


@dataclass(frozen=True)
class Suite:
    id: str
    kind: str
    argv: tuple[str, ...]


def plan() -> list[Suite]:
    powershell = shutil.which("pwsh") or shutil.which("powershell") or "pwsh"
    suites = [
        Suite("design_static", "static", (sys.executable, "-m", "unittest", "discover", "-s", "scripts", "-p", "Test*20260927.py")),
        Suite("audit_self_tests", "static", (sys.executable, "scripts/TestCardLocalizationAudit.py")),
        Suite("production_rules", "pure_production", ("dotnet", "run", "--project", "tests/DesignSyncContracts", "--no-restore")),
        Suite("humility_effects", "pure_production", ("dotnet", "run", "--project", "tests/HumilityEffectContracts", "--no-restore")),
        Suite("generosity_offering", "pure_production", ("dotnet", "run", "--project", "tests/GenerosityOfferingContracts", "--no-restore")),
        Suite("save_codec", "codec_no_engine", ("dotnet", "run", "--project", "tests/LayeredSaveContracts", "--no-restore")),
        Suite("release_build", "compile_only", ("dotnet", "build", "MaidenSuccubus.csproj", "-c", "Release", "-p:DeployMod=false", "--no-restore")),
        Suite("debug_build", "compile_only", ("dotnet", "build", "MaidenSuccubus.csproj", "-c", "Debug", "-p:DeployMod=false", "--no-restore")),
    ]
    for name, script in (
        ("content", "ValidateMvpContent.ps1"),
        ("structure", "ValidateStructuralContracts.ps1"),
        ("localization", "ValidateLocalizationStyle.ps1"),
        ("card_effect_registration", "ValidateCardEffectTests.ps1"),
        ("visual_assets", "ValidateVisualAssets.ps1"),
    ):
        suites.append(Suite(name, "static_gate", (powershell, "-NoProfile", "-NonInteractive", "-File", f"scripts/{script}", "-ProjectDir", ".")))
    suites.append(Suite("full_card_audit", "static_audit", (sys.executable, "scripts/AuditCardLocalization.py", "--no-write")))
    return suites


def select_suites(suites: list[Suite], requested: list[str] | None) -> list[Suite]:
    """Validate before creating reports or executing children; retain plan order."""
    if not requested:
        return list(suites)
    unknown = set(requested) - {suite.id for suite in suites}
    if unknown:
        raise ValueError("Unknown suite(s): " + ", ".join(sorted(unknown)))
    return [suite for suite in suites if suite.id in set(requested)]


def now() -> str:
    return datetime.now(timezone.utc).isoformat()


def fingerprint(root: Path) -> dict:
    """Include untracked source, exclude outputs. Not a claim to hash all visual assets."""
    candidates: set[Path] = set()
    for directory in ("src", "scripts", "tests", "MaidenSuccubus/localization"):
        folder = root / directory
        if folder.is_dir():
            candidates.update(p for p in folder.rglob("*") if p.is_file()
                              and not {"obj", "bin", "__pycache__"}.intersection(p.relative_to(root).parts)
                              and p.suffix.lower() in {".cs", ".csproj", ".json", ".py", ".ps1", ".props", ".targets", ".config"})
    candidates.update(root / name for name in (
        "DesignDoc.md", "PLAN_FRAMEWORK.md", "docs/DESIGN_TRACEABILITY.md", "MaidenSuccubus.csproj"
    ))
    files, errors = {}, []
    for path in sorted(candidates):
        name = path.relative_to(root).as_posix()
        try:
            files[name] = hashlib.sha256(path.read_bytes()).hexdigest()
        except OSError as exc:
            errors.append(f"{name}: {type(exc).__name__}: {exc}")
    digest = hashlib.sha256(json.dumps(files, sort_keys=True).encode("utf-8")).hexdigest()
    return {"sha256": digest, "files": files, "errors": errors,
            "scope": "source, scripts, tests, localization, project and design/plan/trace; excludes binaries and image/audio assets"}


def git_head(root: Path) -> dict:
    try:
        proc = subprocess.run(["git", "rev-parse", "HEAD"], cwd=root, capture_output=True,
                              text=True, encoding="utf-8", errors="replace", timeout=20, check=False)
        return {"head": proc.stdout.strip() if proc.returncode == 0 else None,
                "error": proc.stderr.strip() if proc.returncode else None}
    except (OSError, subprocess.TimeoutExpired) as exc:
        return {"head": None, "error": str(exc)}


def unique_directory(root: Path) -> Path:
    # Only build-output space; never replace latest.json or the shared audit report.
    parent = root / "obj" / "design-sync-validation"
    parent.mkdir(parents=True, exist_ok=True)
    target = parent / (datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ-") + uuid.uuid4().hex[:12])
    target.mkdir(exist_ok=False)
    return target


def execute(suite: Suite, root: Path, log: Path, timeout: float) -> dict:
    result = {"id": suite.id, "kind": suite.kind, "argv": list(suite.argv), "startedAt": now(),
              "log": log.name, "status": "not_run", "exitCode": None}
    started = time.monotonic()
    try:
        with log.open("w", encoding="utf-8") as output:
            completed = subprocess.run(suite.argv, cwd=root, stdout=output, stderr=subprocess.STDOUT,
                                       timeout=timeout, check=False)
        result.update(status="passed" if completed.returncode == 0 else "failed", exitCode=completed.returncode)
    except subprocess.TimeoutExpired as exc:
        result.update(status="timeout", error=str(exc))
    except KeyboardInterrupt:
        result.update(status="interrupted", error="User interrupted this validation process")
    except OSError as exc:
        result.update(status="error", error=f"{type(exc).__name__}: {exc}")
    result.update(finishedAt=now(), elapsedSeconds=round(time.monotonic() - started, 3))
    return result


def summarize(results: list[dict], before: dict, after: dict, head_before: dict, head_after: dict) -> dict:
    drift = before["sha256"] != after["sha256"] or head_before["head"] != head_after["head"]
    metadata_error = bool(before["errors"] or after["errors"] or not head_before["head"] or not head_after["head"])
    interrupted = any(item["status"] == "interrupted" for item in results)
    failed = any(item["status"] in {"failed", "error", "timeout"} for item in results) or drift or metadata_error
    offline = "failed" if failed else "incomplete" if not results or any(item["status"] != "passed" for item in results) else "passed"
    return {"offlineStatus": offline, "sourceChangedDuringRun": drift, "metadataError": metadata_error,
            "goalCompleted": False, "goalStatus": "incomplete", "gameRuntimeStatus": "not_run",
            "exitCode": 130 if interrupted else 1 if failed else 2,
            "passedSuites": sum(item["status"] == "passed" for item in results), "totalSuites": len(results),
            "runtimeGroups": {name: "not_run" for name in ("cards", "monsters", "fourth_act_routes", "events", "multiplayer", "visual_layout")},
            "limitations": ["Passing source or compile gates is not execution of game scenarios.",
                            "No old latest.json or game log is accepted as evidence for this run.",
                            "Open design decisions and unimplemented requirements remain outside any passing offline claim."]}


def run_all(root: Path, suites: list[Suite], directory: Path, timeout: float = 600,
            executor=execute, snapshot=fingerprint, get_head=git_head,
            available_suites: list[Suite] | None = None) -> tuple[dict, Path]:
    available = suites if available_suites is None else available_suites
    selected_ids = [suite.id for suite in suites]
    omitted = [suite.id for suite in available if suite.id not in selected_ids]
    scope = {"mode": "selected" if omitted else "full", "selectedSuites": selected_ids,
             "omittedSuites": omitted, "availableSuiteCount": len(available)}
    started = now()
    before, head_before = snapshot(root), get_head(root)
    results: list[dict] = []
    interrupted = False
    for index, suite in enumerate(suites, 1):
        print(f"[{index}/{len(suites)}] {suite.id}", flush=True)
        if interrupted:
            result = {"id": suite.id, "kind": suite.kind, "argv": list(suite.argv),
                      "status": "not_run", "exitCode": None, "reason": "previous suite interrupted"}
        else:
            result = executor(suite, root, directory / f"{index:02d}-{suite.id}.log", timeout)
        results.append(result)
        interrupted = result["status"] == "interrupted" or interrupted
        print(f"  {result['status']}", flush=True)
        # Preserve partial evidence even if a later process is interrupted externally.
        (directory / "progress.json").write_text(json.dumps({"startedAt": started, "executionScope": scope, "results": results},
                                                          ensure_ascii=False, indent=2), encoding="utf-8")
    after, head_after = snapshot(root), get_head(root)
    summary = summarize(results, before, after, head_before, head_after)
    if omitted and summary["offlineStatus"] == "passed":
        summary["offlineStatus"] = "passed_selected"
    summary["fullPlanOfflineStatus"] = "not_run" if omitted else summary["offlineStatus"]
    summary["executionScope"] = scope
    report = {"schemaVersion": SCHEMA_VERSION, "startedAt": started, "finishedAt": now(), "project": str(root),
              "deployment": "disabled", "gameLaunched": False, "results": results,
              "gitBefore": head_before, "gitAfter": head_after, "sourceBefore": before, "sourceAfter": after,
              "comparisonBaseline": {"ref": "125d9f1c", "exactRequestedSnapshotAvailable": False,
                                     "evidence": "docs/DESIGN_SYNC_20260927_REVIEW.md"},
              "summary": summary}
    path = directory / "report.json"
    path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    return report, path


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--list", action="store_true", help="Print fixed command plan without executing it")
    parser.add_argument("--suite", action="append", metavar="ID", help="Run only this suite; repeat to select more (see --list)")
    parser.add_argument("--timeout-seconds", type=float, default=600, help="Positive per-suite subprocess timeout")
    args = parser.parse_args()
    if not 0 < args.timeout_seconds <= 3600:
        parser.error("--timeout-seconds must be between 0 (exclusive) and 3600")
    available = plan()
    try:
        suites = select_suites(available, args.suite)
    except ValueError as exc:
        parser.error(str(exc))
    if args.list:
        print(json.dumps({"status": "not_executed", "suites": [vars(suite) for suite in suites]}, ensure_ascii=False, indent=2))
        return 0
    report, path = run_all(ROOT, suites, unique_directory(ROOT), args.timeout_seconds, available_suites=available)
    print(json.dumps(report["summary"], ensure_ascii=False, indent=2))
    print(f"Report: {path}")
    return report["summary"]["exitCode"]


if __name__ == "__main__":
    sys.exit(main())
