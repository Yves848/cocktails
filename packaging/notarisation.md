# Notarisation Developer ID — Cocktails

Par défaut, `package-macos.sh` signe le bundle en **ad-hoc** (`SIGN_ID="-"`) : suffisant
pour un usage local (et pour les notifications natives sous l'identité « Cocktails »),
mais macOS affiche un avertissement Gatekeeper au premier lancement sur une autre machine.

Pour une distribution large **sans avertissement**, il faut **signer avec un certificat
Developer ID Application** puis **notariser** l'app auprès d'Apple. Cette procédure exige
une adhésion payante au **Apple Developer Program**.

> État actuel de la machine : seul un certificat **« Apple Development »** est présent
> (utilisable pour le développement, **pas** pour la notarisation). Il faut créer un
> certificat **« Developer ID Application »** avant de pouvoir notariser.

## 1. Prérequis (une seule fois)

1. **Certificat Developer ID Application** — créé depuis le portail développeur Apple
   (Certificates → « Developer ID Application ») puis installé dans le trousseau.
   Vérifier :
   ```bash
   security find-identity -v -p codesigning
   # → doit lister « Developer ID Application: <Nom> (<TEAMID>) »
   ```

2. **Profil d'identifiants notarytool** stocké dans le trousseau (évite de retaper les
   identifiants). Utiliser un **mot de passe d'application** (créé sur appleid.apple.com) :
   ```bash
   xcrun notarytool store-credentials cocktails \
       --apple-id "ton-apple-id@example.com" \
       --team-id "TEAMID" \
       --password "xxxx-xxxx-xxxx-xxxx"   # mot de passe d'application
   ```
   (`cocktails` est le nom du profil, réutilisé ci-dessous via `NOTARY_PROFILE`.)

## 2. Construire, signer, notariser

```bash
SIGN_ID="Developer ID Application: Ton Nom (TEAMID)" \
NOTARIZE=1 \
NOTARY_PROFILE=cocktails \
./packaging/package-macos.sh
```

Le script :
1. publie + assemble le bundle ;
2. signe avec le certificat Developer ID (**runtime durci + horodatage**, exigés) ;
3. zippe, soumet à `notarytool --wait`, puis **agrafe** le ticket (`stapler staple`) et
   valide.

Une fois agrafé, le `.app` se lance sans avertissement Gatekeeper, y compris hors ligne.

## Notes

- Sans `NOTARIZE=1`, ou avec `SIGN_ID="-"` (ad-hoc), l'étape de notarisation est
  ignorée et seule la quarantaine locale est retirée (`xattr`).
- La notarisation se fait par-dessus une **connexion réseau** vers Apple ; comptez
  quelques minutes (`--wait` bloque jusqu'au verdict).
- En cas d'échec : `xcrun notarytool log <submission-id> --keychain-profile cocktails`
  détaille les problèmes (signature manquante sur un binaire embarqué, etc.).
