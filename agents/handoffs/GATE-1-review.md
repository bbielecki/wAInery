# Gate 1 — odbiór człowieka

2026-11-01 / #10, końcowe #34 / BR-10. Fundament techniczny, nie gotowe porady.
IO po fetch/rebase wykonał demo: **177/177 PASS** (77contract+100PG/process),
zero skipped, build0warnings/errors, architecture22 oraz wire/skills PASS.
Niezależne review QA: P1/P2 zamknięte. FinalCI wymagane przed squash.
Kod/review/commity: [backlog](https://github.com/bbielecki/wAInery/issues/10),
[Decisions35](https://github.com/bbielecki/wAInery/pull/35),
[Host37](https://github.com/bbielecki/wAInery/pull/37), [Architecture38](https://github.com/bbielecki/wAInery/pull/38).

Działa:8 Contracts,4 migracje PG/pgvector/integralność, atomic outbox/inbox,
fenced jobs/retry/recovery, rzeczywisty loopback Host live/ready. Testy obejmują
CLI migrate4→replay0, rollback/dedup/stale lease, ready503/live200/recovery200,
ochronę reload i negatywne grafy zależności. Brak domenowych handlerów, weather→plan,
RAG/AI i30–50 eksperckich scenariuszy; to kolejne, niezatwierdzone gates.

## Odtwarzalne demo syntetyczne

Wymagane SDK.NET10 i lokalny Linux Docker. Przygotowanie publicznych zależności:

```powershell
dotnet restore wAInery.slnx
python -m venv .venv-contracts
.venv-contracts/Scripts/python.exe -m pip install --isolated --index-url https://pypi.org/simple -r tools/requirements-contracts.txt
powershell -NoProfile -File scripts/demo.ps1 -Gate 1 -Offline
```

Istniejącego venv nie twórz ponownie. Offline = syntetyczne integracje bez
model/weather API, nie izolacja sieci; script używa --no-restore, Docker może
pobrać brakujące obrazy. Własne DB/kontenery/procesy sprzątane; brak użytkowego
.env/volume/prywatnych dokumentów. Oczekiwany wynik:177 PASS i końcowe PASS Gate1.

## Lokalny start

PowerShell w repo, wolny127.0.0.1:5432. W .env ustaw hasło w pojedynczych
cudzysłowach, np. `POSTGRES_PASSWORD='twoje-prywatne-haslo'`; zachowują `$`.
Do Read-Host wpisz wartość bez zewnętrznych cudzysłowów.
[Reguły dotenv/escaping](https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/#env-file-syntax).
Guard chroni przed nadpisaniem .env przez zmienną terminala; nie wypisuj sekretów.

```powershell
if (Test-Path Env:POSTGRES_PASSWORD) { throw 'Uzyj terminala bez POSTGRES_PASSWORD.' }
if (-not (Test-Path infra/local/.env)) { Copy-Item infra/local/.env.example infra/local/.env }
notepad infra/local/.env
docker compose --project-name wainery-local --env-file infra/local/.env -f infra/local/compose.yaml up -d --wait postgres
dotnet build wAInery.slnx --configuration Release
$dbPassword = Read-Host 'Haslo identyczne z lokalnym .env' -AsSecureString
$dbCredential = [System.Net.NetworkCredential]::new('wainery', $dbPassword)
$dbSettings = New-Object System.Data.Common.DbConnectionStringBuilder
$dbSettings['Host'] = '127.0.0.1'
$dbSettings['Port'] = 5432
$dbSettings['Database'] = 'wainery'
$dbSettings['Username'] = 'wainery'
$dbSettings['Password'] = $dbCredential.Password
$env:WAINERY_DB_CONNECTION = $dbSettings.ConnectionString
Remove-Variable dbPassword, dbCredential, dbSettings
dotnet run --project src/Host/wAInery.Host.csproj --configuration Release --no-build --no-launch-profile -- --migrate
dotnet run --project src/Host/wAInery.Host.csproj --configuration Release --no-build --no-launch-profile
```

Drugi terminal: oba endpointy powinny zwrócić HTTP200 i Healthy.

```powershell
(Invoke-WebRequest -UseBasicParsing http://127.0.0.1:5100/health/live).Content
(Invoke-WebRequest -UseBasicParsing http://127.0.0.1:5100/health/ready).Content
```

Ctrl+C kończy Host; potem `Remove-Item Env:WAINERY_DB_CONNECTION`.
Zatrzymanie DB bez usuwania danych: `docker compose --project-name wainery-local --env-file infra/local/.env -f infra/local/compose.yaml stop postgres`.

## Checklista i ryzyka

- [ ] Demo177 PASS, brak skipped i płatnych/live integracji.
- [ ] Świeży migrate4, kolejny0; normalny Host nie migruje, health200/loopback.
- [ ] Review rollback/recovery/confirmation boundaries w testach; próby awarii tylko na syntetycznej DB, nie historii użytkownika.
- [ ] Admin zastosował i zweryfikował [ochronę main](../../infra/github/README.md) i odrębnego reviewera.
- [ ] Review człowieka oraz jawne `APPROVE GATE 1` przed Gate2; CI/cisza nie zatwierdzają.

API produktu0; koszt sesji kodowania niezmierzony. DTO nie egzekwują jeszcze
praw/budżetów/confirmation; Host bez biznesowych workerów. Health sprawdza
journal/extensions, nie pełną fizyczną bazę. Architecture sprawdza ProjectReference.
Main nadal protected=false; plugin bez adminwrite. To otwarty warunek z governance.

Learning Digest: [4 lekcje z regresjami](../knowledge/lessons.md),3 skills reused,
0 nowych/retired; oszczędności nie mierzono. B ACK Acquisition: DENY/receipt scope,
unknown/zero, ownerconfirmation i Advice/coverageaudit. C ACK Knowledge/AI:
citations/version/locality, currentrights/budget/tools, storage testy później;
AI implementuje Decisions-owned port. A/D implementerzy jeszcze nie przydzieleni.
Znane merged czyste worktrees/gałęzie sprzątnięte po SHA/granicach Temp.

Pakiet: skrypt faktycznie wykonany, AST/JSON/linki/whitespace PASS, syntetyczny
dotenv/connection-string$+semicolon+quote roundtrip PASS. Niezależny QA zamknął
P2 quoting/env precedence. Projekt/design pozostają stabilne; status w Issues/PR,
jeden handoff bramki. Następne zadania według [DAG](GATE-0-plan.md) po akceptacji.
**STOP — oczekiwanie na APPROVE GATE 1.**
