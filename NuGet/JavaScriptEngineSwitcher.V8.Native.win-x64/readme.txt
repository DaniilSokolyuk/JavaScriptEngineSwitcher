﻿

   --------------------------------------------------------------------------------
         README file for JS Engine Switcher: V8 for Windows x64 v3.0.0 Alpha 7

   --------------------------------------------------------------------------------

           Copyright (c) 2013-2017 Andrey Taritsyn - http://www.taritsyn.ru


   ===========
   DESCRIPTION
   ===========
   This package complements the JavaScriptEngineSwitcher.V8 package and contains
   the native implementation of V8 version 6.2.414.40 for Windows (x64).

   For correct working of the Microsoft ClearScript.V8 require `msvcp140.dll`
   assembly from the Visual C++ Redistributable for Visual Studio 2015.

   =============
   RELEASE NOTES
   =============
   1. Microsoft ClearScript.V8 was updated to version 5.5.0 (support of V8 version
      6.2.414.40);
   2. Fixed a error “When using PackageReference DLL is not copied”.

   ====================
   POST-INSTALL ACTIONS
   ====================
   If in your system does not `msvcp140.dll` assembly, then download and install
   the Visual C++ Redistributable for Visual Studio 2015
   (https://www.microsoft.com/en-us/download/details.aspx?id=53840).

   =============
   DOCUMENTATION
   =============
   See documentation on GitHub -
   http://github.com/Taritsyn/JavaScriptEngineSwitcher