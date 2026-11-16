# PolarTQR

Billettkontroll-appen til PolarCode. Dørvakter skanner QR-billetter som kundene
har utstedt gjennom Borealis (`/blog_tickets`), og ser grønt, gult eller rødt på
hele skjermen.

## Slik fungerer det

Alle vakter har egen bruker:

1. **Kunden** inviterer vakten med navn og e-post, og velger hvilke arrangementer
   vakten skal ha tilgang til. Vakten får en invitasjonskode på e-post, gyldig i
   7 dager.
2. **Vakten** velger «Fått invitasjon eller glemt passord?» i appen, skriver inn
   koden og lager sitt eget passord.
3. **Etter det** logger vakten inn, velger arrangement og skanner. Innsjekk logges
   med vaktens navn.
4. **Kunden kan deaktivere vakten** eller fjerne tilgangen når som helst. Da blir
   vakten logget ut av appen innen ca. 20 sekunder. Glemt passord: kunden sender
   en ny kode.

Når vakten velger et arrangement, får appen en skanner-nøkkel som bare gjelder
**det** arrangementet. Den utløper 12 timer etter at arrangementet er slutt, og
slutter å virke med en gang kunden deaktiverer vakten eller fjerner tilgangen.

**Billettkontroll:** hold kameraet mot billetten (`PCT1:…`).

| Farge | Betyr | Vibrasjon |
|---|---|---|
| 🟩 Grønn | Sjekket inn, viser navn og billettype | kort |
| 🟨 Gul | Allerede sjekket inn, viser når og ved hvilken dør | lang |
| 🟥 Rød | Ugyldig, kansellert eller feil arrangement | to støt |
| ⬛ Grå | Ingen kontakt med serveren. Billetten er **ikke** sjekket inn | to støt |

Andre funksjoner:
- **Søk gjest:** finn en gjest på navn eller e-post og sjekk inn manuelt når
  billetten ikke er på mobilen.
- **Angre siste:** brukes ved feilskanning.
- **Lys:** slår på lommelykten.
- Telleren øverst viser antall innsjekket. Den oppdateres ved hvert skann og
  hvert 20. sekund, så den tar med innsjekk fra andre dører.
- Skjermen holdes våken mens skanneren er åpen.

Innloggingen og skanner-nøkkelen lagres i SecureStorage (Android Keystore /
iOS Keychain).

## API (Borealis)

Alle kall sender headeren `X-Scanner-Key`.

| Metode | Path | Brukes til |
|---|---|---|
| GET | `/blog_tickets/scanner/session` | Arrangement, dørnavn og statistikk |
| POST | `/blog_tickets/scanner/check_in` | `{code}` eller `{ticket_id}`. Svarer med `result`: ok, already_checked_in, cancelled, wrong_event, not_found |
| POST | `/blog_tickets/scanner/undo_check_in` | `{ticket_id}` |
| GET | `/blog_tickets/scanner/tickets?q=` | Søk etter gjest |
| GET | `/blog_tickets/scanner/stats` | Statistikk |

Vaktkonto (`Authorization: Staff <token>`):

| Metode | Path | Brukes til |
|---|---|---|
| POST | `/blog_tickets/staff/activate` | `{email, invite_code, password}`, gir token |
| POST | `/blog_tickets/staff/login` | `{email, password}`, gir token. Låses i 15 min etter 5 feil |
| GET | `/blog_tickets/staff/events` | Arrangementer vakten har tilgang til |
| POST | `/blog_tickets/staff/scanner` | `{site, event_id}`, gir skanner-nøkkel |
| POST | `/blog_tickets/staff/logout` | |

Kunden administrerer vakter i portalen (🛡 Adgangsvakter), som bruker
`POST/GET /blog_tickets/<db>/staff`, `PATCH /blog_tickets/<db>/staff/<id>` og
`POST /blog_tickets/<db>/staff/<id>/invite`.

## Bygge

Krever .NET 10 SDK med workloaden `maui-android`. iOS i tillegg krever en Mac.

```bash
dotnet workload install maui-android
cd PolarTQR
dotnet build -f net10.0-android -p:TargetFrameworks=net10.0-android -t:InstallAndroidDependencies \
  -p:AndroidSdkDirectory=<sti> -p:JavaSdkDirectory=<sti> -p:AcceptAndroidSDKLicenses=True   # første gang
dotnet build -f net10.0-android -p:TargetFrameworks=net10.0-android -t:Run                   # telefon via USB
```

Standardserveren er `https://borealis.polarcode.solutions`. Ved testing kan du
endre den nederst på innloggingssiden, under «Server».

## Struktur

```
PolarTQR/
├── App.xaml.cs              Oppstart og navigering: innlogging, arrangement, skanner
├── Models/ApiModels.cs      Svarene fra Borealis, og Flask-datoformatet
├── Services/
│   ├── ScannerApi.cs        HTTP-klient for /blog_tickets/scanner/*
│   ├── SettingsStore.cs     Nøkkel i SecureStorage, server-URL i Preferences
│   ├── CameraPermission.cs
│   └── Feedback.cs          Vibrasjon
├── ViewModels/ScanViewModel.cs   Skannelogikk, resultatvisning og angre
└── Pages/
    ├── StartupPage.cs       Sjekker lagret innlogging og nøkkel ved oppstart
    ├── LoginPage            Vaktens innlogging (startside)
    ├── ActivatePage         Invitasjonskode og eget passord
    ├── EventPickerPage      Arrangementene vakten har tilgang til
    ├── ScanPage             Kamera, teller og grønn/gul/rød visning
    └── SearchPage           Manuelt oppslag
```

## Videre

- [ ] Offline-modus: last ned gyldige koder ved oppstart, sjekk inn lokalt og
      synk når nettet er tilbake. Krever håndtering av konflikter mellom flere
      dører.
- [ ] Lyd ved skann, i tillegg til vibrasjon.
- [ ] iOS-bygg og -test på Mac.
