# TanjiCore
This repo has been moved to https://gitlab.com/Harble/TanjiCore, this version will no longer be maintained.
--
Because Tangerines can be web apps too, damn it.
##

# Build Instructions
```
## These instructions are for Linux, but should work on any platform
git clone https://github.com/scottstamp/TanjiCore
cd TanjiCore/TanjiCore
dotnet restore
dotnet build
## if there are errors here, please let us know

cd TanjiCore.Web
dotnet run

## Server should now be active at https://localhost:8081
```

Alternatively, just import the project with Visual Studio for local development on Windows. Change the run target from IISExpress to TanjiCore<span></span>.Web if necessary.
##

# Contributors
- [scottstamp](https://github.com/scottstamp)
- [ArachisH](https://github.com/ArachisH)
