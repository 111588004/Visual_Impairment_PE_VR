<p align="center">
  <img src="docs/images/logo.png" alt="Visual_Impairment_PE_VR logo" width="220">
</p>

<h1 align="center">Visual_Impairment_PE_VR</h1>

<p align="center">VR/AR visual impairment simulation project built in Unity (URP). Simulates various visual impairment conditions in immersive scenarios for perception/accessibility research.</p>

## Scenes

- **S1 – Stairway Profile**: Stair navigation with contrast control
- **S2 – Corridor Profile**: Corridor navigation with contrast control
- **S3 – Interior Signage Profile**: Indoor wayfinding and signage
- **VC1 – VSCS Profile (Cataract)**: Cataract simulation, Pelli-Robson contrast chart
- **VC2 – VF Profile (Glaucoma)**: Visual field loss / glaucoma simulation

## Tech stack

- Unity 6000.3.21f1 LTS, Universal Render Pipeline (URP)
- XR Interaction Toolkit + OpenXR (VIVE OpenXR)
- Git LFS for large binary assets (textures, models, lightmaps, audio, etc. — see `.gitattributes`)

## Headset / testing environment

Target device: **VIVE Focus Vision**, tested in two modes:

- **Standalone (on-device, Android build)** — runs on the headset's native VIVE OpenXR runtime. Fully working, including all post-processing / visual-effect Volumes. **Use this for any testing where the visual-effect rendering needs to be trusted.**
- **PCVR (streamed via VIVE Business Streaming + SteamVR/OpenXR)** — has a **known, unresolved rendering bug**: whenever URP post-processing (Volume) is enabled on the main camera, at least one eye renders black. Confirmed as a Unity engine regression ([Unity Discussions thread](https://discussions.unity.com/t/steamvr-doesnt-render-to-headset-when-post-processing-is-enabled/1702090), issue UUM-135831) caused by SteamVR's OpenXR runtime not supplying a visibility mesh that URP's post-processing pass requires; not specific to this project. No camera-architecture workaround was found (tested: with/without Camera Stack, independent Base cameras, MultiPass vs Single Pass Instanced, switching the system OpenXR runtime, and a community-provided native patch — all either failed to fix it or broke something else). Only a rewrite of the post-processing effects to avoid URP's Volume pipeline entirely (e.g. a custom overlay shader, as already used for the S3/glaucoma simulation) would avoid this, which has not been done because it would require re-validating the visual output against the published/clinically-validated simulation.

**Practical guidance:** if you're evaluating or demoing the visual-effect simulation over PCVR, either disable post-processing on the main camera first, or use the standalone on-device build instead.

## Getting started

1. Install [Git LFS](https://git-lfs.com/) and run `git lfs install` before cloning.
2. Clone this repository.
3. Open the project with Unity Hub using editor version `6000.3.21f1`.
