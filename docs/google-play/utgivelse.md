# Utgivelse til Google Play

## Engangsoppsett (gjort 2026-11-24 på utviklermaskinen)
Alt ligger i brukermappen, ikke i repoet:
- `%USERPROFILE%\.dotnet` – .NET 10 SDK + arbeidslasten `maui-android`
- `%USERPROFILE%\.jdk` – Microsoft OpenJDK 17
- `%USERPROFILE%\.android-sdk` – Android SDK (+ emulator og systembilde Android 15 for skjermbilder, AVD `polartqr`)
- `%USERPROFILE%\.polartqr-signing\`
  - `polartqr-upload.keystore` + `passord.txt` – **opplastingsnøkkelen**. Ta sikkerhetskopi (passordbehandler). Mistes den, må Google
    tilbakestille opplastingsnøkkelen (Play Console → App integrity), det tar noen dager.
  - `bygg.ps1` – byggeskript
  - `google-review.txt` – vaktkontoen Googles testere bruker (lim inn under *App content → App access*)

Google Play holder selve app-signeringsnøkkelen (Play App Signing). Vi signerer bare opplastingen.

## Ny versjon
1. Øk `ApplicationVersion` i `PolarTQR/PolarTQR.csproj` (heltall, må være høyere enn forrige opplasting) og eventuelt
   `ApplicationDisplayVersion` (det brukerne ser, f.eks. 1.1).
2. Bygg: `powershell -File $env:USERPROFILE\.polartqr-signing\bygg.ps1`
   → `PolarTQR\bin\Release\net10.0-android\publish\solutions.polarcode.polartqr-Signed.aab`
3. Last opp AAB-en i Play Console (Testing eller Production → Create new release) og skriv hva som er nytt.

Testinstallasjon på emulator/telefon: `bygg.ps1 -Apk` og `adb install -r <apk>`.

## Butikkside
Tekster, grafikk og svar til «App content»: se [butikkside.md](butikkside.md) og
Polaris `docs/personvern/google-play-data-safety.md`.
