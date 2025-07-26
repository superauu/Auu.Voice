@echo off
setlocal enabledelayedexpansion

echo ========================================
echo Speech2TextAssistant Qt C++ Build Script
echo ========================================
echo.

REM Set Qt path (users need to modify according to actual installation path)
set QT_DIR=D:\Qt\6.9.1\mingw_64
set CMAKE_PREFIX_PATH=%QT_DIR%

REM Check if Qt exists
if not exist "%QT_DIR%\bin\qmake.exe" (
    echo [ERROR] Qt installation directory not found: %QT_DIR%
    echo Please modify the QT_DIR variable in this script to your Qt installation path
    echo Example: C:\Qt\6.5.0\msvc2019_64
    echo.
    pause
    exit /b 1
)

REM Check CMake
cmake --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] CMake is not installed or not in PATH
    echo Please install CMake and add it to PATH environment variable
    pause
    exit /b 1
)

echo [OK] Qt path: %QT_DIR%
echo [OK] CMake installed
echo.

REM Create build directory
if not exist "build" (
    echo Creating build directory...
    mkdir build
)

cd build

echo Configuring project...
set MINGW_PATH=D:\Qt\Tools\mingw1310_64\bin
set PATH=%MINGW_PATH%;%PATH%
cmake .. -G "MinGW Makefiles" -DCMAKE_PREFIX_PATH="%CMAKE_PREFIX_PATH%" -DCMAKE_BUILD_TYPE=Release
if %errorlevel% neq 0 (
    echo [ERROR] CMake configuration failed
    pause
    exit /b 1
)

echo.
echo Building project...
cmake --build . --config Release
if %errorlevel% neq 0 (
    echo [ERROR] Build failed
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build successful!
echo ========================================
echo.

echo Deploying Qt dependencies...
cd build
"%QT_DIR%\bin\windeployqt.exe" bin\Speech2TextAssistant.exe
if %errorlevel% neq 0 (
    echo [WARNING] Qt deployment failed, but build was successful
    echo You may need to manually copy Qt DLLs
) else (
    echo [OK] Qt dependencies deployed successfully
)

echo.
echo ========================================
echo Build and deployment complete!
echo ========================================
echo.
echo Executable location: build\bin\Speech2TextAssistant.exe
echo.
echo To run the program:
echo cd build\bin
echo Speech2TextAssistant.exe
echo.
pause