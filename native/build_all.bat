@echo off
REM ===========================================================================
REM  CDT-320 Vision 통합 CUDA DLL 빌드 + 자동 배포
REM
REM  실행 위치: "x64 Native Tools Command Prompt for VS 2022"
REM             (시작 메뉴 > Visual Studio 2022 > x64 Native Tools Command Prompt)
REM  실행 방법: 이 폴더(native)에서  build_all.bat   (인자 없음)
REM
REM  하는 일:
REM   1) src\*.cu 전체를 nvcc 로 한 번에 컴파일 → QmcVisionCuda.dll (정적 CRT/CUDART, 모든 GPU arch)
REM   2) 생성 DLL 을 ..\QMC.Vision\NativeDeps\ 에 3개 이름으로 복사(내용 동일, 모든 export 포함)
REM        - MakePixelShiftImage.dll  (CudaInterop : 메모리/측면라인/모폴로지/다이4변)
REM        - ColletFinderCuda.dll     (StdDevFilter)
REM        - AutoFocusCuda.dll        (AutoFocus)
REM      → QMC.Vision 빌드 시 csproj Copy 타깃이 출력 폴더로 자동 복사한다.
REM
REM  arch=all-major 가 빠지면 빌드 PC 와 다른 GPU 에서 "no kernel image" 로 조용히 실패 → 유지.
REM ===========================================================================
setlocal enabledelayedexpansion

set OUT=QmcVisionCuda.dll
set DEPLOY=..\QMC.Vision\NativeDeps
set SRCS=src\device_info.cu src\device_memory.cu src\morphology.cu src\edge_line.cu src\stddev_filter.cu src\autofocus.cu

where nvcc >nul 2>nul
if errorlevel 1 (
  echo [ERROR] nvcc 를 찾을 수 없습니다.
  echo         CUDA Toolkit 설치 후 "x64 Native Tools Command Prompt for VS 2022" 에서 실행하세요.
  exit /b 1
)

echo [1/3] nvcc 컴파일 (src\*.cu -^> %OUT%)
nvcc -O3 --shared --cudart static -arch=all-major -o %OUT% %SRCS%
if errorlevel 1 (
  echo [ERROR] 컴파일 실패. 위 에러 메시지를 확인하세요.
  exit /b 1
)

echo [2/3] 배포 폴더 확인 (%DEPLOY%)
if not exist "%DEPLOY%" mkdir "%DEPLOY%"

echo [3/3] 3개 이름으로 복사
copy /Y %OUT% "%DEPLOY%\MakePixelShiftImage.dll" >nul
copy /Y %OUT% "%DEPLOY%\ColletFinderCuda.dll"    >nul
copy /Y %OUT% "%DEPLOY%\AutoFocusCuda.dll"       >nul

echo.
echo [완료] %OUT% 빌드 및 NativeDeps 배포 완료.
echo        이제 QMC.Vision 을 빌드하면 위 3개 DLL 이 출력 폴더로 자동 복사됩니다.
echo        (DLL 이 없거나 GPU 가 없으면 모든 알고리즘이 CPU 로 폴백 — 안전)
endlocal
