# AGENTS.md — Quest 3 VR Starter Kit

## Project Overview

This is a **Unity VR application** for Meta Quest 3 that combines a medical AI detection backend with immersive 3D visualization. The project visualizes Normal Pressure Hydrocephalus (NPH) detection results, integrating a YOLOv8 FastAPI backend with VR-based CT scan viewing, real-time detection overlays, 3D ventricle mesh visualization, and clinical NPH scoring.

**Key Technologies:**
- Unity 2022.3 LTS (tested with 2022.3.20f1)
- C# with .NET Standard 2.1
- XR Interaction Toolkit 2.5+
- Oculus XR Plugin 4.1.2
- TextMeshPro for UI
- Async/await patterns with UnityWebRequest

**Platform Target:**
- Meta Quest 3 (Android)
- ARM64 architecture
- OpenGLES3 graphics API
- Single Pass Instanced rendering

---

## Project Structure

```
Quest3VR_Prototype/              # Unity project root
├── Assets/
│   ├── Scripts/
│   │   ├── *.cs                 # Base VR kit scripts
│   │   ├── Quest3VR.Prototype.asmdef
│   │   └── NPH/                 # NPH visualization module
│   │       ├── NPHDataModels.cs       # API data types + demo mode
│   │       ├── NPHApiClient.cs        # HTTP client singleton
│   │       ├── CTSliceViewer.cs       # CT slice display with texture handling
│   │       ├── YOLOOverlayRenderer.cs # YOLO bounding box rendering
│   │       ├── NPHScorePanel.cs       # Score dashboard panel
│   │       ├── VentricleMeshGenerator.cs  # Procedural ventricle mesh
│   │       ├── NPHSceneController.cs  # Main orchestrator with demo fallback
│   │       ├── SliceScrollController.cs   # Thumbstick scrolling with haptics
│   │       ├── Quest3VR.NPH.asmdef
│   │       └── Editor/          # Editor-only scripts
│   │           ├── NPHBuildPipeline.cs    # Automated build pipeline
│   │           ├── NPHSceneBuilder.cs     # Scene hierarchy builder
│   │           ├── NPHSceneValidator.cs   # Scene validation utility
│   │           └── Quest3VR.NPH.Editor.asmdef
│   ├── Scenes/
│   │   └── VRStarterScene.unity
│   ├── Prefabs/
│   │   ├── XR_Rig_Quest3.prefab
│   │   ├── Grabbable_Cube.prefab
│   │   └── VR_Dashboard.prefab
│   ├── Materials/
│   └── Textures/SampleCT/       # Generated or imported CT slices
├── ProjectSettings/             # Unity project configuration
├── docs/plans/                  # Implementation documentation
├── README.md                    # User documentation
├── DesignDocument.md            # Technical specifications
└── CLAUDE.md                    # Developer guide for AI assistants

build-quest3.sh                  # Shell build script (root)
.github/workflows/build-quest3.yml   # GitHub Actions CI/CD
```

---

## Assembly Definitions

The project uses three assembly definitions to organize code and manage dependencies:

| Assembly | Platform | References |
|----------|----------|------------|
| `Quest3VR.Prototype` | All | XR Interaction Toolkit, Oculus, XR Management, Input System, XR CoreUtils |
| `Quest3VR.NPH` | All | Prototype, TextMeshPro, XR Interaction Toolkit, Input System |
| `Quest3VR.NPH.Editor` | Editor only | NPH, Prototype, TextMeshPro |

**Important:** Editor scripts MUST be in the `Editor/` folder with the `Quest3VR.NPH.Editor` assembly definition. This prevents build failures on Android.

---

## Build Commands

### Automated Build (Shell Script)

```bash
# Full pipeline: import textures → build scene → validate → configure Quest 3 → build APK
./build-quest3.sh

# With real CT slices and custom API URL
./build-quest3.sh --textures ~/ct-scans/ --api-url http://10.0.0.5:8000

# Scene setup only (no APK build)
./build-quest3.sh --setup-only

# Skip validation or adjust strictness
./build-quest3.sh --strict-validation false

# Custom output path
./build-quest3.sh --output ~/Desktop/nph.apk
```

### Unity Editor Menu

Access via `Tools > NPH` menu:
- `Pipeline: Build All (Full Pipeline)` — Complete build with validation
- `Pipeline: Setup Only (No Build)` — Scene setup without APK
- `Pipeline: Import Textures` — Import CT textures only
- `Pipeline: Build & Configure Scene` — Build scene hierarchy
- `Pipeline: Configure Quest 3 Build` — Configure Android settings
- `Pipeline: Build APK` — Build APK only
- `Validate Scene` — Check scene configuration
- `Quick Fix Scene` — Auto-fix common scene issues

### CI/CD (GitHub Actions)

Workflow: `.github/workflows/build-quest3.yml`

Triggers:
- Push to `feature/nph-yolo-system` or `master` branches
- Pull requests to `master`
- Manual trigger via `workflow_dispatch`

Required secrets:
- `UNITY_LICENSE` — Unity license file
- `UNITY_EMAIL` — Unity account email
- `UNITY_PASSWORD` — Unity account password

---

## Code Organization

### Base VR Kit (`Assets/Scripts/`)

| Script | Purpose |
|--------|---------|
| `VRRigSetup.cs` | VR rig initialization and configuration |
| `VRGrabInteraction.cs` | Grab system with physics-based throwing |
| `VRTeleportation.cs` | Arc-based teleportation with fade transitions |
| `VRHapticsManager.cs` | Haptic feedback singleton with 6 preset patterns |
| `VRDashboardController.cs` | World-space VR UI dashboard |
| `GrabbableObject.cs` | Component for interactable objects |

### NPH Module (`Assets/Scripts/NPH/`)

| Script | Purpose |
|--------|---------|
| `NPHDataModels.cs` | Serializable data types for API + DemoModeData generator |
| `NPHApiClient.cs` | Singleton HTTP client for FastAPI backend |
| `CTSliceViewer.cs` | World-space CT display using MaterialPropertyBlock |
| `YOLOOverlayRenderer.cs` | LineRenderer-based YOLO bounding box visualization |
| `NPHScorePanel.cs` | Dashboard panel displaying NPH metrics |
| `VentricleMeshGenerator.cs` | Procedural bilateral ventricle mesh generation |
| `NPHSceneController.cs` | Main orchestrator with demo mode fallback |
| `SliceScrollController.cs` | Thumbstick-based CT slice scrolling with haptics |

### Editor Scripts (`Assets/Scripts/NPH/Editor/`)

| Script | Purpose |
|--------|---------|
| `NPHBuildPipeline.cs` | Automated build pipeline with MenuItem entries |
| `NPHSceneBuilder.cs` | Programmatic scene hierarchy construction |
| `NPHSceneValidator.cs` | Scene configuration validation and auto-fix |

---

## Key Patterns and Conventions

### Singleton Pattern
Used for `NPHApiClient`, `VRHapticsManager`, `NPHScorePanel`, `VRDashboardController`:
```csharp
public static NPHApiClient Instance { get; private set; }

private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    else
    {
        Destroy(gameObject);
    }
}
```

### Async HTTP with UnityWebRequest
Use `Task.Yield()` instead of coroutines:
```csharp
private async Task<string> GetRequest(string url)
{
    using var req = UnityWebRequest.Get(url);
    var op = req.SendWebRequest();
    while (!op.isDone) await Task.Yield();
    // Handle response
}
```

### MaterialPropertyBlock for Runtime Textures
Efficient texture swapping without creating material instances:
```csharp
private MaterialPropertyBlock propertyBlock;
propertyBlock.SetTexture("_MainTex", texture);
renderer.SetPropertyBlock(propertyBlock);
```

### SerializedObject for Editor Wiring
Use `SerializedObject.FindProperty()` to set `[SerializeField]` fields in Editor scripts:
```csharp
var so = new SerializedObject(targetComponent);
var prop = so.FindProperty("fieldName");
prop.objectReferenceValue = reference;
so.ApplyModifiedPropertiesWithoutUndo();
```

---

## Demo Mode

The application includes a **Demo Mode** that works without a backend server. When the backend is unreachable or `useDemoModeFallback` is enabled:

1. Synthetic metrics are generated using `DemoModeData.GenerateSyntheticMetrics()`
2. Synthetic YOLO detection boxes are created around ventricles and skull
3. NPH scores are calculated locally using the same formula as the backend
4. 3D ventricle meshes are generated from synthetic metrics

**To force demo mode:**
- Set `NPHSceneController > Use Demo Mode Fallback = true`
- Or call `ForceDemoMode()` from the inspector context menu
- Or when backend health check fails

### Demo Mode Scoring Formula
- VSR: 40%
- Evans Index: 25%
- Callosal Angle: 20%
- DESH Score: 10%
- Sylvian Fissure Dilation: 5%

---

## NPH Backend API

The VR app connects to a FastAPI server (separate repository: `nph-yolo-detector`):

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/health` | GET | Health check + model status |
| `/analyze` | POST | YOLO 2D inference on CT slice |
| `/score` | POST | NPH probability scoring |
| `/analyze-ct3d` | POST | TotalSegmentator 3D pipeline |
| `/mesh-ventricle` | POST | Marching cubes mesh export |

Default URL: `http://192.168.1.100:8000` (configurable via Inspector or `--api-url`)

### YOLO Classes
- 0 = ventricle
- 1 = sylvian_fissure
- 2 = tight_convexity
- 3 = pvh (periventricular hyperintensity)
- 4 = skull_inner

### NPH Scoring Formula
`VSR 40% + Evans Index 25% + Callosal Angle 20% + DESH 10% + Sylvian Fissure 5%`

---

## Quest 3 Build Settings

| Setting | Value |
|---------|-------|
| Scripting Backend | IL2CPP |
| Target Architecture | ARM64 |
| Texture Compression | ASTC 6x6 |
| Graphics API | OpenGLES3 |
| Stereo Rendering | Single Pass Instanced |
| Minimum API Level | Android 10 (API 29) |
| XR Plugin | Oculus 4.1.2 |

---

## Development Workflow

1. **Start NPH backend:**
   ```bash
   cd nph-yolo-detector && uvicorn api:app --reload
   ```

2. **Open Unity project and setup scene:**
   ```bash
   ./build-quest3.sh --setup-only
   ```
   Or in Unity: `Tools > NPH > Pipeline: Setup Only (No Build)`

3. **Validate scene:**
   ```bash
   # In Unity
   Tools > NPH > Validate Scene
   ```

4. **Test in Editor:**
   - Open `Assets/Scenes/VRStarterScene.unity`
   - Press Play (uses demo textures if no real CTs)

5. **Build and deploy:**
   ```bash
   ./build-quest3.sh
   adb install -r Builds/NPH_Quest3.apk
   ```

---

## Testing

### Manual Testing Checklist
- [ ] VR rig initializes correctly
- [ ] Grab interactions work on both controllers
- [ ] Teleportation with arc visualization functions
- [ ] Haptic feedback triggers on interactions
- [ ] Dashboard opens/closes with Menu button
- [ ] CT slice scrolling works with right thumbstick
- [ ] Backend connection status displays correctly
- [ ] YOLO overlays render with correct colors
- [ ] NPH score panel updates with metrics
- [ ] 3D ventricle mesh generates and is grabbable
- [ ] Demo mode works when backend is offline

### Demo Mode Testing
1. Disconnect from network or stop backend
2. Start the app - should show "Backend offline — using demo mode"
3. Scroll through slices - should see synthetic YOLO boxes
4. Check NPH score - should show calculated score from synthetic metrics

---

## Code Style Guidelines

### Namespaces
All scripts use organized namespaces:
- `Quest3VR.Prototype` — Base VR kit
- `Quest3VR.NPH` — NPH visualization module
- `Quest3VR.NPH.Editor` — Editor-only tools

### Naming Conventions
- **Classes:** PascalCase (e.g., `NPHSceneController`)
- **Methods:** PascalCase (e.g., `AnalyzeCurrentSlice`)
- **Fields:** camelCase with `[SerializeField]` for private inspector fields
- **Constants:** PascalCase or ALL_CAPS for public constants

### Comments
- Use XML documentation for public APIs: `/// <summary>`
- Inline comments for complex logic
- Region blocks for organizing large classes (`#region Public Methods`)

---

## Security Considerations

1. **No hardcoded secrets** — API URLs are configurable via Inspector or command-line args
2. **Editor assembly isolation** — Prevents Editor-only code from reaching Android builds
3. **File path validation** — Check `File.Exists()` before reading user-provided paths
4. **Timeout handling** — All HTTP requests have appropriate timeouts (10s health, 30s JSON, 300s file upload)

---

## Common Issues

### Build fails with "UnityEditor namespace" error
Ensure Editor scripts are in `Assets/Scripts/NPH/Editor/` folder with `Quest3VR.NPH.Editor.asmdef`.

### Textures not showing in build
Verify textures are marked as `isReadable` in import settings (done automatically by `ImportTextures()`). The `CTSliceViewer` also handles non-readable textures by creating readable copies.

### Backend connection fails
Check that `NPHApiClient > Server Url` matches your backend IP. Use `CheckHealth()` to diagnose. The app will fall back to demo mode automatically.

### Haptics not working
Ensure `VRHapticsManager` is in the scene and controllers are properly referenced.

### Slice scrolling not working
Check that `SliceScrollController` has a reference to `CTSliceViewer`. Try enabling `Use Legacy Input` as a fallback.

### Scene validation fails
Run `Tools > NPH > Quick Fix Scene` to auto-fix common issues, or check the validation output for specific missing components.

---

## External Dependencies

### Unity Packages (via Package Manager)
- `com.unity.xr.interaction.toolkit` 2.5.2
- `com.unity.xr.oculus` 4.1.2
- `com.unity.xr.management` 4.4.0
- `com.unity.xr.hands` 1.3.0
- `com.unity.textmeshpro` 3.0.6
- `com.unity.xr.coreutils`

### External Tools Required
- Unity 2022.3.x with Android Build Support (IL2CPP + SDK/NDK)
- Android SDK with API 29+
- Optional: NPH backend server (Python/FastAPI)

---

## License

MIT License — Free for commercial and personal use.
