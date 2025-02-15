```
 ____  ____  ____    __  __ _              _   ____            _ _ _         
/ ___|| __ )| __ )  |  \/  (_)_  _____  __| | |  _ \ ___  __ _| (_) |_ _   _ 
\___ \|  _ \|  _ \  | |\/| | \ \/ / _ \/ _` | | |_) / _ \/ _` | | | __| | | |
 ___) | |_) | |_) | | |  | | |>  <  __/ (_| | |  _ <  __/ (_| | | | |_| |_| |
|____/|____/|____/  |_|  |_|_/_/\_\___|\__,_| |_| \_\___|\__,_|_|_|\__|\__, |
                                                                       |___/
```

![SBB Mixed Reality](README.assets/mr-sbb-splash-image.png)

# Contents

<!-- TOC -->
* [Contents](#contents)
* [About](#about)
* [Development “Quick”start](#development-quickstart)
  * [Requirements and Set-up](#requirements-and-set-up)
    * [:computer: Operating System](#computer-operating-system)
    * [:video_game: Unity](#video_game-unity)
    * [:wrench: Visual Studio](#wrench-visual-studio)
    * [:cloud: Microsoft Azure](#cloud-microsoft-azure)
      * [Setting up Azure Object Anchors](#setting-up-azure-object-anchors)
      * [Setting up Azure AI Visions](#setting-up-azure-ai-visions)
    * [:camera: 3D Object Capture](#camera-3d-object-capture)
  * [Project Setup](#project-setup)
    * [Project Structure](#project-structure)
  * [Components](#components)
    * [Object Tracking](#object-tracking)
      * [Adding New AOA Models](#adding-new-aoa-models)
    * [OCR](#ocr)
    * [UI](#ui)
  * [Deploying](#deploying)
    * [Build Step 1: Build Unity Project](#build-step-1-build-unity-project)
      * [First-time Setup](#first-time-setup)
      * [Build](#build)
    * [Build Step 2: Build Visual Studio Solution](#build-step-2-build-visual-studio-solution)
      * [First-time Setup](#first-time-setup-1)
      * [Build](#build-1)
    * [Build Step 3: Deploy to HoloLens](#build-step-3-deploy-to-hololens)
* [File Downloads](#file-downloads)
* [Contact](#contact)
<!-- TOC -->

# About

**Project Title:** SBB: Preventive/Predictive Maintenance with AR Assistant

**Abstract:** This project presents a comprehensive mixed reality application
tailored for train door maintenance, utilizing the spatial computing
capabilities of the Microsoft HoloLens 2. We integrate several software packages
to provide a mixed reality application featuring: 1) Train door identification
via its unique label, which is scanned using OCR technology provided by Azure AI
Vision; 2) Accurate tracking of physical objects in 3D space using Azure Object
Anchors, and subsequent overlaying of a 3D model, or “digital twin”; 3) An
intuitive user interface for displaying repair manuals and a checklist to track
maintenance progress using the Mixed Reality Toolkit (MRTK3). We additionally
compare various approaches for 3D model tracking and evaluate respective
strengths and weaknesses when deployed to the Microsoft HoloLens 2. Finally, we
evaluate the effectiveness of the application using a comprehensive user study.

# Development “Quick”start

## Requirements and Set-up

### :computer: Operating System

Development can only be done on Windows 10 or newer due to the reliance on .NET
products and other Microsoft services.

### :video_game: Unity

The project is set up for Unity version **2023.1.14f1**. Other versions might
work, but the project must be ported manually. Additionally, use
the [Mixed Reality Feature Tool](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/welcome-to-mr-feature-tool)
to install the following additional Unity packages:

- Azure Mixed Reality Services &rarr; Microsoft Azure Object Anchors
- MRTK3 (all components)
- Platform support &rarr; Mixed Reality OpenXR Plugin

Optionally, you can also install support for Holographic Remoting:

- [HoloLens 2 Holographic Remoting Unity package](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/native/holographic-remoting-overview)

### :wrench: Visual Studio

A recent version of Visual Studio is required. The following additional
components need to be installed:

**Workloads:**

- .NET desktop development
- Desktop development with C++
- Universal Windows Platform development
- Game development with Unity

**Individual components:**

- Windows 10 SDK _or_ Windows 11 SDK (untested)
- \[Optional\] ARM64 Remote Debugger

> :warning: **Storage space requirements** :floppy_disk:
>
> The full installation for Unity +
> Visual Studio (including components) takes up about 70 GB of disk space when
> installed. Builds and projects incur an additional storage cost. A minimum of
> 100 GB free storage space are recommended.

### :cloud: Microsoft Azure

We also use two Azure services:

- **Azure Object Anchors**, for 3D model tracking
- **Azure AI Vision**, for OCR

It is required to set up those services on your own Azure account and generate
API keys which you can then use to make requests. Students get US$200 of free
Azure credit that can be used for the project. Note that especially Azure AI
Vision is subject to rate and monthly request limits.

#### Setting up Azure Object Anchors

Please first follow this
guide: [Instructions for Azure Object Anchors](https://learn.microsoft.com/en-us/azure/object-anchors/quickstarts/get-started-model-conversion).

In the Unity project, under `Assets/AzureObjectAnchors.SDK/Resources` edit
the `ObjectAnchorsConfig` ScriptableObject to contain your newly generated API
keys.

#### Setting up Azure AI Visions

Please first follow instructions on this
webpage: [Instructions for Azure AI Vision](https://learn.microsoft.com/en-us/azure/ai-services/computer-vision/overview-ocr).

In the Unity project, open the Script `Assets/Scripts/OCR/OCRManager.cs` and
edit the `Endpoint` and `APIKey` constants to your own credentials.

### :camera: 3D Object Capture

We used [PolyCam](https://poly.cam/) for scanning of 3D models using our mobile
phones. On Android, you have 20 free scans after creating an account. The
quality is sufficient for our purposes.

We then clean up the models in [Blender 3D](https://www.blender.org/). Note the
difference in coordinate systems when exporting from Blender into Unity: *
*Blender** uses a **right-handed, z-up** coordinate system while **Unity** uses
a **left-handed, y-up** coordinate system. We have found the **GLB** format most
suitable for export from Blender as it can be used for

- Azure Object Anchor model conversion, includes textures
- Importing into Unity for visualization

When exporting from Blender to GLB, you can check the `Y-axis up`option to make
it conform to Unity’s coordinate system.

## Project Setup

After installing all requirements, packages, and dependencies, open the Unity
project from source and wait for all packages to finish importing. This can take
a while.

### Project Structure

The project is structured as standard Unity application. All user files are
stored in the `Assets` directory and organized into file type/functionality.
Important directories include:

| Directory | Purpose                                                        |
|-----------|----------------------------------------------------------------|
| Materials | Unity materials for rendering                                  |
| Models    | Raw 3D models imported from Blender                            |
| Prefabs   | Unity prefabs                                                  |
| Scenes    | Unity scenes                                                   |
| Scripts   | C# scripts                                                     |
| Textures  | 2D image texture assets for use in materials or other purposes |

Other directories might be system-generated and generally should not be
modified.

## Components

### Object Tracking

Object tracking is managed with the Azure Object Anchors SDK and the main entry
point is found in `ObjectTrackingManager.cs` on the similarly named GameObject.
Properties and methods are documented in the source code.

In order for AOA to work, you need to set your API credentials as mentioned in
the Set-up section.

#### Adding New AOA Models

In order to track your own models, you first need to preprocess them with AOA,
and then copy the trained model to the HL.

1. Follow the
   steps [detailed here](https://learn.microsoft.com/en-us/azure/object-anchors/quickstarts/get-started-model-conversion)
   to convert your 3D model/scan to an AOA model. Supported file formats
   are `fbx`, `ply`, `obj`, and `glb`. We recommend using `glb`.
2. Connect your HL via USB to your PC. You might need to enable additional
   options (
   cf. [Developer Mode](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/advanced-concepts/using-visual-studio?tabs=hl2))
   on the HL before copying files to/from the HL is supported.
3. Copy the newly created `.ou` files (AOA models) to the `3D Objects` library
   folder on the HoloLens. The application will automatically scan this
   directory for any files ending in `.ou` and register them as trackable
   objects.
4. Import the visualization mesh (CAD model) into Unity. Open
   the `ObjectVisualization` prefab and add a new entry to
   the `ModelSwitcher.visualizationPrefabs` component from within the Unity
   editor. The `name` must match the filename of the `.ou` file (without
   extension). Once this AOA model is detected, the corresponding visualization
   mesh will be displayed.

### OCR

For OCR we use the Azure AI Vision REST APIs using Unity’s built-in HTTP client.
The main file is `OCRManager.cs`on the similarly named GameObject. Properties
and methods are documented in the source code.

### UI

We use MRTK3 for all of our UI components. This is motivated by the fact that
the available components are tested from a UX perspective to be optimal for MR
applications. All UI components are grouped under the “UI” GameObject. The app
currently contains the following UI components:

- **HandMenuBase:** Contains all buttons for main functionality, as well as a
  few sub-menus:
    - Manage tracked objects
    - General notifications (last scanned door label)
    - Tracking status
- **PDFViewer:** The in-game PDF viewer.
- **CheckListMenu:** In-game, door-specific checklists
- **DebugPlaque:** Used for testing OCR in the Unity editor without having to
  build and deploy

## Deploying

Deploying is essentially a three-step process. The steps are detailed below.

### Build Step 1: Build Unity Project

#### First-time Setup

First follow the instructions on how to properly configure an MRTK3 project in
Unity: <https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk3-overview/getting-started/setting-up/setup-new-project>.
Follow the “UWP” route. Most importantly, set the build platform to:

- **Platform:** Universal Windows Platform
- **Architecture:** ARM 64-bit
- Remaining settings at their defaults

Under **Player Settings**, make sure that the following capabilities are
enabled:

- InternetClient
- InternetClientServer
- PicturesLibrary
- PrivateNetworkClientServer
- Webcam
- Microphone
- Objects3D
- SpatialPerception
- GazeInput

**Supported devices** should be **Holographic**.

Ideally, these settings are already set when cloning the project from version
control.

#### Build

When you want to create a new build, in Unity,
select `File > Build Settings > Build`. If successful, this should produce a
Visual Studio solution.

### Build Step 2: Build Visual Studio Solution

Open up the newly created Visual Studio solution.

#### First-time Setup

In the top bar, make sure that **Release** and **ARM64** are selected.
Additionally, you want to change Local Machine to **Remote Machine**. This will
allow you to deploy to the HoloLens. Then follow the steps below to connect the
solution to your HL:

1. On your HoloLens, shout “**What is my IP**” – Note that “What’s my IP” will
   not work (seriously)
2. Click the small arrow next to Remote Machine, and then select **SBB Mixed
   Reality MRTK3 Debug Properties** and select the  **Debugging**subsection.
3. Paste your HL’s IP into the **Machine Name** field. Press “OK” to apply.

You may have
to [enable developer mode](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/advanced-concepts/using-visual-studio?tabs=hl2)
before being able to deploy to the HL over WiFi.

#### Build

Build the solution by clicking `Build > Build Solution` or by using <kbd>
Ctrl</kbd><kbd>Shift</kbd><kbd>B</kbd>. The first build can take up to 30
minutes, but will be faster (around 1-10 minutes) on subsequent builds.

### Build Step 3: Deploy to HoloLens

Once the solution is built, turn on your HL and unlock it. Make sure your
PC/laptop are connected to the same network, and press **Start without
Debugging**
![Start without Debugging](./README.assets/start-without-debugging.png)in
Visual Studio. This should automatically upload the code to your HL and start
the application. This process takes usually around 2 minutes (depending on the
executable size).

# File Downloads

If you want to re-use our files for 3D models, Azure Object Anchor models, or
installer/package files, you can find a collection of such files in this shared
folder:

- [PolyBox File Downloads](https://polybox.ethz.ch/index.php/s/12Hbol0WzZUxl9m)

You will need a valid ETHZ account to access these files.

# Contact

- [Federico Mantovani](mailto:fmantova@ethz.ch)
- [Leonardo Salsi](mailto:salsil@ethz.ch)
- [Diego de los Santos Gausí](mailto:dgausi@ethz.ch)
- [Luca Vögeli](mailto:lvoegeli@ethz.ch)

**Citation:**

```
@misc{Mantovani_Salsi_de_los_Santos_Gausí_Vögeli_2024, 
  title={SBB: Preventive/Predictive Maintenance with AR Assistant},
  url={https://gitlab.ethz.ch/mr-23/sbb-mixed-reality},
  journal={ETHZ - Mixed Reality Fall Semester 2023},
  author={Mantovani, Federico and Salsi, Leonardo and de los Santos Gausí, Diego and Vögeli, Luca},
  year={2024}, 
  month={Jan}
} 
```
