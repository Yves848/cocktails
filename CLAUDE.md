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
    Les méthodes de parsing (`ParseInstalled`, `ParseSearch`, `ParseOutdated`,
    `ParseInfo`) sont **statiques et publiques** afin d'être testées sur des sorties
    `brew` capturées, sans lancer de processus.
  - `IProcessRunner` / `ProcessRunner` : abstraction de `System.Diagnostics.Process`.
    C'est le point d'injection qui rend `HomebrewService` testable — les tests
    fournissent un runner factice, la prod utilise le vrai. `RunAsync` accepte un
    `IProgress<string>?` : `ProcessRunner` lit stdout/stderr **ligne à ligne** et les
    signale au fil de l'eau (les opérations mutantes install/uninstall/upgrade le
    relaient, cf. l'`OutputLog` de `ScreenViewModel` affiché dans l'overlay).
  - `Models/Package` : record de liste (`Name`, `Kind`, `InstalledVersion`,
    `LatestVersion`, dérivés `IsInstalled` / `IsOutdated` / `KindLabel` / `KindBadge`).
    `Models/PackageDetails` : détail enrichi (`GetInfoAsync` → `brew info --json=v2`)
    avec description, homepage, dépendances, versions, pinned.
  - `HomebrewException` : levée quand une commande `brew` renvoie un code non nul.
- **`src/Cocktails`** — app Avalonia (MVVM). Ne dépend **que** de `IHomebrewService`,
  jamais de `Process` ni du format de sortie de brew.
  - `App.axaml.cs` compose la racine : `new HomebrewService(new ProcessRunner())`
    injecté dans `MainViewModel`. (Pas de conteneur DI pour l'instant — instanciation
    manuelle ; c'est le point à faire évoluer si le graphe de dépendances grossit.)
  - **Navigation** : `ViewModels/MainViewModel` est le **shell** (nav latérale
    `NavItems` + `SelectedNav` + `CurrentScreen`). Chaque écran est un
    `ScreenViewModel` (base : `IsBusy`, `StatusMessage`, `RunAsync` pour occupé/erreurs,
    `OnFirstActivatedAsync` pour le chargement paresseux). Le master-detail
    (sélection → `Details` via `GetInfoAsync`, chargement local `IsLoadingDetails`
    sans overlay global) est mutualisé dans `PackageListViewModel`. Écrans :
    `Installed` (filtre texte + segmented Formulae/Casks + tri) et `Search`
    (master-detail, volet partagé `Views/PackageDetailView` — avec icône via
    `AppIcon` et bouton « Ouvrir la page »), `Outdated`, `Maintenance` (cleanup/
    autoremove/doctor) et `Settings`. Les vues (`Views/XxxView.axaml`) sont résolues
    par le `ViewLocator` (mapping `ViewModels.XxxViewModel` → `Views.XxxView`).
  - **Confirmation & réglages** : `ScreenViewModel.RequestConfirmation` + dialogue modal
    (cf. `ConfirmationRequest`) ; `AppSettings` (instance partagée créée par le shell)
    porte p. ex. `ConfirmBeforeUninstall`. Actions destructives (désinstaller, autoremove)
    passent par une confirmation.
  - `Controls/ShakerLoader` : loader vectoriel animé (shaker) — overlay pendant les
    opérations (`CurrentScreen.IsBusy`) et splash d'ouverture (cf. `MainWindow.axaml.cs`).
  - `Controls/AppIcon` : icône du package = favicon du site (`Homepage`), récupéré via
    le **proxy `favicons.yg-devworks.com`** (contrat : `docs/proxy-favicons.md`), mis en
    cache, avec repli sur l'initiale du type. Seul point du code UI qui sort sur le réseau
    en dehors de `brew` (et il ne parle qu'à `yg-devworks.com`).
  - `Converters/StringToGeometryConverter` : parse les icônes (path SVG) à l'affichage,
    pour garder les VMs indépendants de la plateforme (testables).
  - `ViewModels/DesignHomebrewService` : stub **design-time uniquement** (previewer
    XAML et ctors sans argument des VMs) ; jamais utilisé à l'exécution réelle.
- **`tests/Cocktails.Core.Tests`** — xUnit. Parsers de `HomebrewService` + VMs d'écran.

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
