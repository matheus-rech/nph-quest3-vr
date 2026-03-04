#!/bin/bash
# Open Quest 3 VR project in Unity

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/Quest3VR_Prototype"
UNITY_PATH="/Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity"

echo "🎮 Opening Quest 3 VR Project in Unity..."
echo "Project: $PROJECT_DIR"
echo ""

if [ ! -f "$UNITY_PATH" ]; then
    echo "❌ Unity not found at: $UNITY_PATH"
    echo "Please install Unity 2022.3.20f1"
    exit 1
fi

echo "✅ Opening Unity..."
"$UNITY_PATH" -projectPath "$PROJECT_DIR" &

echo ""
echo "📖 Next steps:"
echo "   1. Wait for Unity to load"
echo "   2. Go to Tools → NPH → Pipeline: Setup Only"
echo "   3. Open VRStarterScene.unity"
echo "   4. File → Build Settings → Build"
echo ""
echo "📘 Full instructions: BUILD_INSTRUCTIONS.md"
