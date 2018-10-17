echo off
del /F /Q Generated\src\HCore.Identity.Generated\Controllers\*.*
del /F /Q Generated\src\HCore.Identity.Generated\Models\*.*
java -jar openapi-generator-cli.jar generate -i hcore-authapi-v1.0.0.yaml -t ./Templates -g aspnetcore -Dmodels -Dapis -c openapi-config.json -o Generated 
del /F /Q ..\HCore-Identity\Generated\Controllers\*.*
del /F /Q ..\HCore-Identity\Generated\Models\ApiException.cs
copy Generated\src\HCore.Identity.Generated\Controllers\*.* ..\HCore-Identity\Generated\Controllers
copy Generated\src\HCore.Identity.Generated\Models\*.* ..\HCore-Identity\Generated\Models
del /F /Q ..\HCore-Identity\Generated\Models\ApiException.cs
pause