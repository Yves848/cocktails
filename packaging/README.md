# Packaging macOS (.app)

Construit un bundle `Cocktails.app` autonome (runtime .NET inclus), avec icône et
identité, lançable au double-clic et distribuable.

```bash
./packaging/package-macos.sh
open dist/Cocktails.app
```

Variables surchargeables : `RID` (défaut `osx-arm64`), `CONFIG` (`Release`),
`VERSION` (`1.0.0`), `OUT` (`dist/`). Sortie dans `dist/` (ignoré par git).

## Contenu

- `package-macos.sh` — `dotnet publish` self-contained + assemblage du bundle
  (`Contents/MacOS`, `Resources/Cocktails.icns`, `Info.plist`, bundle id
  `com.yg-devworks.cocktails`).
- `make-icon.py` — génère l'icône (shaker bleu sur fond sombre) via PIL, puis
  `sips` + `iconutil` produisent `Cocktails.icns` (committé, régénéré si absent).

## Limites connues

- **Non signé / non notarisé** : au premier lancement, macOS peut demander un
  clic droit → « Ouvrir » (le script retire l'attribut de quarantaine en local).
  Pour distribuer largement : signer (`codesign`) + notariser avec un compte
  développeur Apple.
- **Notifications** : elles passent par `osascript` (`display notification`), donc
  s'affichent attribuées à « Script Editor », pas à « Cocktails », même bundlé.
  Une vraie identité de notification nécessiterait l'API native
  `UNUserNotificationCenter` (interop Objective-C) — évolution possible.
