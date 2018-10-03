echo off
del /F /Q Generated\src\ReinhardHolzner.Core.Identity.Generated\Controllers\*.*
del /F /Q Generated\src\ReinhardHolzner.Core.Identity.Generated\Models\*.*
java -jar openapi-generator-cli.jar generate -i rhcore-authapi-v1.0.0.yaml -t ./Templates -g aspnetcore -Dmodels -Dapis -c openapi-config.json -o Generated 
del /F /Q ..\Core-Identity\Generated\Controllers\*.*
del /F /Q ..\Core-Identity\Generated\Models\ApiException.cs
copy Generated\src\ReinhardHolzner.Core.Identity.Generated\Controllers\*.* ..\Core-Identity\Generated\Controllers
copy Generated\src\ReinhardHolzner.Core.Identity.Generated\Models\*.* ..\Core-Identity\Generated\Models
del /F /Q ..\Core-Identity\Generated\Models\ApiException.cs
pause