# Strodelo Companion

A GUI for Windows designed to be used with [Strodelo Viewer](https://github.com/jiink/StrodeloViewer). This program lets you send 3D models to a Meta Quest 3 running Strodelo in mixed reality.

![image](https://github.com/user-attachments/assets/d34e7852-62f0-49d7-bb6c-a1e24da557db)

## Quick Start
1. Download the zip file from the latest (pre)release from [the Releases section](https://github.com/jiink/StrodeloCompanion/releases)
2. Extract the zip and run setup.exe
3. Launch Strodelo Companion from your Start Menu or elsewhere
4. Run the Strodelo Viewer on your Quest 3
5. Enter the pairing code / IP address that appears in the hand menu on the Quest 3 and press "Pair"
6. Drag and drop a 3D model file into the box, or click on it to select a file
7. See that the 3D model appears in the Quest

## Building from Source
This project currently uses .NET 8.0 and Windows Presentation Foundation (WPF) and is developed in Visual Studio Community 2022. The components needed to build this project may be found in the Visual Studio Installer by clicking Modify for Visual Studio 2022 and checking ".NET desktop development". The project is built like any Visual Studio .NET project.
