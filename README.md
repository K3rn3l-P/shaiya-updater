# Shaiya Updater (archived fork)

Fork of [kurtekat/shaiya-updater](https://github.com/kurtekat/shaiya-updater), a WPF-based
patcher/updater for Shaiya private servers. Archived: dormant since August 2025, no active
development.

## What this adds

Two native projects on top of the original updater shell:

- **Updater.Data / Updater.Interop** — an implementation of the SAF/SAH binary archive format
  used by the game client, with a C++/C# interop layer. `DataBuilder` packs a folder tree into an
  archive; `DataPatcher` compares entry sizes against a baseline and rewrites only the files that
  changed, backing up the target before touching it.
- **Updater.Tool / DuffToolCli** — a standalone patch-building tool ("Duff"): builds and applies
  AES-encrypted, hash-verified patches, independent of the WPF updater itself.
- A C# test suite for the configuration and patching logic (none existed upstream).

The compiled output of Updater.Tool/DuffToolCli is what
[UltimateAntiCheat](https://github.com/K3rn3l-P/UltimateAntiCheat) uses as its own
patch-distribution mechanism — this repo is where that piece was actually built.

## Environment

Windows 10, Visual Studio 2022, C# 12, WPF, .NET 8.0, .NET Framework 4.8.

## Attribution

The base updater shell, client/server configuration flow and build instructions are
[kurtekat/shaiya-updater](https://github.com/kurtekat/shaiya-updater), shared as-is by the
author, no license attached.

## Related

Same Shaiya toolchain as
[PSM_Cmd-SecureCommandChannel](https://github.com/K3rn3l-P/PSM_Cmd-SecureCommandChannel) — same
author, same period, no code shared between them.

## Security note

`AESHelper.cs` and `HashHelper.cs` both hardcode the same `aesPassword`, used to derive the AES
key that encrypts patches. It's a test value, not a secret worth keeping — but it's also the only
thing standing between a patch and anyone who wants to decrypt or forge one. If you actually use
this code, **generate your own password and don't commit it**: pull it from a config file or an
environment variable instead.

## State

Archived, no further changes planned. `Updater/Common/Constants.cs` points at the author's own
private Tailscale host — replace it with your own server before building.
