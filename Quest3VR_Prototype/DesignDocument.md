# Quest 3 VR Prototype - Design Document

## Project Overview

**Name:** Quest 3 VR Prototype Package  
**Version:** 1.0  
**Platform:** Meta Quest 3 (Android)  
**Engine:** Unity 2022.3 LTS  
**Target Frame Rate:** 72 FPS

---

## Core Systems

### 1. XR Rig Architecture
- **Tracking:** 6DOF inside-out tracking
- **Rendering:** Single Pass Instanced (performance optimized)
- **Refresh:** 72Hz (Quest 3 native)
- **Space:** Room-scale with boundary support

### 2. Interaction Framework

| Feature | Implementation | Details |
|---------|---------------|---------|
| Grab | XRGrabInteractable + Custom Physics | Velocity-based throwing, dynamic attach |
| Teleport | Arc Raycaster + Fade Transition | 15m max range, layer validation |
| Haptics | XRController.SendHapticImpulse | 6 preset patterns, intensity 0-1 |
| UI | World-space Canvas + Gaze Tracking | 1.5m distance, smooth follow |

### 3. Input Mapping

**Left Controller:**
- Grip → Grab
- Thumbstick Press → Teleport Mode
- Menu Button → Dashboard Toggle
- Thumbstick → Snap Turn

**Right Controller:**
- Grip → Grab
- Thumbstick Press → Teleport Mode
- Menu Button → Dashboard Toggle
- Thumbstick → Snap Turn

---

## Technical Specifications

### Performance Targets
- Draw Calls: < 100
- Poly Count: < 50k per frame
- Texture Memory: < 256MB
- Physics Bodies: < 50 dynamic

### Optimization Settings
- Occlusion Culling: Enabled
- Static Batching: Enabled
- GPU Instancing: Enabled
- Texture Compression: ASTC 6x6
- Mesh Compression: High

### Script Execution Order
1. VRRigSetup (XR initialization)
2. VRHapticsManager (haptic queue)
3. VRGrabInteraction (input handling)
4. VRTeleportation (movement)
5. VRDashboardController (UI updates)

---

## Prefab Library

| Prefab | Purpose | Key Components |
|--------|---------|----------------|
| XR_Rig_Quest3 | Player rig | Camera, controllers, tracking |
| Grabbable_Cube | Interactive object | Rigidbody, collider, XRGrabInteractable |
| VR_Dashboard | UI system | Canvas, buttons, TMP text |
| TeleportReticle | Target indicator | Visual feedback for teleport |

---

## Scene Composition

```
VRStarterScene
├── Lighting
│   ├── Directional Light (baked shadows)
│   └── Ambient (gradient)
├── Environment
│   ├── Floor (20x20m, grid texture)
│   └── Boundary (visual only)
├── XR Rig
│   ├── Camera Offset
│   ├── Main Camera
│   ├── Left Controller (grab + teleport)
│   └── Right Controller (grab + teleport)
├── Interactables
│   ├── Grabbable_Cube (x3)
│   └── Teleport Areas
└── UI
    └── VR_Dashboard (hidden by default)
```

---

## Haptic Patterns

| Pattern | Pulses | Use Case |
|---------|--------|----------|
| Tap | 1x 0.3/0.05s | Button hover |
| Strong | 1x 0.8/0.2s | Important action |
| Double | 2x 0.5/0.08s | Selection confirm |
| Warning | 3x 0.6/0.1s | Error/caution |
| Success | 2x ramp 0.3-0.6 | Action complete |
| Error | 2x 0.8/0.2s | Invalid action |

---

## Build Configuration

**Platform:** Android  
**Minimum API:** 29 (Android 10)  
**Target API:** 33  
**Architecture:** ARM64 only  
**Scripting Backend:** IL2CPP  
**C++ Compiler Config:** Release  
**Target Devices:** Quest 3 only

---

## Future Enhancements

- Hand tracking support (Quest 3 native)
- Passthrough integration
- Multiplayer networking foundation
- Advanced gesture recognition
- Spatial anchor persistence

---

**Document Version:** 1.0  
**Last Updated:** 2024
