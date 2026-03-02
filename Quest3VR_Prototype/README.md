# Quest 3 VR Prototype — NPH Visualization

A Unity VR application for Meta Quest 3 that combines an NPH (Normal Pressure Hydrocephalus) AI detection backend with immersive 3D visualization. View CT scans in VR, see real-time YOLO detection overlays, interact with 3D ventricle meshes, and review clinical NPH scores — all hands-on in a Quest 3 headset.

## Features

### Base VR Kit
- **XR Rig Setup** — Pre-configured Quest 3 optimized VR rig
- **Grab Interactions** — Direct grab, throw physics, and visual feedback
- **Teleportation** — Arc-based teleportation with preview and fade transitions
- **Haptic Feedback** — Advanced haptics with customizable patterns
- **UI Dashboard** — Floating VR dashboard with interactive panels

### NPH Visualization System
- **CT Slice Viewer** — Browse CT slices in world-space with thumbstick scrolling
- **YOLO Detection Overlay** — Real-time bounding boxes for ventricle, sylvian fissure, tight convexity, PVH, skull inner
- **3D Ventricle Mesh** — Procedural bilateral ventricle visualization from Evans Index/VSR metrics, grabbable and color-coded by severity
- **NPH Score Dashboard** — Live score panel showing Evans Index, VSR, Callosal Angle, DESH Score, Sylvian Dilation, with severity-based haptic feedback
- **Backend Integration** — HTTP client connecting to a FastAPI server for YOLO inference, NPH scoring, and TotalSegmentator 3D analysis

## Requirements

- Unity 2022.3 LTS (tested with 2022.3.20f1)
- XR Plugin Management + Oculus XR Plugin 4.1.2
- XR Interaction Toolkit 2.5+
- TextMeshPro
- Input System

For the AI backend (optional — app works with demo mode):
- Python 3.10+ with FastAPI, ultralytics, TotalSegmentator
- See [nph-yolo-detector](https://github.com/matheus-rech/nph-yolo-detector) for backend setup

## Quick Start

### Automated Build (recommended)

```bash
# Full pipeline: import textures → build scene → configure Quest 3 → build APK
./build-quest3.sh

# With real CT slices and custom API URL
./build-quest3.sh --textures ~/ct-scans/ --api-url http://10.0.0.5:8000

# Scene setup only (then iterate in Unity Editor)
./build-quest3.sh --setup-only

# See all options
./build-quest3.sh --help
```

### Manual Setup

1. Open the project in Unity 2022.3+
2. Run `Tools > NPH > Pipeline: Setup Only (No Build)` from the menu bar
3. Open `Assets/Scenes/VRStarterScene.unity`
4. Set `NPHApiClient > Server Url` to your backend IP in the Inspector
5. (Optional) Import CT PNGs to `Assets/Textures/SampleCT/` and assign to `NPHSceneController > Sample CT Slices`
6. Press Play to test, or Build & Run for Quest 3

## Project Structure

```
Quest3VR_Prototype/
├── Assets/
│   ├── Scripts/
│   │   ├── VRRigSetup.cs              # VR rig initialization
│   │   ├── VRGrabInteraction.cs        # Grab system
│   │   ├── VRTeleportation.cs          # Teleportation system
│   │   ├── VRHapticsManager.cs         # Haptic feedback (singleton)
│   │   ├── VRDashboardController.cs    # UI dashboard
│   │   ├── GrabbableObject.cs          # Grabbable component
│   │   └── NPH/                        # NPH visualization module
│   │       ├── NPHDataModels.cs        # API data types
│   │       ├── NPHApiClient.cs         # HTTP client singleton
│   │       ├── CTSliceViewer.cs        # CT slice display (MaterialPropertyBlock)
│   │       ├── YOLOOverlayRenderer.cs  # YOLO bbox rendering
│   │       ├── NPHScorePanel.cs        # Score dashboard panel
│   │       ├── VentricleMeshGenerator.cs # Procedural ventricle mesh
│   │       ├── NPHSceneController.cs   # Main orchestrator
│   │       ├── SliceScrollController.cs # Thumbstick slice scrolling
│   │       └── Editor/
│   │           ├── NPHSceneBuilder.cs  # Scene hierarchy builder
│   │           └── NPHBuildPipeline.cs # Automated build pipeline
│   ├── Scenes/
│   │   └── VRStarterScene.unity
│   ├── Prefabs/
│   ├── Materials/
│   └── Textures/
│       └── SampleCT/                   # Generated or imported CT slices
├── ProjectSettings/
├── docs/
│   └── plans/                          # Implementation plans
├── CLAUDE.md                           # Developer guide for AI assistants
└── README.md                           # This file
build-quest3.sh                         # Automated build script
.github/workflows/build-quest3.yml      # CI workflow
```

## Controls

| Action | Controller Input |
|--------|-----------------|
| Grab objects | Grip Button |
| Teleport | Thumbstick Press + Aim |
| Dashboard | Menu Button / M Key |
| Scroll CT slices | Right Thumbstick Up/Down |
| Grab ventricle mesh | Grip on 3D mesh |
| Recenter | Dashboard Button |

## NPH Backend API

The VR app connects to a FastAPI server for AI-powered analysis:

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/health` | GET | Health check + model status |
| `/analyze` | POST | YOLO 2D inference on CT slice |
| `/score` | POST | NPH probability scoring |
| `/analyze-ct3d` | POST | TotalSegmentator 3D pipeline |
| `/mesh-ventricle` | POST | Marching cubes mesh export |

Default server: `http://192.168.1.100:8000`. If the backend is unreachable, the app falls back to demo mode.

### Scoring Formula

| Component | Weight | Threshold |
|-----------|--------|-----------|
| VSR (Ventricular-Skull Ratio) | 40% | > 2.0 = high NPH probability |
| Evans Index | 25% | > 0.3 = ventriculomegaly |
| Callosal Angle | 20% | < 90° = suggestive of NPH |
| DESH Score | 10% | Higher = more characteristic |
| Sylvian Fissure Dilation | 5% | Present/absent |

## CI/CD

GitHub Actions builds the APK automatically on push:

```yaml
# Triggered on push to feature/nph-yolo-system or master
# Uses game-ci/unity-builder with Unity 2022.3.20f1
# Uploads APK as artifact (30-day retention)
```

**Required secrets:** `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`

See [game-ci activation docs](https://game.ci/docs/github/activation) for Unity license setup.

Manual trigger available via `workflow_dispatch` with configurable API URL.

## Building for Quest 3

### Build Settings (auto-configured by pipeline)

| Setting | Value |
|---------|-------|
| Scripting Backend | IL2CPP |
| Target Architecture | ARM64 |
| Texture Compression | ASTC 6x6 |
| Graphics API | OpenGLES3 |
| Stereo Rendering | Single Pass Instanced |
| Minimum API Level | Android 10 (API 29) |
| XR Plugin | Oculus 4.1.2 |

### Deploy to Quest 3

```bash
adb install -r Builds/NPH_Quest3.apk
```

## License

MIT License — Free for commercial and personal use.
