# CLAUDE.md — NPH Quest 3 VR Project

## Overview

Unity VR application for Meta Quest 3 that visualizes Normal Pressure Hydrocephalus (NPH) detection results. Combines a YOLOv8 FastAPI backend with immersive 3D visualization of CT scans, YOLO detection overlays, ventricle meshes, and clinical scoring.

## Architecture

```
Quest3VR_Prototype/           ← Unity 2022.3.20f1 project
├── Assets/Scripts/
│   ├── *.cs                  ← Base VR kit (grab, teleport, haptics, dashboard)
│   └── NPH/                  ← NPH visualization system (Quest3VR.NPH assembly)
│       ├── NPHDataModels.cs       Data types for API (DetectionBox, NPHMetrics, etc.)
│       ├── NPHApiClient.cs        Singleton HTTP client → FastAPI backend
│       ├── CTSliceViewer.cs       World-space CT display (MaterialPropertyBlock)
│       ├── YOLOOverlayRenderer.cs YOLO bbox LineRenderers + TMP labels
│       ├── NPHScorePanel.cs       Dashboard panel with metrics + haptics
│       ├── VentricleMeshGenerator.cs  Procedural bilateral ventricle mesh
│       ├── NPHSceneController.cs  Main orchestrator (analyze, score, mesh)
│       ├── SliceScrollController.cs   Thumbstick CT slice scrolling
│       └── Editor/
│           ├── NPHSceneBuilder.cs      Scene hierarchy builder (Tools > NPH menu)
│           ├── NPHBuildPipeline.cs     Full automated build pipeline
│           └── Quest3VR.NPH.Editor.asmdef  Editor-only assembly
build-quest3.sh               ← Shell script: full headless build pipeline
.github/workflows/build-quest3.yml  ← CI: game-ci/unity-builder → APK artifact
```

## NPH Backend (separate repo)

The VR app talks to a FastAPI server (see `nph-yolo-detector` repo):

| Endpoint | Purpose |
|----------|---------|
| `POST /analyze` | YOLO 2D inference on CT slice PNG/JPEG |
| `POST /score` | NPH probability scoring |
| `POST /analyze-ct3d` | TotalSegmentator 3D pipeline (NIfTI/DICOM) |
| `POST /mesh-ventricle` | Marching cubes mesh export |
| `GET /health` | Health check + model status |

Default URL: `http://192.168.1.100:8000` — configurable via Inspector or `--api-url`.

## YOLO Classes

0=ventricle, 1=sylvian_fissure, 2=tight_convexity, 3=pvh, 4=skull_inner

## NPH Scoring Formula

VSR 40% + Evans Index 25% + Callosal Angle 20% + DESH 10% + Sylvian Fissure 5%

## Build Commands

```bash
# Full automated pipeline (requires Unity 2022.3.x installed)
./build-quest3.sh

# With real CT textures
./build-quest3.sh --textures ~/ct-scans/ --api-url http://10.0.0.5:8000

# Scene setup only (no APK)
./build-quest3.sh --setup-only

# Inside Unity Editor (menu)
Tools > NPH > Pipeline: Build All (Full Pipeline)
Tools > NPH > Pipeline: Setup Only (No Build)
Tools > NPH > Build NPH Scene  (scene hierarchy only)
```

## Assembly Definitions

| Assembly | Platform | References |
|----------|----------|------------|
| `Quest3VR.Prototype` | All | Base VR kit |
| `Quest3VR.NPH` | All | Prototype, TextMeshPro, XR Interaction Toolkit, InputSystem |
| `Quest3VR.NPH.Editor` | Editor only | NPH, Prototype, TextMeshPro |

## Key Patterns

- **MaterialPropertyBlock** for CT texture swapping (no material instances)
- **Singleton** pattern on NPHApiClient and VRHapticsManager
- **UnityWebRequest + Task.Yield()** for async HTTP (no coroutines)
- **SerializedObject.FindProperty()** in Editor scripts to wire [SerializeField] fields
- **Procedural mesh** generation using spherical coordinates for ventricle approximation
- **LineRenderer** per YOLO detection box with TMP world-space labels

## Quest 3 Build Settings

- Backend: IL2CPP
- Architecture: ARM64
- Texture compression: ASTC 6x6
- Graphics: OpenGLES3
- Rendering: Single Pass Instanced
- Min SDK: Android 10 (API 29)
- XR Plugin: Oculus 4.1.2

## Development Workflow

1. Start NPH backend: `cd NPHProject && uvicorn api:app --reload`
2. Open Unity project, run `Tools > NPH > Pipeline: Setup Only`
3. Press Play to test in Editor (uses demo textures if no real CTs)
4. Deploy: `./build-quest3.sh` or `adb install -r Builds/NPH_Quest3.apk`

## Do NOT

- Put `using UnityEditor` in runtime scripts (only in `Editor/` folder)
- Use `FindObjectOfType` in Update loops (cache references)
- Skip the Editor assembly definition — it prevents build failures on Android
- Hardcode the backend URL — always use the serialized `serverUrl` field
