@echo off
REM Build ColletFinderCuda.dll (nvcc + MSVC).
REM Requires CUDA Toolkit (nvcc) and Visual Studio C++ build tools.
setlocal
cd /d "%~dp0"

REM 1) Locate Visual Studio (for cl.exe) via vswhere
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
  echo [ERROR] vswhere not found. Visual Studio is required.
  exit /b 1
)
REM CUDA supports VS 2017-2022 (v15-v17) host compilers; filter out newer (v18+).
"%VSWHERE%" -latest -version "[15.0,18.0)" -property installationPath > "%TEMP%\cf_vspath.txt"
set /p VSPATH=<"%TEMP%\cf_vspath.txt"
del "%TEMP%\cf_vspath.txt" >nul 2>&1
if not defined VSPATH (
  echo [ERROR] No CUDA-compatible Visual Studio 2017-2022 found.
  echo         Install VS 2022 C++ tools, or add -allow-unsupported-compiler to nvcc.
  exit /b 1
)
call "%VSPATH%\VC\Auxiliary\Build\vcvars64.bat" >nul
if errorlevel 1 ( echo [ERROR] vcvars64 failed & exit /b 1 )

REM 2) Check nvcc
where nvcc >nul 2>&1
if errorlevel 1 (
  echo [ERROR] nvcc not found. Check CUDA Toolkit install / PATH.
  exit /b 1
)

REM 3) Compile (static CUDA runtime -> no cudart DLL dependency)
REM   compute_52 PTX : broad GPU compatibility via JIT
REM   sm_89          : RTX 40 series (Ada) native
set "OUT=%~dp0ColletFinderCuda.dll"
echo [BUILD] nvcc compiling...
nvcc -O3 --shared -cudart static -Wno-deprecated-gpu-targets -o "%OUT%" "%~dp0ColletFinderCuda.cu" ^
  -gencode arch=compute_52,code=compute_52 ^
  -gencode arch=compute_89,code=sm_89 ^
  -Xcompiler "/MT /O2 /wd4819"
if errorlevel 1 ( echo [ERROR] build failed & exit /b 1 )
echo [OK] %OUT%

REM 4) Copy next to C# build outputs if present
if exist "%~dp0..\ColletFinder\bin\Debug"   copy /Y "%OUT%" "%~dp0..\ColletFinder\bin\Debug\"   >nul
if exist "%~dp0..\ColletFinder\bin\Release" copy /Y "%OUT%" "%~dp0..\ColletFinder\bin\Release\" >nul
echo [DONE]
exit /b 0
