# RNNoise bundled dependency

RNNoise 0.2, upstream tag v0.2, commit
904a876dce1f9ab8860c0a5000ed151f9f6eef58:
https://github.com/xiph/rnnoise/tree/v0.2

Source headers and C files are vendored unchanged, except the added
`src/os_support.h` compatibility shim for the scalar build. The upstream
scalar path references this absent header; the shim supplies OPUS_CLEAR.

The embedded default model is upstream model 0b50c45:
https://media.xiph.org/rnnoise/models/rnnoise_data-0b50c45.tar.gz
Archive SHA-256: 4ac81c5c0884ec4bd5907026aaae16209b7b76cd9d7f71af582094a2f98f4b43
`rnnoise_data.c` and `rnnoise_data.h` were extracted from this archive.

License: BSD-3-Clause; see rnnoise/COPYING and the individual source notices.
The generated Windows x64 DLL is committed so ordinary .NET builds need no
native compiler, model download or internet access at runtime.

Rebuild: `./native/build.ps1` with Visual Studio C++ build tools installed.
The x64 build uses baseline SSE2, not AVX, and statically links the C runtime.
The script also supports `-Runtime win-arm64` when the ARM64 C++ compiler and
runtime libraries are installed. No ARM64 binary is distributed or tested.
For ARM64 publishing, build that native DLL first; never reuse the x64 DLL.
Build objects go to ignored artifacts/native-*, not the source directories.
