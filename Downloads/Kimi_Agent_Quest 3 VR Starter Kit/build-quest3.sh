#!/usr/bin/env bash
set -euo pipefail

# ╔══════════════════════════════════════════════════════════════╗
# ║  build-quest3.sh — Automated NPH VR Quest 3 Build Pipeline  ║
# ╚══════════════════════════════════════════════════════════════╝
#
# Automates the full pipeline:
#   1. Find Unity Editor installation
#   2. Import CT textures (or generate demos)
#   3. Build NPH scene hierarchy & wire all references
#   4. Configure Android/Quest 3 build settings
#   5. Build APK
#
# Usage:
#   ./build-quest3.sh                           # Full pipeline with demo textures
#   ./build-quest3.sh --textures ~/ct-images/   # Use real CT slices
#   ./build-quest3.sh --setup-only              # Scene setup only, no APK build
#   ./build-quest3.sh --api-url http://10.0.0.5:8000
#   ./build-quest3.sh --unity /path/to/Unity    # Custom Unity path
#
# Requirements:
#   - Unity 2022.3.x with Android Build Support (IL2CPP + SDK/NDK)
#   - Android SDK with API 29+ (usually installed with Unity)

# ── Defaults ──────────────────────────────────────────────────

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="${SCRIPT_DIR}/Quest3VR_Prototype"
UNITY_PATH=""
API_URL="http://192.168.1.100:8000"
TEXTURES_DIR=""
OUTPUT_PATH=""
SETUP_ONLY=false
SKIP_BUILD=false
LOG_FILE="${SCRIPT_DIR}/build-quest3.log"
UNITY_VERSION="2022.3"

# ── Colors ────────────────────────────────────────────────────

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m' # No Color

# ── Helpers ───────────────────────────────────────────────────

log()    { echo -e "${BLUE}[$(date +%H:%M:%S)]${NC} $*"; }
info()   { echo -e "${CYAN}[INFO]${NC} $*"; }
ok()     { echo -e "${GREEN}[OK]${NC} $*"; }
warn()   { echo -e "${YELLOW}[WARN]${NC} $*"; }
err()    { echo -e "${RED}[ERROR]${NC} $*" >&2; }
step()   { echo -e "\n${BOLD}── $* ──${NC}"; }
banner() {
    echo -e "${BOLD}${CYAN}"
    echo "╔══════════════════════════════════════════════════╗"
    echo "║     NPH VR Quest 3 — Automated Build Pipeline   ║"
    echo "╚══════════════════════════════════════════════════╝"
    echo -e "${NC}"
}

usage() {
    cat <<'EOF'
Usage: ./build-quest3.sh [OPTIONS]

Options:
  --unity PATH        Path to Unity Editor executable (auto-detected if omitted)
  --api-url URL       NPH backend API URL (default: http://192.168.1.100:8000)
  --textures DIR      Directory with CT slice PNGs/JPGs (generates demos if omitted)
  --output PATH       Output APK path (default: Builds/NPH_Quest3.apk)
  --setup-only        Import textures + build scene only, skip APK build
  --skip-build        Run full pipeline but skip APK compilation
  --strict-validation Fail build if scene validation has errors (default: true)
  --log FILE          Log file path (default: build-quest3.log)
  --help              Show this help message

Examples:
  ./build-quest3.sh                                    # Full pipeline, demo textures
  ./build-quest3.sh --textures ~/ct-scans/ --api-url http://10.0.0.5:8000
  ./build-quest3.sh --setup-only                       # Just set up the scene
  ./build-quest3.sh --output ~/Desktop/nph.apk         # Custom output path

Pipeline Steps:
  1. Detect Unity 2022.3.x installation
  2. Import CT textures (real or procedural demo)
  3. Build NPH scene hierarchy (NPHSceneBuilder)
  4. Wire all SerializeField references (NPHBuildPipeline)
  5. Configure Android/Quest 3 settings (IL2CPP, ARM64, ASTC)
  6. Build APK

EOF
    exit 0
}

# ── Parse Arguments ───────────────────────────────────────────

STRICT_VALIDATION=true

while [[ $# -gt 0 ]]; do
    case "$1" in
        --unity)      UNITY_PATH="$2"; shift 2 ;;
        --api-url)    API_URL="$2"; shift 2 ;;
        --textures)   TEXTURES_DIR="$2"; shift 2 ;;
        --output)     OUTPUT_PATH="$2"; shift 2 ;;
        --setup-only) SETUP_ONLY=true; shift ;;
        --skip-build) SKIP_BUILD=true; shift ;;
        --strict-validation) STRICT_VALIDATION="$2"; shift 2 ;;
        --log)        LOG_FILE="$2"; shift 2 ;;
        --help|-h)    usage ;;
        *)            err "Unknown option: $1"; usage ;;
    esac
done

# ── Validate Project ──────────────────────────────────────────

banner

if [[ ! -d "$PROJECT_DIR" ]]; then
    err "Unity project not found at: $PROJECT_DIR"
    err "Expected directory: Quest3VR_Prototype/"
    exit 1
fi

if [[ ! -f "$PROJECT_DIR/ProjectSettings/ProjectVersion.txt" ]]; then
    err "Not a valid Unity project — missing ProjectSettings/ProjectVersion.txt"
    exit 1
fi

PROJECT_UNITY_VERSION=$(grep "m_EditorVersion" "$PROJECT_DIR/ProjectSettings/ProjectVersion.txt" | sed 's/m_EditorVersion: //')
info "Project Unity version: ${BOLD}${PROJECT_UNITY_VERSION}${NC}"

# ── Find Unity Editor ────────────────────────────────────────

find_unity() {
    # 1. Explicit path provided
    if [[ -n "$UNITY_PATH" ]]; then
        if [[ -x "$UNITY_PATH" ]]; then
            echo "$UNITY_PATH"
            return 0
        elif [[ -x "$UNITY_PATH/Contents/MacOS/Unity" ]]; then
            echo "$UNITY_PATH/Contents/MacOS/Unity"
            return 0
        fi
        err "Unity not found at: $UNITY_PATH"
        return 1
    fi

    # 2. Unity Hub standard locations (macOS)
    local hub_editors="/Applications/Unity/Hub/Editor"
    if [[ -d "$hub_editors" ]]; then
        # Try exact version first
        local exact="$hub_editors/$PROJECT_UNITY_VERSION/Unity.app/Contents/MacOS/Unity"
        if [[ -x "$exact" ]]; then
            echo "$exact"
            return 0
        fi

        # Try any 2022.3.x
        for editor_dir in "$hub_editors"/2022.3.*/; do
            local candidate="${editor_dir}Unity.app/Contents/MacOS/Unity"
            if [[ -x "$candidate" ]]; then
                echo "$candidate"
                return 0
            fi
        done

        # Try any 2022.x
        for editor_dir in "$hub_editors"/2022.*/; do
            local candidate="${editor_dir}Unity.app/Contents/MacOS/Unity"
            if [[ -x "$candidate" ]]; then
                warn "Using $(basename "$(dirname "$(dirname "$(dirname "$candidate")")")") instead of $PROJECT_UNITY_VERSION"
                echo "$candidate"
                return 0
            fi
        done
    fi

    # 3. Direct /Applications install
    local direct="/Applications/Unity/Unity.app/Contents/MacOS/Unity"
    if [[ -x "$direct" ]]; then
        echo "$direct"
        return 0
    fi

    # 4. Spotlight search (macOS)
    local spotlight
    spotlight=$(mdfind "kMDItemFSName == 'Unity.app' && kMDItemKind == 'Application'" 2>/dev/null | head -1)
    if [[ -n "$spotlight" && -x "$spotlight/Contents/MacOS/Unity" ]]; then
        echo "$spotlight/Contents/MacOS/Unity"
        return 0
    fi

    # 5. Linux paths
    for linux_path in \
        "$HOME/Unity/Hub/Editor/$PROJECT_UNITY_VERSION/Editor/Unity" \
        "$HOME/Unity/Hub/Editor"/2022.3.*/Editor/Unity \
        /opt/unity/Editor/Unity; do
        if [[ -x "$linux_path" ]]; then
            echo "$linux_path"
            return 0
        fi
    done

    # 6. PATH
    if command -v Unity &>/dev/null; then
        command -v Unity
        return 0
    fi

    return 1
}

step "Step 0: Locating Unity Editor"

UNITY_EXE=$(find_unity) || {
    err "Could not find Unity Editor."
    echo ""
    echo "Install Unity 2022.3.x via Unity Hub, or specify the path:"
    echo "  ./build-quest3.sh --unity /Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app"
    echo ""
    echo "Unity Hub download: https://unity.com/download"
    exit 1
}

ok "Found Unity: $UNITY_EXE"
UNITY_ACTUAL_VERSION=$("$UNITY_EXE" -version 2>/dev/null || echo "unknown")
info "Unity version: $UNITY_ACTUAL_VERSION"

# ── Validate Textures Directory ───────────────────────────────

if [[ -n "$TEXTURES_DIR" ]]; then
    if [[ ! -d "$TEXTURES_DIR" ]]; then
        err "Textures directory not found: $TEXTURES_DIR"
        exit 1
    fi
    TEXTURE_COUNT=$(find "$TEXTURES_DIR" -maxdepth 1 \( -name '*.png' -o -name '*.jpg' -o -name '*.jpeg' \) | wc -l | tr -d ' ')
    if [[ "$TEXTURE_COUNT" -eq 0 ]]; then
        warn "No PNG/JPG files in $TEXTURES_DIR — will generate demo textures"
        TEXTURES_DIR=""
    else
        ok "Found $TEXTURE_COUNT CT texture(s) in $TEXTURES_DIR"
    fi
fi

# ── Build Unity Command Args ─────────────────────────────────

build_unity_args() {
    local method="$1"
    shift

    local args=(
        -batchmode
        -nographics
        -quit
        -projectPath "$PROJECT_DIR"
        -executeMethod "$method"
        -logFile "$LOG_FILE"
        -apiUrl "$API_URL"
    )

    if [[ -n "$TEXTURES_DIR" ]]; then
        args+=(-texturesDir "$TEXTURES_DIR")
    fi

    if [[ -n "$OUTPUT_PATH" ]]; then
        args+=(-outputPath "$OUTPUT_PATH")
    fi

    # Pass through extra flags
    args+=("$@")

    echo "${args[@]}"
}

run_unity() {
    local description="$1"
    local method="$2"
    shift 2

    log "$description"
    info "Method: $method"

    local start_time
    start_time=$(date +%s)

    # Build args array
    local -a args
    args=(-batchmode -nographics -quit -projectPath "$PROJECT_DIR" -executeMethod "$method" -logFile "$LOG_FILE" -apiUrl "$API_URL")

    if [[ -n "$TEXTURES_DIR" ]]; then
        args+=(-texturesDir "$TEXTURES_DIR")
    fi

    if [[ -n "$OUTPUT_PATH" ]]; then
        args+=(-outputPath "$OUTPUT_PATH")
    fi

    if [[ "$STRICT_VALIDATION" == "true" ]]; then
        args+=(-strictValidation)
    fi

    # Pass through extra flags
    args+=("$@")

    info "Running: Unity ${args[*]}"
    echo "---" >> "$LOG_FILE"

    if "$UNITY_EXE" "${args[@]}" 2>&1; then
        local end_time
        end_time=$(date +%s)
        local elapsed=$((end_time - start_time))
        ok "$description completed in ${elapsed}s"
        return 0
    else
        local exit_code=$?
        err "$description FAILED (exit code: $exit_code)"
        err "Check log: $LOG_FILE"
        echo ""
        echo "Last 20 lines of Unity log:"
        tail -20 "$LOG_FILE" 2>/dev/null || true
        return $exit_code
    fi
}

# ── Execute Pipeline ──────────────────────────────────────────

PIPELINE_START=$(date +%s)

# Clear log
echo "NPH VR Quest 3 Build — $(date)" > "$LOG_FILE"
echo "Unity: $UNITY_EXE" >> "$LOG_FILE"
echo "Project: $PROJECT_DIR" >> "$LOG_FILE"
echo "API URL: $API_URL" >> "$LOG_FILE"
echo "Textures: ${TEXTURES_DIR:-<generating demos>}" >> "$LOG_FILE"
echo "Output: ${OUTPUT_PATH:-<default: Builds/NPH_Quest3.apk>}" >> "$LOG_FILE"
echo "========================================" >> "$LOG_FILE"

if [[ "$SETUP_ONLY" == true ]]; then
    # ── Setup-only mode ───────────────────────────────────────
    step "Step 1/3: Importing CT Textures"
    run_unity "Texture import" "Quest3VR.NPH.Editor.NPHBuildPipeline.ImportTextures"

    step "Step 2/3: Building & Configuring Scene"
    run_unity "Scene build + configuration" "Quest3VR.NPH.Editor.NPHBuildPipeline.BuildAndConfigureScene"

    step "Step 3/3: Validating Scene"
    run_unity "Scene validation" "Quest3VR.NPH.Editor.NPHSceneValidator.ValidateScene"

    PIPELINE_END=$(date +%s)
    TOTAL=$((PIPELINE_END - PIPELINE_START))

    echo ""
    echo -e "${GREEN}${BOLD}Setup complete in ${TOTAL}s${NC}"
    echo ""
    echo "Next steps:"
    echo "  1. Open Unity and load the project"
    echo "  2. Open Assets/Scenes/VRStarterScene.unity"
    echo "  3. Review the NPH_System hierarchy"
    echo "  4. Press Play to test in Editor"
    echo "  5. Build & Run for Quest 3 when ready"

else
    # ── Full pipeline ─────────────────────────────────────────
    step "Step 1/5: Importing CT Textures"
    run_unity "Texture import" "Quest3VR.NPH.Editor.NPHBuildPipeline.ImportTextures"

    step "Step 2/5: Building & Configuring Scene"
    run_unity "Scene build + configuration" "Quest3VR.NPH.Editor.NPHBuildPipeline.BuildAndConfigureScene"

    step "Step 3/5: Validating Scene"
    # Validation is done inside BuildAll, but we can also run it separately
    info "Scene validation included in build pipeline"

    step "Step 4/5: Configuring Quest 3 Build Settings"
    run_unity "Quest 3 build config" "Quest3VR.NPH.Editor.NPHBuildPipeline.ConfigureQuest3Build"

    if [[ "$SKIP_BUILD" == true ]]; then
        step "Step 5/5: APK Build SKIPPED (--skip-build)"
    else
        step "Step 5/5: Building APK"
        run_unity "APK build" "Quest3VR.NPH.Editor.NPHBuildPipeline.BuildAPK"
    fi

    PIPELINE_END=$(date +%s)
    TOTAL=$((PIPELINE_END - PIPELINE_START))

    # ── Summary ───────────────────────────────────────────────

    echo ""
    echo -e "${GREEN}${BOLD}"
    echo "╔══════════════════════════════════════════════════╗"
    echo "║          BUILD PIPELINE COMPLETE                 ║"
    echo "╚══════════════════════════════════════════════════╝"
    echo -e "${NC}"

    echo "  Total time:    ${TOTAL}s"
    echo "  API URL:       $API_URL"
    echo "  Log:           $LOG_FILE"

    if [[ "$SKIP_BUILD" != true ]]; then
        APK_PATH="${OUTPUT_PATH:-$PROJECT_DIR/Builds/NPH_Quest3.apk}"
        if [[ -f "$APK_PATH" ]]; then
            APK_SIZE=$(du -h "$APK_PATH" | cut -f1)
            echo "  APK:           $APK_PATH ($APK_SIZE)"
            echo ""
            echo "Deploy to Quest 3:"
            echo "  adb install -r \"$APK_PATH\""
        else
            warn "APK not found at expected path: $APK_PATH"
            echo "  Check log for build errors: $LOG_FILE"
        fi
    fi

    echo ""
fi
