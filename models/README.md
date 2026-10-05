# Bundled speech model

Source: https://github.com/snakers4/silero-vad
Pinned commit: 1e261b036686cd0017d500ee96acd1c4ba572a9d
File: src/silero_vad/data/silero_vad.onnx
SHA-256: 1a153a22f4509e292a94e67d6f9b85e8deb25b4988682b7e174c65279d8788e3
License: MIT, see SILERO-LICENSE.

RoomMute runs the bundled model locally using Microsoft.ML.OnnxRuntime 1.30.0,
one CPU thread, 16 kHz/512-sample windows with 64-sample context and per-instance
recurrent state. This is independent evidence from the original microphone
signal, combined conservatively with RNNoise's score. Two consecutive windows
must support speech. No model downloads are made at runtime.

The supplied user audio fixture remains outside this repository and is never
included in commits or release packages. Reproduction requires an explicit
local path: `dotnet run --project RoomMute.Tests -c Release -- --verify-noise-wave <PCM16.wav>`.

ONNX Runtime depends on Microsoft's Visual C++ runtime. The four required x64
CRT DLLs are copied unchanged from Visual Studio 2026 Community's
VC/Redist/MSVC/14.51.36231/x64/Microsoft.VC145.CRT into native/win-x64 for
app-local deployment. They are Microsoft redistributable components, not MIT.
See THIRD-PARTY-NOTICES.txt and Microsoft's redistribution list. No installer
or registry changes are needed to use these bundled dependencies.
