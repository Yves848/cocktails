# Publication sur Homebrew (Cask)

Cocktails est une **app graphique** (`.app`) : elle se distribue via un **Cask**
Homebrew (et non une formule). Le Cask vit dans un **tap** dédié,
[`homebrew-cocktails`](https://gitlab.yg-devworks.com/yves/homebrew-cocktails), et
pointe vers une **release GitLab** du dépôt applicatif qui héberge le `.zip` du bundle.

```
dépôt app (yves/cocktails)                tap (yves/homebrew-cocktails)
  packaging/release-macos.sh   ─build─►   dist/Cocktails-<v>.zip + sha256
  packaging/release-gitlab.sh  ─upload─►  release GitLab v<v> (asset .zip)
                               ─maj────►  Casks/cocktails.rb (version + sha256)
utilisateur:  brew tap yves/cocktails … && brew install --cask cocktails
```

## Prérequis (une seule fois)

1. **Notarisation** — le Cask distribue un bundle **notarisé** (sinon Gatekeeper bloque
   au 1er lancement). Il faut donc un **certificat Developer ID Application** et un profil
   `notarytool`. Procédure complète : [`../notarisation.md`](../notarisation.md).
   > État machine : seul un certificat « Apple Development » est présent. Créer un
   > certificat **Developer ID Application** (adhésion Apple Developer Program) avant
   > la première release publique.

2. **Tap GitLab** — créer le dépôt public `homebrew-cocktails` (le nom **doit**
   commencer par `homebrew-`) et y pousser le contenu de `~/git/homebrew-cocktails` :
   ```sh
   # projet GitLab public (cf. ~/git/CLAUDE.md pour le token)
   TOKEN="$(printf 'protocol=https\nhost=gitlab.yg-devworks.com\n\n' | git credential fill | sed -n 's/^password=//p')"
   curl -fsS -H "PRIVATE-TOKEN: $TOKEN" -X POST \
     https://gitlab.yg-devworks.com/api/v4/projects \
     --data-urlencode "name=homebrew-cocktails" \
     --data-urlencode "visibility=public"
   cd ~/git/homebrew-cocktails
   git remote add origin https://gitlab.yg-devworks.com/yves/homebrew-cocktails.git
   git push -u origin main
   ```

## Publier une version

Depuis le dépôt applicatif (`~/git/cocktails`) :

```sh
# 1. Bundle notarisé → zip + sha256, met à jour le Cask du tap voisin
SIGN_ID="Developer ID Application: Nom (TEAMID)" NOTARIZE=1 \
VERSION=1.0.0 ./packaging/release-macos.sh

# 2. Release GitLab + téléversement du zip (URL de download stable)
VERSION=1.0.0 ./packaging/release-gitlab.sh

# 3. Publier le Cask mis à jour
cd ~/git/homebrew-cocktails
git commit -am "cocktails 1.0.0" && git push
```

Pour une **nouvelle version**, changez `VERSION` (et `CFBundleShortVersionString`,
piloté par la même variable dans `package-macos.sh`) et rejouez les trois étapes.

## Vérifier / auditer

```sh
brew tap yves/cocktails https://gitlab.yg-devworks.com/yves/homebrew-cocktails.git
brew install --cask cocktails
brew audit --cask --online cocktails     # règles Homebrew (url, sha256, verified, …)
brew style ~/git/homebrew-cocktails/Casks/cocktails.rb
```

## Notes

- **URL du Cask** : le download passe par le lien d'asset direct de la release
  (`/-/releases/v<v>/downloads/Cocktails-<v>.zip`), qui redirige vers le registre de
  paquets générique — accessible **anonymement** car le projet est public (indispensable :
  `brew` télécharge sans jeton).
- `verified: "gitlab.yg-devworks.com/yves/cocktails/"` est requis car l'hôte de l'`url`
  (gitlab) diffère de celui de `homepage` (cocktails.yg-devworks.com).
- **Soumission à homebrew-cask officiel** non visée : les casks core exigent des projets
  notables/largement diffusés. Un tap personnel est le canal adapté ici.
