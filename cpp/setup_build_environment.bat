@echo off
chcp 65001 >nul
echo ========================================
echo Speech2TextAssistant Qt C++ Build Setup
echo ========================================
echo.

echo Checking build environment...
echo.

REM Check CMake
cmake --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] CMake not installed or not in PATH
    echo Please download and install CMake from:
    echo https://cmake.org/download/
    echo.
) else (
    echo [OK] CMake is installed
    cmake --version
    echo.
)

REM Check Qt
qmake --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Qt not installed or not in PATH
    echo Please download and install Qt from:
    echo https://www.qt.io/download-qt-installer
    echo Recommended: Qt 6.5 or higher
    echo.
    echo After installation, add Qt bin directory to PATH:
    echo Example: D:\Qt\6.9.1\mingw_64\bin
    echo.
) else (
    echo [OK] Qt is installed
    qmake --version
    echo.
)

REM Check compiler
where cl >nul 2>&1
if %errorlevel% neq 0 (
    where g++ >nul 2>&1
    if %errorlevel% neq 0 (
        echo [WARNING] No Visual Studio or MinGW compiler found
        echo Please install one of the following:
        echo - Visual Studio 2019/2022 (recommended)
        echo - MinGW (included with Qt Creator)
        echo.
    ) else (
        echo [OK] MinGW compiler is installed
        g++ --version | findstr "g++"
        echo.
    )
) else (
    echo [OK] Visual Studio compiler is installed
    cl 2>&1 | findstr "Microsoft"
    echo.
)

echo ========================================
echo Build Instructions:
echo ========================================
echo.
echo 1. Make sure all dependencies are installed
echo 2. Create build folder in this directory
echo 3. Run the following commands:
echo.
echo    mkdir build
echo    cd build
echo    cmake .. -DCMAKE_PREFIX_PATH="D:/Qt/6.9.1/mingw_64"
echo    cmake --build . --config Release
echo.
echo Note: Replace CMAKE_PREFIX_PATH with your Qt installation path
echo.
echo If you encounter issues, please refer to README.md
echo.
pause