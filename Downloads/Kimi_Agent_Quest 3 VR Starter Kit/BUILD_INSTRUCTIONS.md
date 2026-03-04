# 🎮 Quest 3 VR - Quick Build Guide

## Option A: Build in Unity Editor (Easiest)

### Step 1: Open Unity
1. Open **Unity Hub**
2. Click **"Open"** → Select `Quest3VR_Prototype` folder
3. Wait for project to load

### Step 2: Setup Scene
1. In Unity, go to **Tools** → **NPH** → **Pipeline: Setup Only (No Build)**
   - This imports textures and configures the scene
2. Open scene: `Assets/Scenes/VRStarterScene.unity`

### Step 3: Configure Build
1. Go to **File** → **Build Settings**
2. Click **"Android"** → **"Switch Platform"**
3. Click **"Player Settings"**:
   - **Scripting Backend**: IL2CPP
   - **Target Architecture**: ARM64
   - **Minimum API Level**: Android 10.0 (API 29)
4. Close Player Settings

### Step 4: Build
1. In Build Settings, click **"Build"**
2. Save as: `NPH_Quest3.apk`
3. Wait for build (5-10 minutes)

### Step 5: Deploy to Quest 3
```bash
adb install -r NPH_Quest3.apk
```

---

## Option B: Use Unity's Build & Run

1. Connect Quest 3 via USB
2. In Unity: **File** → **Build And Run**
3. Select your Quest 3 device
4. Unity will build and install automatically!

---

## ✅ Pre-Configured Settings

The project is already set up with:
- ✅ XR Rig for Quest 3
- ✅ NPH System prefab
- ✅ Demo textures (auto-generated)
- ✅ All scripts compiled

Just open and build! 🚀
