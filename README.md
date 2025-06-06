# DroneConsoleApp

DroneConsoleApp is a .NET console application that connects to an ArduPilot SITL drone via MAVLink over TCP, performs automated missions including takeoff, waypoint navigation, and landing, with real-time logging support.

## Features
Connects to MAVLink-compatible drones (e.g., ArduPilot SITL)

Supports automatic takeoff, guided navigation, and landing

Real-time telemetry logging (position updates, mission status)

Timeout handling and safety checks

Clean architecture using services and interfaces

## Project Structure
DroneConsoleApp.Services

DroneController.cs — Handles low-level drone interaction (connect, takeoff, fly, land).

MissionController.cs — High-level mission runner that uses the DroneController.

DroneConsoleApp.Logging

ConsoleLoggerService.cs — Logs mission info, errors, and status to the console.

Program.cs — Main entry point to run the mission.

## How It Works
The program starts and logs the mission start.

It connects to the drone via TCP (127.0.0.1:5760).

Once a heartbeat is received and the drone is ready, it switches to GUIDED mode.

Takes off to a specified altitude.

Navigates to a specified GPS coordinate.

Lands automatically and ends the mission.

## Requirements
.NET 6.0 or higher

ASV.MAVLink Nuget Package

ArduPilot SITL running and accessible at 127.0.0.1:5760

## Example Output
```
[MISSION START] 12:00:00 — Ardu SITL Test Mission
[INFO] 12:00:01 — Starting drone connection process...
[INFO] 12:00:02 — Initializing DeviceExplorer...
[INFO] 12:00:03 — Searching for available drones...
[INFO] 12:00:04 — Drone found: Id=1
[INFO] 12:00:04 — Drone successfully assigned.
[INFO] 12:00:05 — Drone connection established successfully.
[INFO] 12:00:06 — Switching to GUIDED mode...
[INFO] 12:00:11 — Taking off to 20 meters...
[INFO] 12:00:16 — Takeoff complete. Flying to target...
[INFO] 12:00:16 — Target position: Lat=55.755800, Lon=37.617300, Alt=20.00 m
...
[INFO] 12:01:00 — Landing...
[INFO] 12:01:10 — Landed.
[MISSION END]   12:01:10 — Ardu SITL Test Mission
```
