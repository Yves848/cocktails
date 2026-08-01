# Cocktails 🍸

**A native macOS GUI for [Homebrew](https://brew.sh) that hides nothing.**

Cocktails puts a clear, fast interface on top of the `brew` CLI — without ever hiding
what happens underneath. Every action runs the real `brew` command and streams its
output live, so the GUI is a convenience, not a black box.

![Cocktails — installed packages](https://cocktails.yg-devworks.com/screenshots/installed.png)

> Website & screenshots: **https://cocktails.yg-devworks.com**
> Mirror of the canonical repo hosted on a private GitLab; this GitHub repo is the
> public mirror.

## Features

- **Installed / Search / Outdated** for formulae *and* casks — icon, description,
  versions, and one-click install / reinstall / pin / uninstall.
- **Batch operations** — tick several packages and upgrade or uninstall them at once.
- **Dependency tree** — the transitive `brew deps --tree` of a formula, rendered inline.
- **Services, Taps, Maintenance** — cleanup, autoremove and doctor from the UI.
- **Menu-bar agent** — checks `brew outdated` in the background and sends a native
  notification when updates land. No Dock icon; the app lives in the menu bar.
- **Transparent** — every action shows the actual `brew` command and its live output.
- **Multilingual** — English, French, Spanish, German (live language switch).

Native, signed & **notarized**, macOS 11+ (Apple Silicon).

## Install

```sh
brew tap yves/cocktails https://gitlab.yg-devworks.com/yves/homebrew-cocktails.git
brew install --cask cocktails
```

Then launch **Cocktails** from Spotlight or Launchpad — it's a menu-bar agent, so look
for the shaker icon in the menu bar.

## Build from source

Requires the .NET 10 SDK.

```sh
dotnet build                      # build the solution
dotnet run --project src/Cocktails # run the app
dotnet test                       # run the tests
./packaging/package-macos.sh      # build a signed .app bundle in dist/
```

## Architecture

A strict boundary separates the UI from Homebrew:

- **`src/Cocktails.Core`** — business layer, no Avalonia dependency. The only place that
  knows the `brew` CLI. `HomebrewService` runs brew through an injectable
  `IProcessRunner`; its output **parsers are pure, public, static functions** tested
  against captured `brew` output, so nothing shells out during tests.
- **`src/Cocktails`** — the Avalonia app (MVVM, CommunityToolkit.Mvvm). Depends only on
  `IHomebrewService`, never on `System.Diagnostics` or brew's output format.
- **`tests/Cocktails.Core.Tests`** — xUnit tests for the parsers and view-models.

Tech: **C# / .NET 10** · **[Avalonia](https://avaloniaui.net)** · MVVM.

## Status

Cocktails is in **private beta**. Want to be notified at release? Leave your address on
the [website](https://cocktails.yg-devworks.com).
