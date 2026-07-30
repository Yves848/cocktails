# Reste à faire — Cocktails

Backlog vivant : on coche au fur et à mesure. Détail des commandes dans
[`homebrew-couverture.md`](analyse/homebrew-couverture.md).

Légende : `[ ]` à faire · `[~]` en cours · `[x]` fait.

## En cours

_(rien pour l'instant)_

## Manques Homebrew (priorisés)

- [ ] **`deps --tree`** — arbre de dépendances dans le détail.
- [ ] **`missing`** — dépendances manquantes, alerte Maintenance.
- [ ] **`fetch`** — préchargement (niche).
- [ ] **`vulns`** — vulnérabilités connues, info sécurité dans le détail (à considérer).

## Réglages à enrichir

_(fait — voir « Fait »)_

## Packaging / distribution

- [ ] Notarisation **Developer ID** (`SIGN_ID` + notarytool) pour distribution large.
- [ ] **Universal binary** (arm64 + x64).
- [ ] Lancement au login (option).

## Raffinements

- [ ] Auto-rafraîchir les écrans quand le moniteur détecte un changement.
- [ ] Raccourcis clavier (navigation, rechercher).

## Fait

- [x] Socle : rechercher, installer, désinstaller, mettre à jour, détail (`info`).
- [x] Mises à jour + monitoring arrière-plan + badge + notifications natives.
- [x] Maintenance : `cleanup` / `autoremove` / `doctor`.
- [x] Détail : icône (proxy favicons), « Ouvrir la page », dépendances directes.
- [x] Filtre texte + segmented Formulae/Casks + tri ; marquage des installés en recherche.
- [x] Confirmation avant désinstallation ; log `brew` en direct (auto-scroll).
- [x] Réglages persistés (`~/Library/Application Support/Cocktails/settings.json`).
- [x] Empaquetage `.app` (icône, signature ad-hoc), menu « Cocktails ».
- [x] **`update`** — bouton « Actualiser l'index » (brew update) sur Mises à jour.
- [x] **Brewfile** — export/import (`brew bundle dump`/`install`) dans Maintenance.
- [x] **Services** (`brew services`) — écran dédié : liste + démarrer/arrêter/redémarrer.
- [x] **`pin` / `unpin`** — boutons Épingler/Désépingler dans le détail Installés.
- [x] Fenêtre : boutons **réduire / agrandir** (min/max) dans l'en-tête.
- [x] **Taps** — écran dédié : lister, ajouter, retirer, faire confiance (`brew trust`).
- [x] **`uses` / `leaves`** — dépendants dans le détail Installés + filtre « Racines ».
- [x] **`reinstall`** — bouton « Réinstaller » dans le détail Installés.
- [x] **Réglages enrichis** — version/préfixe/cache réels (`brew config`/`--cache`) + toggle analytics.
- [x] **Persistance de la fenêtre** — taille/position/maximisé restaurés au démarrage.
