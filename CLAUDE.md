# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Objectif

**Cocktails** est une interface graphique moderne pour [Homebrew](https://brew.sh),
écrite en **C# / .NET 10** avec **[Avalonia](https://avaloniaui.net) 12** (UI
cross-platform, pattern MVVM via CommunityToolkit.Mvvm). L'app pilote la CLI `brew`
via une GUI : lister/rechercher/installer/mettre à jour/désinstaller des packages, et
lister les mises à jour disponibles.

## Commandes

- **Build :** `dotnet build`
- **Lancer l'app :** `dotnet run --project src/Cocktails`
- **Tests :** `dotnet test`
- **Un seul test :** `dotnet test --filter "FullyQualifiedName~ParseOutdated"`

## Architecture

Solution `Cocktails.slnx`, trois projets, avec une frontière stricte UI ↔ Homebrew :

- **`src/Cocktails.Core`** — couche métier, **sans dépendance Avalonia**. C'est le
  seul endroit qui connaît la CLI `brew`.
  - `IHomebrewService` / `HomebrewService` : opérations brew de haut niveau
    (`GetInstalled`, `Search`, `GetOutdated`, `Install`, `Uninstall`, `Upgrade`).
    Les méthodes de parsing (`ParseInstalled`, `ParseSearch`, `ParseOutdated`) sont
    **statiques et publiques** afin d'être testées sur des sorties `brew` capturées,
    sans lancer de processus.
  - `IProcessRunner` / `ProcessRunner` : abstraction de `System.Diagnostics.Process`.
    C'est le point d'injection qui rend `HomebrewService` testable — les tests
    fournissent un runner factice, la prod utilise le vrai.
  - `Models/Package` : record exposé à l'UI (`Name`, `Kind`, `InstalledVersion`,
    `LatestVersion`, propriétés dérivées `IsInstalled` / `IsOutdated`).
  - `HomebrewException` : levée quand une commande `brew` renvoie un code non nul.
- **`src/Cocktails`** — app Avalonia (MVVM). Ne dépend **que** de `IHomebrewService`,
  jamais de `Process` ni du format de sortie de brew.
  - `App.axaml.cs` compose la racine : `new HomebrewService(new ProcessRunner())`
    injecté dans `MainViewModel`. (Pas de conteneur DI pour l'instant — instanciation
    manuelle ; c'est le point à faire évoluer si le graphe de dépendances grossit.)
  - `ViewModels/MainViewModel` : commandes `[RelayCommand]` async, état `IsBusy`,
    `StatusMessage`, collection observable `Packages`. Le helper `RunAsync` centralise
    la gestion occupé/erreurs (attrape `HomebrewException`).
  - `ViewModels/DesignHomebrewService` : stub **design-time uniquement** (previewer
    XAML et ctor sans argument du VM) ; jamais utilisé à l'exécution réelle.
- **`tests/Cocktails.Core.Tests`** — xUnit. Cible surtout les parsers de `HomebrewService`.

### Conventions importantes

- **Ajouter une opération brew** = l'ajouter à `IHomebrewService`, l'implémenter dans
  `HomebrewService` via `_runner`, et si elle parse une sortie, écrire une méthode de
  parsing statique + un test avec une sortie réelle capturée.
- Le chemin de `brew` par défaut est `/opt/homebrew/bin/brew` (Apple Silicon) ;
  `/usr/local/bin/brew` sur Intel. Paramétrable via le ctor de `HomebrewService`.
- La couche UI ne doit **jamais** lancer de processus directement ni importer
  `System.Diagnostics` — tout passe par `IHomebrewService`.

## Git / versioning

Voir les conventions communes dans `~/git/CLAUDE.md` : GitLab self-hosted
(`gitlab.yg-devworks.com`, namespace `yves`) est l'unique source de vérité, dépôts
publics par défaut. Commit à chaque tâche terminée ; ne jamais push sans demande
explicite de l'utilisateur.
