#!/usr/bin/env bash
#
# Crée (ou met à jour) la release GitLab de la version courante et téléverse le zip
# comme asset téléchargeable : registre de paquets générique + lien d'asset direct,
# ce qui donne l'URL stable utilisée par le Cask :
#   https://gitlab.yg-devworks.com/yves/cocktails/-/releases/v<version>/downloads/Cocktails-<version>.zip
#
#   VERSION=1.0.0 ./packaging/release-gitlab.sh
#
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
HOST="gitlab.yg-devworks.com"
PROJECT="yves%2Fcocktails"          # namespace/nom, URL-encodé
API="https://$HOST/api/v4"

VERSION="${VERSION:-1.0.0}"
ZIP="${ZIP:-$ROOT/dist/Cocktails-$VERSION.zip}"
TAG="v$VERSION"
FILE="Cocktails-$VERSION.zip"

[ -f "$ZIP" ] || { echo "Archive absente : $ZIP (lancez d'abord release-macos.sh)." >&2; exit 1; }

# Token API depuis le trousseau macOS (cf. ~/git/CLAUDE.md).
TOKEN="$(printf 'protocol=https\nhost=%s\n\n' "$HOST" | git credential fill | sed -n 's/^password=//p')"
[ -n "$TOKEN" ] || { echo "Token GitLab introuvable dans le trousseau." >&2; exit 1; }
AUTH=(-H "PRIVATE-TOKEN: $TOKEN")

echo "==> Téléversement dans le registre de paquets générique ($FILE)"
PKG="$API/projects/$PROJECT/packages/generic/cocktails/$VERSION/$FILE"
curl -fsS "${AUTH[@]}" --upload-file "$ZIP" "$PKG" >/dev/null
DL="https://$HOST/api/v4/projects/$PROJECT/packages/generic/cocktails/$VERSION/$FILE"

echo "==> Création de la release $TAG"
# --fail-with-body : si la release existe déjà (409), on l'indique sans planter le flux.
if ! curl -fsS "${AUTH[@]}" -X POST "$API/projects/$PROJECT/releases" \
        --data-urlencode "name=Cocktails $VERSION" \
        --data-urlencode "tag_name=$TAG" \
        --data-urlencode "ref=main" \
        --data-urlencode "description=Cocktails $VERSION — bundle macOS (.app) notarisé." \
        --data-urlencode "assets[links][][name]=$FILE" \
        --data-urlencode "assets[links][][url]=$DL" \
        --data-urlencode "assets[links][][direct_asset_path]=/$FILE" \
        --data-urlencode "assets[links][][link_type]=package" >/dev/null 2>&1; then
    echo "    (la release $TAG existe peut-être déjà — vérifiez sur GitLab.)" >&2
fi

echo "==> Publié. URL stable (utilisée par le Cask) :"
echo "    https://$HOST/yves/cocktails/-/releases/$TAG/downloads/$FILE"
