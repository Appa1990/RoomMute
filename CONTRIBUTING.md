# Contributing to RoomMute

Issues and pull requests are welcome. Describe the problem, the expected behavior,
your Windows version and microphone, and how to reproduce it. For audio or connection
changes, include the relevant checks from `docs/TESTPLAN.md`.

Use .NET SDK 8 or later on Windows:

```powershell
dotnet build RoomMute.sln -c Release
dotnet run --project RoomMute.Tests -c Release --no-build
```

The test project is an executable harness, not a `dotnet test` project.
Keep German and English UI messages together in `RoomMute.Core/UiText.cs`.
Preserve manually muted microphones and external volume choices. RoomMute exchanges
coordination state only; do not add audio recording or transfer to normal operation.

Build a portable package with `./publish.ps1`. Generated binaries and local settings
are excluded from Git. Contributions are provided under the project's MIT license.
