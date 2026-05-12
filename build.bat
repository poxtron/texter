@echo off
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
  /nologo /target:winexe /optimize+ /out:Texter.exe ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  Texter.cs
if %errorlevel%==0 (echo Build successful.) else (echo Build failed.)
pause
