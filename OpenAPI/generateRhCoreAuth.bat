echo off
del /F /Q Generated\src\ReinhardHolzner.Core.Identity.AuthAPI.Generated\Controllers\*.*
del /F /Q Generated\src\ReinhardHolzner.Core.Identity.AuthAPI.Generated\Models\*.*
java -jar openapi-generator-cli.jar generate -i rhcore-auth-v1.0.0.yaml -t ./Templates -g aspnetcore -Dmodels -Dapis -c openapi-config.json -o Generated 
del /F /Q ..\Core-Identity-AuthAPI\Generated\Controllers\*.*
del /F /Q ..\Core-Identity-AuthAPI\Generated\Models\ApiException.cs
del /F /Q Generated\src\ReinhardHolzner.Core.Identity.AuthAPI.Generated\Models\UserSpec.cs
copy Generated\src\ReinhardHolzner.Core.Identity.AuthAPI.Generated\Controllers\*.* ..\Core-Identity-AuthAPI\Generated\Controllers
copy Generated\src\ReinhardHolzner.Core.Identity.AuthAPI.Generated\Models\*.* ..\Core-Identity-AuthAPI\Generated\Models
del /F /Q ..\Core-Identity-AuthAPI\Generated\Models\ApiException.cs
pause