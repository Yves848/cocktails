#!/usr/bin/env bash
#
# Empaquette Cocktails en bundle macOS (.app).
#   ./packaging/package-macos.sh            # Release, osx-arm64, self-contained
# Variables surchargeables : RID, CONFIG, VERSION, OUT.
#
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$HERE/.." && pwd)"

APP_NAME="Cocktails"
BUNDLE_ID="com.yg-devworks.cocktails"
RID="${RID:-osx-arm64}"
CONFIG="${CONFIG:-Release}"
VERSION="${VERSION:-1.0.0}"
OUT="${OUT:-$ROOT/dist}"

APP="$OUT/$APP_NAME.app"
PUBDIR="$OUT/publish-$RID"

echo "==> Publish ($CONFIG / $RID, self-contained)"
rm -rf "$PUBDIR"
dotnet publish "$ROOT/src/Cocktails/Cocktails.csproj" \
    -c "$CONFIG" -r "$RID" --self-contained true \
    -p:UseAppHost=true -o "$PUBDIR"

echo "==> Assemblage du bundle : $APP"
rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp -R "$PUBDIR/." "$APP/Contents/MacOS/"

# Icône (régénérée si absente et si python3 dispo).
if [ ! -f "$HERE/Cocktails.icns" ] && command -v python3 >/dev/null 2>&1; then
    echo "==> Génération de l'icône"
    python3 "$HERE/make-icon.py" "$HERE/icon_1024.png"
    rm -rf "$HERE/Cocktails.iconset" && mkdir "$HERE/Cocktails.iconset"
    while read -r sz name; do
        sips -z "$sz" "$sz" "$HERE/icon_1024.png" --out "$HERE/Cocktails.iconset/icon_${name}.png" >/dev/null
    done <<'SIZES'
16 16x16
32 16x16@2x
32 32x32
64 32x32@2x
128 128x128
256 128x128@2x
256 256x256
512 256x256@2x
512 512x512
1024 512x512@2x
SIZES
    iconutil -c icns "$HERE/Cocktails.iconset" -o "$HERE/Cocktails.icns"
    rm -rf "$HERE/Cocktails.iconset"
fi
cp "$HERE/Cocktails.icns" "$APP/Contents/Resources/Cocktails.icns"

echo "==> Info.plist"
cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>               <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>        <string>$APP_NAME</string>
    <key>CFBundleIdentifier</key>         <string>$BUNDLE_ID</string>
    <key>CFBundleExecutable</key>         <string>$APP_NAME</string>
    <key>CFBundleIconFile</key>           <string>Cocktails</string>
    <key>CFBundlePackageType</key>        <string>APPL</string>
    <key>CFBundleInfoDictionaryVersion</key> <string>6.0</string>
    <key>CFBundleShortVersionString</key> <string>$VERSION</string>
    <key>CFBundleVersion</key>            <string>$VERSION</string>
    <key>LSMinimumSystemVersion</key>     <string>11.0</string>
    <key>NSHighResolutionCapable</key>    <true/>
    <key>LSApplicationCategoryType</key>  <string>public.app-category.developer-tools</string>
</dict>
</plist>
PLIST

chmod +x "$APP/Contents/MacOS/$APP_NAME"

# Signature ad-hoc : indispensable pour que les notifications natives
# (UNUserNotificationCenter) demandent l'autorisation et s'affichent sous l'identité
# « Cocktails ». SIGN_ID surchargeable pour une vraie identité Developer ID.
SIGN_ID="${SIGN_ID:--}"
echo "==> Signature ($SIGN_ID)"
codesign --force --deep --sign "$SIGN_ID" "$APP" >/dev/null 2>&1 \
    && echo "    signé" || echo "    (signature échouée — notifications natives possiblement inactives)"

# Un bundle non signé traîne parfois l'attribut de quarantaine : on le retire en local.
xattr -dr com.apple.quarantine "$APP" 2>/dev/null || true

echo "==> OK : $APP"
echo "    Lancer : open \"$APP\""
