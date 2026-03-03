# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Unity 2022.3.20f1 VR application for Meta Quest 3 that visualizes Normal Pressure Hydrocephalus (NPH) detection results. Combines a YOLOv8 + TotalSegmentator FastAPI backend with immersive VR visualization: CT slice viewing, YOLO detection overlays, 3D ventricle meshes, and clinical NPH scoring dashboards.

## Repository Structure

```
nph-quest3-vr/                    ← Repo root (build scripts, CI)
├── build-quest3.sh               ← Primary build pipeline script
├── .github/workflows/            ← CI: game-ci/unity-builder → APK artifact
└── Quest3VR_Prototype/           ← Unity project root
    ├── Assets/Scripts/
    │   ├── *.cs                  ← Base VR kit (Quest3VR.Prototype assembly)
    │   └── NPH/                  ← NPH module (Quest3VR.NPH assembly)
    │       └── Editor/           ← Build automation (Quest3VR.NPH.Editor, editor-only)
    ├── Packages/manifest.json    ← Unity package dependencies
    └── ProjectSettings/          ← Unity project config
```

## Build Commands

```bash
# Full pipeline: import textures → build scene → configure Quest 3 → build APK
./build-quest3.sh

# With real CT slices and custom backend URL
./build-quest3.sh --textures ~/ct-scans/ --api-url http://10.0.0.5:8000

# Scene setup only (no APK build, for Editor iteration)
./build-quest3.sh --setup-only

# Unity batch mode (CI or headless)
Unity -batchmode -quit \
  -projectPath Quest3VR_Prototype \
  -executeMethod Quest3VR.NPH.Editor.NPHBuildPipeline.BuildAll

# Deploy to device
adb install -r Builds/NPH_Quest3.apk
```

**Unity Editor menu:** `Tools > NPH > Pipeline: Build All` / `Setup Only` / `Build NPH Scene`

## Architecture

### Three Assembly Definitions

| Assembly | Platform | Role |
|----------|----------|------|
| `Quest3VR.Prototype` | All | Base VR: rig, grab, teleport, haptics, dashboard |
| `Quest3VR.NPH` | All | NPH visualization: API client, CT viewer, YOLO overlay, ventricle mesh, score panel |
| `Quest3VR.NPH.Editor` | Editor only | Build pipeline, scene hierarchy builder |

### NPH Module — Data Flow

```
NPHSceneController (orchestrator)
  ├── CTSliceViewer         → displays slice via MaterialPropertyBlock
  ├── NPHApiClient          → POST /analyze → YOLO boxes + NPH metrics
  ├── YOLOOverlayRenderer   → LineRenderer boxes + TMP labels
  ├── VentricleMeshGenerator → procedural bilateral spheroid mesh
  ├── NPHScorePanel         → metrics dashboard + severity haptics
  └── SliceScrollController → right thumbstick slice navigation
```

### Backend Integration (separate `nph-yolo-detector` repo)

| Endpoint | Purpose |
|----------|---------|
| `GET /health` | Health check + model status |
| `POST /analyze` | YOLO 2D inference on CT slice PNG/JPEG |
| `POST /score` | NPH probability scoring from metrics |
| `POST /analyze-ct3d` | TotalSegmentator 3D pipeline |
| `POST /mesh-ventricle` | Marching cubes mesh export |

Default URL: `http://192.168.1.100:8000` — configurable via Inspector `serverUrl` field or `--api-url` CLI flag. Falls back to demo mode if backend unreachable.

**YOLO classes:** 0=ventricle, 1=sylvian_fissure, 2=tight_convexity, 3=pvh, 4=skull_inner

**Scoring formula:** VSR 40% + Evans Index 25% + Callosal Angle 20% + DESH 10% + Sylvian Fissure 5%

## Key Patterns

- **MaterialPropertyBlock** for CT texture swapping (avoids material instance overhead on mobile GPU)
- **Singletons** (`DontDestroyOnLoad`): `NPHApiClient.Instance`, `VRHapticsManager.Instance`
- **async/await with `UnityWebRequest + Task.Yield()`** — no coroutines for HTTP calls
- **SerializedObject.FindProperty()** in Editor scripts to programmatically wire `[SerializeField]` references
- **Procedural mesh** via spherical coordinates for bilateral ventricle approximation
- **LineRenderer** per YOLO detection box (5-point closed rectangle) with world-space TMP labels

## Quest 3 Build Settings (auto-configured by pipeline)

IL2CPP, ARM64, ASTC 6x6, OpenGLES3, Single Pass Instanced rendering, Android 10+ (API 29), Oculus XR Plugin 4.1.2, 72 Hz target.

**Performance targets:** <100 draw calls, <50k polys/frame, <256MB texture memory.

## Development Workflow

1. Start NPH backend: `cd nph-yolo-detector && uvicorn api:app --reload`
2. Open `Quest3VR_Prototype` in Unity 2022.3+
3. Run `Tools > NPH > Pipeline: Setup Only` to build scene hierarchy
4. Press Play to test (demo mode if no backend)
5. Deploy: `./build-quest3.sh` then `adb install -r Builds/NPH_Quest3.apk`

## CI/CD

GitHub Actions on push to `feature/nph-yolo-system` or `master`. Uses `game-ci/unity-builder@v4`. Requires secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`. Manual dispatch via `workflow_dispatch` with configurable API URL.

## Anti-Patterns

- Never put `using UnityEditor` in runtime scripts — only in `Assets/Scripts/NPH/Editor/`
- Never use `FindObjectOfType` in Update loops — cache references
- Never skip the Editor assembly definition — it prevents Android build failures
- Never hardcode the backend URL — use the serialized `serverUrl` field
- Never use material instances for CT texture swapping — use `MaterialPropertyBlock`
