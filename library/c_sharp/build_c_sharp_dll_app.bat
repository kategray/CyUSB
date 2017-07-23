@echo off

echo -e "\r"
echo -e "******************************************************\r"
echo -e "***  		Building C# DLL  & Application      ***\r "
echo -e "******************************************************\r"
echo -e "\r"

echo -e "\r"
echo -e "****CyUSB solution build started for Release AnyCPU config...\r "
echo -e "\r"

%1MSBuild.exe CyUSB.sln /t:Rebuild /p:Configuration=Release >>CyUSB_Release_AnyCPU.log
if %ERRORLEVEL%==0 goto success 
if not %ERRORLEVEL%==0 goto fail 

:fail
echo -e "***Build failed for detail check  CyUSB_Release_AnyCPU.log config. check logs.***\r"
goto done

:success
echo -e "\r"
echo -e "****Success\r "
echo -e "\r"

echo -e "\r"
echo -e "****CyUSB solution build started for Debug AnyCPU config...\r "
echo -e "\r"

%1MSBuild.exe CyUSB.sln /t:Rebuild /p:Configuration=Debug >>CyUSB_Debug_AnyCPU.log
if %ERRORLEVEL%==0 goto success 
if not %ERRORLEVEL%==0 goto fail 

:fail
echo -e "***Build failed for detail check CyUSB_Debug_AnyCPU.log config. check logs.***\r"
goto done

:success
echo -e "\r"
echo -e "****Success\r "
echo -e "\r"


:done
echo -e "CyUSB solution Build completed end"