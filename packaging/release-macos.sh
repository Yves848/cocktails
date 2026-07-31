#!/usr/bin/env bash
#
# Produit l'artefact de distribution Homebrew à partir du bundle .app :
#   .app (notarisé) → Cocktails-<version>.zip + sha256, et met à jour le Cask du tap.
#
#   SIGN_ID="Developer ID Application: Nom (TEAMID)" NOTARIZE=1 ./packaging/release-macos.sh
#
# Variables : VERSION (1.0.0), SIGN_ID (- = ad-hoc), NOTARIZE (0/1),
#             NOTARY_PROFILE (cocktails), OUT (dist/), TAP_DIR (../homebrew-cocktails).
#
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$HERE/.." && pwd)"

VERSION="${VERSION:-1.0.0}"
OUT="${OUT:-$ROOT/dist}"
APP="$OUT/Cocktails.app"
ZIP="$OUT/Cocktails-$VERSION.zip"
TAP_DIR="${TAP_DIR:-$ROOT/../homebrew-cocktails}"
CASK="$TAP_DIR/Casks/cocktails.rb"

if [ "${SIGN_ID:--}" = "-" ]; then
    cat >&2 <<'WARN'
⚠  SIGN_ID est ad-hoc : l'artefact ne sera PAS notarisé (macOS affichera un
   avertissement Gatekeeper au 1er lancement sur une autre machine). Pour une
   release publique, exportez d'abord un certificat Developer ID (cf.
   packaging/notarisation.md) puis relancez :
     SIGN_ID="Developer ID Application: Nom (TEAMID)" NOTARIZE=1 ./packaging/release-macos.sh
WARN
fi

echo "==> Construction du bundle (version $VERSION)"
SIGN_ID="${SIGN_ID:--}" NOTARIZE="${NOTARIZE:-0}" \
NOTARY_PROFILE="${NOTARY_PROFILE:-cocktails}" \
VERSION="$VERSION" OUT="$OUT" "$HERE/package-macos.sh"

echo "==> Archive de distribution : $ZIP"
rm -f "$ZIP"
# ditto --keepParent produit un .zip contenant Cocktails.app à la racine (attendu
# par le Cask via `app "Cocktails.app"`), en préservant les métadonnées macOS.
ditto -c -k --keepParent "$APP" "$ZIP"

SHA="$(shasum -a 256 "$ZIP" | awk '{print $1}')"
SIZE="$(du -h "$ZIP" | awk '{print $1}')"
echo "    taille : $SIZE"
echo "    sha256 : $SHA"

if [ -f "$CASK" ]; then
    echo "==> Mise à jour du Cask : $CASK"
    sed -i '' -E "s/^  version \".*\"/  version \"$VERSION\"/" "$CASK"
    sed -i '' -E "s/^  sha256 \".*\"/  sha256 \"$SHA\"/" "$CASK"
else
    echo "⚠  Cask introuvable ($CASK) — clonez le tap à côté du dépôt ou définissez TAP_DIR." >&2
fi

cat <<EOF

Artefact prêt. Publication :
  1. Release GitLab + téléversement du zip :
       VERSION=$VERSION ./packaging/release-gitlab.sh
  2. Committer/pousser le Cask mis à jour dans le tap homebrew-cocktails.
  3. Vérifier :
       brew tap yves/cocktails https://gitlab.yg-devworks.com/yves/homebrew-cocktails.git
       brew install --cask cocktails
EOF
