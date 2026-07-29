# Cocktails — Analyse fonctionnelle

> Interface graphique moderne pour Homebrew (C# / .NET 10 / Avalonia).
> Document de cadrage : périmètre, écrans, priorités, modèle de données.
> Statut à date : le **socle technique** est en place (couche `Cocktails.Core`
> pilotant `brew`, UI de base avec liste / recherche / install / uninstall).

## 1. Vision

Permettre de gérer Homebrew **sans terminal** : voir ce qui est installé, chercher,
installer, mettre à jour, désinstaller, et être **prévenu** quand des mises à jour
sont disponibles — le tout dans une interface native, sombre et lisible.

Principe directeur : **l'UI ne fait que piloter `brew`**. Toute la logique passe par
`IHomebrewService` (cf. `CLAUDE.md`). Chaque fonctionnalité = une ou plusieurs
commandes `brew` + un parsing + un écran.

## 2. Priorisation

| Niveau | Sens |
|--------|------|
| **P0** | Socle indispensable au « MVP utilisable » |
| **P1** | Complète le socle, attendu d'un vrai gestionnaire |
| **P2** | Confort / phase 2 (monitoring, maintenance) |
| **P3** | Avancé / optionnel |

## 3. Inventaire fonctionnel

### Phase 1 — Socle (gestion de paquets)

| # | Fonctionnalité | Prio | Commande `brew` | Écran | État |
|---|----------------|------|-----------------|-------|------|
| F1 | Lister les paquets installés (formulae + casks) | P0 | `list --versions --formula/--cask` | Installés | ✅ fait |
| F2 | Rechercher un paquet | P0 | `search <q>` | Recherche | ✅ fait |
| F3 | Installer un paquet | P0 | `install <name>` | tous | ✅ back-end |
| F4 | Désinstaller un paquet | P0 | `uninstall <name>` | Installés / Détail | ✅ back-end |
| F5 | Erreurs `brew` lisibles à l'écran | P0 | — | tous | ◐ partiel |
| F6 | Détail d'un paquet (desc, version, deps, homepage, taille) | P1 | `info --json=v2 <name>` | **Détail** (à créer) | ⛌ |
| F7 | Lister les paquets obsolètes | P1 | `outdated --json=v2` | Mises à jour | ✅ fait |
| F8 | Mettre à jour un paquet | P1 | `upgrade <name>` | Mises à jour / Détail | ✅ back-end |
| F9 | Tout mettre à jour | P1 | `upgrade` | Mises à jour | ✅ back-end |
| F10 | Rafraîchir l'index (`update`) avant recherche/outdated | P1 | `update` | global | ⛌ |
| F11 | Voir la progression / log d'une commande longue | P1 | streaming stdout | **Overlay** (à créer) | ⛌ |
| F12 | Confirmation avant action destructive | P1 | — | dialog | ⛌ |
| F13 | Filtre formulae / casks + tri | P1 | — (côté UI) | Installés | ⛌ |

### Phase 2 — Avancé (monitoring & maintenance)

| # | Fonctionnalité | Prio | Commande `brew` | Écran | État |
|---|----------------|------|-----------------|-------|------|
| F14 | Monitoring actif des mises à jour (arrière-plan) | P2 | `outdated` périodique | service | ⛌ |
| F15 | Notification système « N mises à jour disponibles » | P2 | — (API notif macOS) | — | ⛌ |
| F16 | Réglages (fréquence monitoring, chemin brew, thème) | P2 | — | **Réglages** (à créer) | ⛌ |
| F17 | Nettoyage disque (`cleanup`, `autoremove`) | P2 | `cleanup` / `autoremove` | Maintenance | ⛌ |
| F18 | Diagnostic (`brew doctor`) | P3 | `doctor` | Maintenance | ⛌ |
| F19 | Épingler / débloquer une version | P3 | `pin` / `unpin` | Détail | ⛌ |
| F20 | Voir dépendances / dépendants | P3 | `deps` / `uses` | Détail | ⛌ |
| F21 | Gérer les services (`brew services`) | P3 | `services …` | Services | ⛌ |

## 4. Architecture des écrans

Passage d'une fenêtre mono-vue (barre d'outils) à une **navigation latérale**
(master-detail), plus adaptée au nombre d'écrans :

```
┌──────────────────────────────────────────────────────┐
│  Cocktails                                        ✕    │
├──────────────┬───────────────────────────────────────┤
│  ▸ Installés │  [ zone principale : liste + détail ]  │
│  ▸ Rechercher│                                        │
│  ▸ Mises à j.│   ┌─ liste ─────┐ ┌─ détail paquet ─┐  │
│  ▸ Maintenance│  │ F git   2.45│ │ nom, desc,      │  │
│  ▸ Réglages  │   │ C vscode 1.9│ │ version, deps,  │  │
│              │   │ …           │ │ [Installer]     │  │
│              │   └─────────────┘ └─────────────────┘  │
├──────────────┴───────────────────────────────────────┤
│  Barre d'état : progression + message                 │
└──────────────────────────────────────────────────────┘
```

Écrans :

1. **Installés** — liste des paquets installés, filtre formulae/casks, recherche
   locale, tri. Clic → volet **Détail**.
2. **Rechercher** — champ + résultats du dépôt, badge « installé » si déjà présent.
3. **Mises à jour** — paquets obsolètes, version actuelle → nouvelle, bouton par
   ligne + « Tout mettre à jour ».
4. **Détail paquet** (volet ou fenêtre) — description, homepage, version(s),
   dépendances, taille, actions (installer / MàJ / désinstaller / épingler).
5. **Maintenance** (P2) — nettoyage, autoremove, doctor.
6. **Réglages** (P2) — fréquence du monitoring, activation des notifications,
   chemin de `brew`, thème.
7. **Overlay de progression** (P1) — superposition affichant le log `brew` en
   direct pendant une commande longue (install/upgrade), avec possibilité d'annuler.

## 5. Modèle de données (cible)

`Package` doit s'enrichir au-delà de l'actuel (Name / Kind / versions) pour
alimenter l'écran Détail :

```
Package
  Name              string
  Kind              Formula | Cask
  InstalledVersion  string?      // null si non installé
  LatestVersion     string?
  Description       string?
  Homepage          string?      // (info)
  Dependencies      string[]     // (info / deps)
  InstallSize       long?        // (info)
  IsPinned          bool         // (info / pin)
  Tap               string?      // dépôt d'origine
  IsOutdated        => calculé
```

Source : `brew info --json=v2 <name>` fournit description, homepage, dépendances,
versions, tailles, état pinned — un seul appel enrichit le détail.

## 6. Impact technique / travaux à prévoir

- **`IHomebrewService`** : ajouter `GetInfoAsync(name)` (F6), `UpdateIndexAsync` (F10),
  `CleanupAsync` / `AutoremoveAsync` (F17), `DoctorAsync` (F18), `Pin/Unpin` (F19).
  Chaque ajout suit la règle du `CLAUDE.md` : parsing statique + test sur sortie réelle.
- **Streaming (F11)** : `IProcessRunner` ne renvoie aujourd'hui que le résultat final.
  Prévoir une surcharge exposant les lignes stdout au fil de l'eau (événement /
  `IAsyncEnumerable<string>`) pour l'overlay de progression.
- **Navigation** : introduire un shell avec navigation latérale et des ViewModels par
  écran (ex. `InstalledViewModel`, `SearchViewModel`, `OutdatedViewModel`,
  `PackageDetailViewModel`, `SettingsViewModel`). Envisager un conteneur DI léger quand
  le graphe grossit (aujourd'hui : instanciation manuelle dans `App.axaml.cs`).
- **Monitoring (F14/F15)** : tâche périodique en arrière-plan + notifications macOS.
  Décider : intégré au process UI, ou petit service séparé (cf. l'app LedControl qui
  sépare app / service).
- **Confirmations & erreurs (F5/F12)** : fenêtre de confirmation réutilisable, et
  affichage structuré des erreurs `brew` (déjà capturées via `HomebrewException`).

## 7. Jalons proposés

- **M1 — MVP utilisable (P0)** : Installés + Recherche + Install/Uninstall + erreurs
  lisibles. *(quasi atteint)*
- **M2 — Gestion complète (P1)** : Détail paquet, Mises à jour + tout-MàJ, overlay de
  progression, confirmations, `update` avant recherche, filtres/tri.
- **M3 — Monitoring (P2)** : monitoring arrière-plan + notifications + Réglages.
- **M4 — Maintenance & avancé (P2/P3)** : cleanup/autoremove/doctor, pin, deps, services.
