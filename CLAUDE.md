# CLAUDE.md

Guidance for Claude Code (and humans) working in this repo.

## What this is

A sample FastAGI server for Asterisk, written in C# on .NET 7 with the AsterNET.NetStandard 2.2.1
NuGet package (a .NET Standard build of AsterNET). The project uses the ASP.NET Core web
SDK, but the real work is a FastAGI listener on `127.0.0.1:4573` that maps the AGI script name
`Script` to the class `FastAgi.Script`, a small IVR that plays prompts and moves through a menu on
DTMF digits. The AMI (Manager) namespaces are imported but not used.

## Commands

```bash
dotnet restore
dotnet build FastAgi.sln
dotnet run --project FastAgi                           # needs the .NET 7 runtime
DOTNET_ROLL_FORWARD=Major dotnet run --project FastAgi # only a newer runtime installed (works on 8.0)
```

- The target is `net7.0`, which is out of support. A .NET 8 SDK builds it with warning NETSDK1138
  only. Without the .NET 7 runtime the app refuses to start unless roll-forward is allowed.
- There are no tests and no test project. The solution (`FastAgi.sln`) has one project.

To call it from Asterisk, add to the dialplan (`extensions.conf`) and reload:

```
exten => 200,1,AGI(agi://127.0.0.1:4573/Script)
exten => 200,n,Hangup()
```

The script name is the URL path and is case-sensitive: `agi://…/script` is answered with
"No script configured". Run `agi set debug on` in the Asterisk CLI to watch the exchange.

## Layout

```
FastAgi.sln                       solution, one project
README.md
FastAgi/
  FastAgi.csproj                  net7.0, Microsoft.NET.Sdk.Web, Nullable + ImplicitUsings on
  Program.cs                      minimal web host + checkFastAGI(): AsteriskFastAGI + GeneralMappingStrategy
  Script.cs                       FastAgi.Script : AGIScript, the IVR
  appsettings.json                Logging:LogLevel, AllowedHosts
  appsettings.Development.json    Logging:LogLevel
  Properties/launchSettings.json  "http" (localhost:5269) and "https" (localhost:7287) profiles
```

## Configuration

- The FastAGI bind address and port are hard-coded in `Program.cs`
  (`new AsteriskFastAGI(ipaddress: "127.0.0.1", port: 4573)`). Nothing in appsettings affects
  them. Because it binds to loopback, Asterisk must run on the same machine unless the address is
  changed (e.g. to `0.0.0.0`).
- Scripts are mapped in code: a `GeneralMappingStrategy` with a list of `ScriptMapping`
  (`ScriptName` = the path in the AGI URL, `ScriptClass` = the full type name). A new IVR is a new
  `AGIScript` subclass plus one more `ScriptMapping`.
- `launchSettings.json` sets `ASPNETCORE_ENVIRONMENT=Development` and the web URLs.

## Conventions / gotchas

- `agi.Start()` blocks: it runs AsterNET's accept loop, and `checkFastAGI()` is called before
  `app.Run()`. So the web host never starts, `GET /` ("Hello World!") never answers on
  5269/7287, and the browser opened by `launchBrowser` shows nothing. To serve both, the AGI
  server has to run on its own thread (e.g. a hosted service). `app.UseWebSockets()` is
  registered but no WebSocket endpoint exists.
- The commented-out block in `Program.cs` was copied from AsterNET's example and mentions
  `customivr`, `CustomIVR.cs` and `fastagi-mapping.resx`. None of these exist here.
- IVR flow (`Script.Service`): answer, play `welcome`. Digit 1 plays `press-1` and opens
  submenu 1 (4 plays `press-4`, `*` goes back to `welcome`); 2 plays `press-2` and opens submenu 2
  (5 plays `press-5`, `*` goes back); 3 plays `press-3`; any other digit plays `bad` then `digit`.
  No digit within 5 s (`WaitForDigit(5000)`) or `#` plays `goodbye` and hangs up. Digits
  `0123456789*#` interrupt playback. These are Asterisk sound file names: they must exist in its
  sounds directory for the channel's language.
- `SC511_CAUSES_EXCEPTION` and `SCHANGUP_CAUSES_EXCEPTION` are left at their defaults (the lines
  that set them are commented out).
- There is no `.gitignore`. `dotnet build` creates `FastAgi/bin/` and `FastAgi/obj/`, and Visual
  Studio creates `.vs/`; these were committed once and removed in a later commit. Don't stage
  them.
- Keep each file's encoding and line endings when editing: the `.cs` and appsettings files start
  with a UTF-8 BOM, and `FastAgi.sln` and `FastAgi.csproj` use CRLF.
