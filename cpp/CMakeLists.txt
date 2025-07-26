cmake_minimum_required(VERSION 3.16)
project(Speech2TextAssistant VERSION 1.0.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Find Qt6
find_package(Qt6 REQUIRED COMPONENTS Core Widgets Network Multimedia)

# Enable Qt MOC
set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTOUIC ON)
set(CMAKE_AUTORCC ON)

# Source files
set(SOURCES
    main.cpp
    MainWindow.cpp
    Models/AppSettings.cpp
    Services/ConfigManager.cpp
    Services/SpeechRecognizerService.cpp
    Services/ChatGptService.cpp
    Services/HotkeyService.cpp
    Services/OutputSimulator.cpp
    UI/RecordingOverlay.cpp
)

# Header files
set(HEADERS
    MainWindow.h
    Models/AppSettings.h
    Models/ChatGptPromptType.h
    Services/ConfigManager.h
    Services/SpeechRecognizerService.h
    Services/ChatGptService.h
    Services/HotkeyService.h
    Services/OutputSimulator.h
    UI/RecordingOverlay.h
)

# Create executable
add_executable(Speech2TextAssistant ${SOURCES} ${HEADERS})

# Link Qt libraries
target_link_libraries(Speech2TextAssistant
    Qt6::Core
    Qt6::Widgets
    Qt6::Network
    Qt6::Multimedia
)

# Windows specific libraries
if(WIN32)
    target_link_libraries(Speech2TextAssistant
        user32
        kernel32
        crypt32
    )
endif()

# Include directories
target_include_directories(Speech2TextAssistant PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/Models
    ${CMAKE_CURRENT_SOURCE_DIR}/Services
    ${CMAKE_CURRENT_SOURCE_DIR}/UI
)

# Set output directory
set_target_properties(Speech2TextAssistant PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin
)

# Copy Qt DLLs on Windows
if(WIN32)
    add_custom_command(TARGET Speech2TextAssistant POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
        $<TARGET_FILE:Qt6::Core>
        $<TARGET_FILE:Qt6::Widgets>
        $<TARGET_FILE:Qt6::Network>
        $<TARGET_FILE:Qt6::Multimedia>
        $<TARGET_FILE_DIR:Speech2TextAssistant>
    )
endif()