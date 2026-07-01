@echo off
REM ===========================================================================
REM  CDT-320 Vision - Build unified CUDA DLL and deploy 3 names
REM
REM  Run in : "x64 Native Tools Command Prompt for VS 2022"
REM  Usage  : run this from the native folder ->  build_all.bat   (no args)
REM
REM  Steps:
REM   1) nvcc compiles all src\*.cu -> QmcVisionCuda.dll (static CRT/CUDART, all arch)
REM   2) copy the DLL to ..\QMC.Vision\NativeDeps\ under 3 names (identical content):
REM        - MakePixelShiftImage.dll  (CudaInterop : memory / side line / morphology / die edges)
REM        - ColletFinderCuda.dll     (StdDevFilter)
REM        - AutoFocusCuda.dll        (AutoFocus)
REM      QMC.Vision build then auto-copies them to the output folder (csproj targets).
REM
REM  Keep -arch=all-major : without it the kernel fails silently ("no kernel image")
REM  on a GPU different from the build PC.
REM  -Xcompiler /wd4819 : silence C4819 warnings from CUDA headers on CP949 (harmless).
REM
REM  NOTE: this .bat is ASCII-only on purpose. cmd reads batch files in the OEM code
REM        page (949 on Korean Windows), so non-ASCII text here would be mangled.
REM ===========================================================================
setlocal

set OUT=QmcVisionCuda.dll
set DEPLOY=..\QMC.Vision\NativeDeps
set SRCS=src\device_info.cu src\device_memory.cu src\morphology.cu src\edge_line.cu src\stddev_filter.cu src\autofocus.cu

where nvcc >nul 2>nul
if errorlevel 1 (
  echo [ERROR] nvcc not found. Install CUDA Toolkit and run from
  echo         "x64 Native Tools Command Prompt for VS 2022".
  exit /b 1
)

echo [1/3] nvcc compile ^(src\*.cu -^> %OUT%^)
nvcc -O3 --shared --cudart static -arch=all-major -Xcompiler /wd4819 -o %OUT% %SRCS%
if errorlevel 1 (
  echo [ERROR] compile failed. See messages above.
  exit /b 1
)

echo [2/3] ensure deploy folder ^(%DEPLOY%^)
if not exist "%DEPLOY%" mkdir "%DEPLOY%"

echo [3/3] copy to 3 names
copy /Y "%OUT%" "%DEPLOY%\MakePixelShiftImage.dll" >nul
copy /Y "%OUT%" "%DEPLOY%\ColletFinderCuda.dll"    >nul
copy /Y "%OUT%" "%DEPLOY%\AutoFocusCuda.dll"       >nul

echo.
echo [DONE] %OUT% built and deployed to NativeDeps.
echo        Build QMC.Vision next; the 3 DLLs are auto-copied to the output folder.
echo        (No DLL or no GPU -^> all algorithms fall back to CPU, which is safe.)
endlocal
