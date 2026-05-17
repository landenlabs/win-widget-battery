@echo off

echo Build and install WinWidgetBattery

:: Kill widget is running, so it can be re-built
taskkill /IM WinWidgetBattery.exe /F 2>nul
dotnet publish WinWidgetBattery.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

:: Make directory to hold the exe and assets
mkdir c:\opt\bin\winwidgets 2>nul
xcopy /E /Y bin\Release\net10.0-windows10.0.17763.0\win-x64\publish\* c:\opt\bin\winwidgets\

echo start "" c:\opt\bin\winwidgets\WinWidgetBattery.exe > c:\opt\bin\WinWidgetBattery.bat

