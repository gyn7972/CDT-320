NativeDeps — QMC.Vision 네이티브 의존 DLL (출력 폴더로 자동 복사)
================================================================

ColletFinderCuda.dll
--------------------
플랫 콜렛 파인더(FlatColletFinder)의 CUDA(GPU) 백엔드.

- 이 폴더의 DLL 은 빌드 시 MSBuild 타깃 "CopyColletFinderCuda"(QMC.Vision.csproj)가
  자동으로 출력 폴더(bin\Debug, bin\Release)에 복사한다 → 매번 수동 이동 불필요.
- x64 + CUDA/CRT 정적 링크(-cudart static, /MT)라 의존 DLL 이 없다. 이 한 파일이면 충분.
- QMC.Vision 은 x64 빌드라 64bit DLL 로드 가능.
- DLL 이 없거나 CUDA GPU/드라이버가 없으면 StdDevFilter 정적 생성자에서 예외를 잡아
  자동으로 CPU 멀티스레드 경로로 폴백한다(안전, 빌드/실행에 지장 없음).

DLL 갱신(알고리즘 .cu 수정 시)
------------------------------
1) CUDA Toolkit + VS 2022 C++ 빌드도구가 설치된 PC 에서
   docs\Collet Finder\ColletFinderCuda\build.bat 실행 → ColletFinderCuda.dll 생성.
2) 생성된 ColletFinderCuda.dll 을 이 폴더(QMC.Vision\NativeDeps\)에 덮어쓴다.
3) QMC.Vision 빌드하면 출력 폴더로 자동 복사된다.

복사 우선순위(첫 번째로 존재하는 것 사용):
  1. QMC.Vision\NativeDeps\ColletFinderCuda.dll   (권장: 커밋/영구 위치)
  2. docs\Collet Finder\ColletFinderCuda\ColletFinderCuda.dll
  3. docs\Collet Finder\ColletFinderCuda\x64\$(Configuration)\ColletFinderCuda.dll
  4. docs\Collet Finder\ColletFinder\bin\$(Configuration)\ColletFinderCuda.dll
