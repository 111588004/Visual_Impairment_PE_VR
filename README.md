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

## Getting started

1. Install [Git LFS](https://git-lfs.com/) and run `git lfs install` before cloning.
2. Clone this repository.
3. Open the project with Unity Hub using editor version `6000.3.21f1`.
