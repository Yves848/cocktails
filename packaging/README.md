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

## Notifications

En bundle `.app`, l'app utilise les **notifications natives**
(`UNUserNotificationCenter`, cf. `Services/MacUserNotifier`) : elles s'affichent sous
l'identité **« Cocktails »** (nom + icône). Le script **signe le bundle en ad-hoc**
(`codesign --sign -`), ce qui est nécessaire pour que macOS demande l'autorisation et
affiche les notifications. Au 1er lancement, macOS demande l'autorisation « Cocktails ».

Hors bundle (exécution en dev via `dotnet run`), l'app retombe sur `osascript`
(attribué à « Script Editor ») — cf. `Services/PlatformNotifier`.

## Limites connues

- **Ad-hoc, non notarisé** : au premier lancement, macOS peut demander un clic droit →
  « Ouvrir » (le script retire l'attribut de quarantaine en local). Pour distribuer
  largement : signer avec une identité **Developer ID** (`SIGN_ID=...`) + notariser.
