# Cocktails — couverture des commandes Homebrew

Relevé exhaustif des commandes `brew` (base : Homebrew **6.0.13**, 116 commandes) et
leur articulation dans l'application.

**Statuts** : ✅ implémenté · ◐ partiel · ⛌ à faire · ⊘ hors périmètre (GUI grand public).

**Rappel d'architecture** : toute commande passe par `IHomebrewService` → `HomebrewService`
(via `IProcessRunner`), avec une méthode de parsing statique + test quand il y a une sortie
à interpréter. La sortie temps réel remonte via `IProgress<string>` (overlay/log). Voir
`CLAUDE.md`.

---

## 1. Cycle de vie des paquets

| Commande | Rôle | Statut | Articulation dans l'app |
|----------|------|:---:|-------------------------|
| `install FORMULA\|CASK` | Installer | ✅ | `InstallAsync` · écran **Rechercher** (+ détail) · log en direct |
| `uninstall` | Désinstaller | ✅ | `UninstallAsync` · **Installés** · **confirmation** + log |
| `upgrade [NAME]` | Mettre à jour (un ou tous) | ✅ | `UpgradeAsync(name?)` · **Mises à jour** (unitaire + « Tout ») |
| `reinstall` | Réinstaller | ⛌ | Ajouter `ReinstallAsync` ; bouton dans le détail (utile après corruption) |
| `pin` / `unpin` | Figer / défiger une version | ◐ | `PackageDetails.IsPinned` **affiché** (pastille) ; **actions à câbler** (`Pin/UnpinAsync` + bouton détail) |
| `link` / `unlink` | (Dé)lier dans le prefix | ⛌ | `Link/UnlinkAsync` ; action avancée, écran détail (formulae `keg-only`) |
| `postinstall` | Rejouer le post-install | ⛌ | Niche ; `RunAsync(["postinstall", name])` au besoin |

## 2. Découverte & information

| Commande | Rôle | Statut | Articulation |
|----------|------|:---:|--------------|
| `search TEXT\|/REGEX/` | Rechercher | ✅ | `SearchAsync` · écran **Rechercher** (parse sections Formulae/Casks) |
| `info --json=v2 NAME` | Détail enrichi | ✅ | `GetInfoAsync` → `PackageDetails` · **volets de détail** (desc, homepage, deps, versions, pinned) |
| `list --versions` | Installés | ✅ | `GetInstalledAsync` (formulae + casks) · **Installés** (filtre/tri/type) |
| `outdated --json=v2` | Obsolètes | ✅ | `GetOutdatedAsync` · **Mises à jour** + **monitoring** (badge) |
| `desc` | Description courte | ✅ | Fournie par `info` (champ `desc`) — pas d'appel dédié |
| `home` / `homepage` | Ouvrir le site | ✅ | Bouton **« Ouvrir la page »** (Launcher) depuis `PackageDetails.Homepage` |
| `deps` | Dépendances | ◐ | Deps directes affichées (via `info`) ; **arbre** (`deps --tree`) et deps manquantes ⛌ |
| `uses` | Paquets qui en dépendent | ⛌ | `UsesAsync(name)` ; utile avant désinstallation → onglet « Dépendants » du détail |
| `leaves` | Formulae installées « à la racine » | ⛌ | `LeavesAsync` ; filtre « installés explicitement » sur Installés |
| `options` | Options d'install d'une formula | ⛌ | Niche ; `info --json` porte déjà les options |
| `cat` | Affiche la formule (Ruby) | ⊘ | Développeur |
| `tab` | Métadonnées d'install (ex-`INSTALL_RECEIPT`) | ⛌ | Source possible pour « installé à la demande / dépendance » |

## 3. Index & synchronisation

| Commande | Rôle | Statut | Articulation |
|----------|------|:---:|--------------|
| `update` | Met à jour la BDD des formules | ⛌ | `UpdateIndexAsync` ; bouton global + **avant** recherche/outdated (fraîcheur) |
| `update-if-needed` | Idem, conditionnel | ⛌ | Variante silencieuse pour le **monitoring** (moins coûteux) |
| `update-reset` | Réinitialise les dépôts git | ⊘ | Dépannage avancé |
| `missing` | Dépendances manquantes | ⛌ | `MissingAsync` ; alerte dans **Maintenance** |

## 4. Maintenance & diagnostic

| Commande | Rôle | Statut | Articulation |
|----------|------|:---:|--------------|
| `cleanup` | Nettoyer cache / vieilles versions | ✅ | `CleanupAsync` · **Maintenance** |
| `autoremove` | Retirer les deps orphelines | ✅ | `AutoremoveAsync` · **Maintenance** (confirmation) |
| `doctor` | Diagnostic | ✅ | `DoctorAsync` · **Maintenance** (ne lève pas sur avertissements) |
| `config` | Config de l'environnement brew | ⛌ | Bloc lecture seule dans **Réglages** (diagnostic) |
| `--prefix`/`--cellar`/`--caskroom`/`--cache`/`--repository` | Chemins | ⛌ | Infos **Réglages** (aujourd'hui seul le chemin de brew, en dur) |
| `--version` | Version de brew | ◐ | Affichée dans la barre d'état (« Homebrew 4.3.8 » de la maquette) — à brancher réellement |
| `analytics` | Télémétrie on/off | ⛌ | Toggle **Réglages** (vie privée) |

## 5. Taps (dépôts tiers)

| Commande | Rôle | Statut | Articulation |
|----------|------|:---:|--------------|
| `tap` / `untap` | Ajouter / retirer un dépôt | ⛌ | Écran **Taps** : lister (`tap`), ajouter/retirer, `tap-info` |
| `tap-info` | Détail d'un tap | ⛌ | Volet détail de l'écran Taps |
| `taps` (`--taps`) | Lister les taps | ⛌ | Liste de l'écran Taps |
| `trust` / `untrust` | (Dé)faire confiance à un tap | ⛌ | Pertinent : `brew install obs` a averti de taps non fiables — action dans l'écran Taps |
| `tap-new` | Créer un tap | ⊘ | Développeur |

## 6. Services (`brew services`)

| Sous-commande | Rôle | Statut | Articulation |
|---------------|------|:---:|--------------|
| `services list` | Lister les services | ⛌ | Écran **Services** (prévu dans la maquette) : nom, statut, utilisateur |
| `services start/stop/restart/run` | Piloter un service | ⛌ | Boutons par ligne ; parse de `services list` (colonnes Name/Status/User/File) |
| `services info` | Détail d'un service | ⛌ | Volet détail |

## 7. Brewfile (`brew bundle`)

| Sous-commande | Rôle | Statut | Articulation |
|---------------|------|:---:|--------------|
| `bundle dump` | Exporter l'installé en `Brewfile` | ⛌ | **Import/Export** : sauvegarder sa config (très utile pour migrer de machine) |
| `bundle install` | Installer depuis un `Brewfile` | ⛌ | Restaurer une config (sélecteur de fichier) |
| `bundle check` / `cleanup` | Vérifier / purger vs Brewfile | ⛌ | Écran Brewfile |

## 8. Fichiers & téléchargement

| Commande | Rôle | Statut | Articulation |
|----------|------|:---:|--------------|
| `fetch` | Pré-télécharger sans installer | ⛌ | Bouton « Télécharger » (préchargement) — niche |
| `unpack` / `extract` | Décompresser / extraire une version | ⊘ | Développeur |
| `--cache` | Chemin du cache | ⛌ | Info Réglages + taille récupérable (cf. maquette « 1,8 Go ») |
| `log` | Journal git d'une formule | ⛌ | Onglet « Historique » du détail (niche) |
| `linkage` | Vérifier les liens dynamiques | ⊘ | Diagnostic avancé |

## 9. Environnement & intégration shell

| Commande | Statut | Note |
|----------|:---:|------|
| `shellenv`, `--env`, `setup-ruby`, `command-not-found-init`, `completions` | ⊘ | Intégration shell / installation — hors GUI |
| `mcp-server` | ⊘ | Serveur MCP de brew — hors sujet |

## 10. Développement de formules (hors périmètre GUI)

`create`, `edit`, `bump*` (bump, bump-formula-pr, bump-cask-pr, bump-revision, …),
`audit`, `style`, `rubocop`, `test`, `test-bot`, `livecheck`, `readall`, `typecheck`,
`prof`, `debugger`, `irb`, `ruby`, `rubydoc`, `sh`, `sandbox-exec`, `setup-sandbox`,
`generate-man-completions`, `generate-zap`, `vendor-gems`, `install-bundler-gems`,
`update-perl-resources`, `update-python-resources`, `unbottled`, `bottle`, `pyenv-sync`,
`rbenv-sync`, `nodenv-sync`, `contributions`, `lgtm`, `gist-logs`, `docs`, `command`,
`alias`/`unalias`, `formula`/`formulae`/`casks`, `which-formula`/`which-update`, `vulns`,
`migrate`, `verify`, `version-install`, `as-console-user`, `developer`, `exec` : **⊘ hors
périmètre** — outillage de contributeur/mainteneur, sans intérêt pour un utilisateur qui
gère ses paquets.

*(Exception à considérer : `vulns` — alertes de vulnérabilités connues sur les installés —
pourrait devenir une info de sécurité dans le détail. `migrate` est automatique via `upgrade`.)*

---

## Synthèse

- **Socle complet** : rechercher, installer, désinstaller, mettre à jour, détail (info),
  obsolètes + monitoring, cleanup/autoremove/doctor. ✅
- **Manques à forte valeur** (ordre suggéré) :
  1. **`update`** avant recherche/outdated (fraîcheur de l'index) — petit, gros impact.
  2. **`brew bundle` (Brewfile)** — export/import de config, tueur pour migrer de machine.
  3. **Services** (`brew services`) — écran déjà prévu dans la maquette.
  4. **`pin`/`unpin`** — l'UI affiche déjà l'état, il ne manque que les actions.
  5. **`reinstall`** + **`uses`/`leaves`** — confort autour du détail et de la désinstallation.
  6. **Taps** (`tap`/`untap`/`trust`) — répond à l'avertissement vu sur `obs`.
- **Réglages à enrichir** : `config`/chemins réels, version de brew, toggle `analytics`.
- **Hors périmètre** : ~50 commandes de développement/interne, listées §10.
