# Gate 1 — pakiet do odbioru człowieka

2026-11-01; #10, końcowe #34. Fundament techniczny, nie gotowy system doradczy.
Gate0 zatwierdzony przez użytkownika. Gate1 oczekuje review; Gate2 nie rozpoczęty.
Stan kodu przed końcowym PR:73ef3c1,177/177 PASS,77 kontraktowych i100 rzeczywistych
PG/procesowych, zero skipped. Lokalne finalne demo wykonane:177/177 PASS,
build0/0, architecture22, wire/skills PASS, exit0. Syntax/odmowa Gate2 i lokalne
linki Markdown PASS. Niezależny qa_g1_01 finalreview READY FOR PR: brak otwartych
P0/P1/P2 w zmianach; reviewer nie uruchamiał demo. CI końcowego PR pending.

## Co działa i jak to udowodniono

| Zachowanie | Odtwarzalny dowód / wynik oczekiwany |
|---|---|
| Publiczne porty8 kontekstów, scope/UTC/unknown/proweniencja/budżety | Contracts suite; niezgodne workspace i v1 odrzucone, nieznane wartości nie stają się zerem. DTO nie wykonują autoryzacji ani oceny naukowej. |
| Cztery migracje, pgvector, integralność historii/occupancy/definicji | MigrationTests/IntegrityTests: CLI `Applied 4 migration(s).`, replay0; overlap/cross-workspace/mutacja odrzucone; zły upgrade rollback bez częściowej publikacji. |
| Atomowy outbox, inbox i fanout | OutboxAtomicityTests/OutboxDispatcherTests: rollback efektów/inboxów przy awarii, dedup/retry/DEAD, anulowanie caller vsconsumer; konsument tylko krótkie efekty DB. |
| Durable jobs, coalescing, fencing i recovery | JobQueueTests: token+revision+expiry po locku, dwie DB, brak publikacji starego lease, bounded retry i nowy desired input. |
| Rzeczywisty Host | HostReadinessTests: błędny startup exit1 bez sekretów; dynamiczny loopback; live/ready200, błąd SQL/journal→ready503/live200, naprawa→ready200; brak dodatkowego listenera po reload. |
| Granice projektów | ArchitectureBoundaryTests: poprawny graf0, Contracts→Persistence/obca implementacja/cykl→1 z FAIL. To kontrola ProjectReference, nie pełny audyt każdego pakietu/API. |

Nie wykonano: rzeczywistych poleceń domenowych, prognoz/predykcji/rankingu/planów,
OCR/RAG/providerów AI/weather, pomiaru kosztu zaakceptowanego planu ani30–50
eksperckich scenariuszy. To późniejsze bramki, nie pominięte testy gotowego produktu.

## Odtwarzalne demo syntetyczne

Wymagane SDK.NET10, uruchomiony lokalny Linux Docker, przygotowane zależności.
Pierwsze przygotowanie może pobierać publiczne pakiety/obrazy; bez klucza AI,
prywatnych książek i danych winnicy. Repo NuGet.Config izoluje personal feeds.

```powershell
dotnet restore wAInery.slnx
python -m venv .venv-contracts
.venv-contracts/Scripts/python.exe -m pip install --isolated --index-url https://pypi.org/simple -r tools/requirements-contracts.txt
powershell -NoProfile -File scripts/demo.ps1 -Gate 1 -Offline
```

Istniejącego venv nie trzeba tworzyć ponownie. `-Offline` oznacza syntetyczne
integracje bez płatnych modeli i live weather; nie izolację sieci. Script używa
--no-restore; Docker/Testcontainers może pobrać brakujące obrazy. Dane i hasła
fixture generowane lokalnie; własne kontenery/bazy/procesy są sprzątane. Nie używa
użytkowego .env/volume ani prawdziwej historii. Oczekiwane: build0warnings/errors,
architecture22 PASS, schema/negative/skill PASS,177/177 PASS, końcowe PASS Gate1.
Demo nie zatwierdza następnej bramki. Pojedyncze regresje: polecenia w handoffach
PR poniżej; MTP wymaga `dotnet test --solution`, nie pozycyjnego sln argumentu.

## Lokalne uruchomienie aplikacji

PowerShell z katalogu repo. Przy pierwszym użyciu otwórz .env i ustaw prywatne
hasło; istniejącego pliku nie nadpisujemy. Wymagany wolny127.0.0.1:5432.
W .env użyj pojedynczych cudzysłowów: `POSTGRES_PASSWORD='twoje-prywatne-haslo'`;
zachowują literalny `$`. W Read-Host wpisz wartość hasła bez zewnętrznych
cudzysłowów pliku. [Reguły dotenv/escaping](https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/#env-file-syntax).
Zmienna POSTGRES_PASSWORD terminala ma pierwszeństwo przed .env: guard poniżej
odmawia pracy, zamiast niejawnie zmieniać wybrane hasło. Użyj terminala bez tej
zmiennej; nie wypisuj jej wartości.

```powershell
if (Test-Path Env:POSTGRES_PASSWORD) { throw 'Uzyj terminala bez POSTGRES_PASSWORD, aby Compose uzyl lokalnego .env.' }
if (-not (Test-Path infra/local/.env)) { Copy-Item infra/local/.env.example infra/local/.env }
notepad infra/local/.env
docker compose --project-name wainery-local --env-file infra/local/.env -f infra/local/compose.yaml up -d --wait postgres
dotnet restore wAInery.slnx
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

Nie wypisuj zmiennej połączenia. Drugi terminal:

```powershell
(Invoke-WebRequest -UseBasicParsing http://127.0.0.1:5100/health/live).Content
(Invoke-WebRequest -UseBasicParsing http://127.0.0.1:5100/health/ready).Content
```

Oba `Healthy`, HTTP200. Ctrl+C kończy Host. Po pracy w jego terminalu:
`Remove-Item Env:WAINERY_DB_CONNECTION`. Zatrzymanie lokalnego serwisu bez kasowania
danych: `docker compose --project-name wainery-local --env-file infra/local/.env -f infra/local/compose.yaml stop postgres`.
Nie używaj usuwania volume do naprawiania konfiguracji/hasła istniejącej bazy.

## Checklista manualna

- [ ] Demo kończy się177 PASS i bez skipped, bez model/API/private data.
- [ ] Świeży lokalny migrate daje4, drugi0; normalny Host sam nie migruje.
- [ ] Start i oba health200; nasłuch tylko127.0.0.1. Techniczny health nie oznacza gotowych porad.
- [ ] Syntetyczne scenariusze w demo potwierdzają rollback/replay/stale lease, ready503/recovery i ochronę reload; nie zmieniaj historii użytkowej bazy do tych prób.
- [ ] Przejrzane ryzyka, lesson digest i pending admin controls poniżej.
- [ ] Administrator zastosował i zweryfikował ochronę main oraz odrębnego uprawnionego reviewera.
- [ ] Decyzja PO: jawne `APPROVE GATE 1` dopiero po review. Sukces CI i cisza nie są akceptacją.

## PR, review, koszty i ryzyka

| PR → commit main | Zakres |
|---|---|
|[#11](https://github.com/bbielecki/wAInery/pull/11) ?→47991ec|baseline/NuGet/build globs|
|[#13](https://github.com/bbielecki/wAInery/pull/13) ?→966bd43|BB/event codec/contract suite|
|[#15](https://github.com/bbielecki/wAInery/pull/15) ?→3db94ea|Weather porty|
|[#17](https://github.com/bbielecki/wAInery/pull/17) ?→9bafee6|runner/001/actual PG|
|[#19](https://github.com/bbielecki/wAInery/pull/19) ?→281ec1d|002 integralność|
|[#21](https://github.com/bbielecki/wAInery/pull/21) ?→27a5fec|owner command/read ports|
|[#26](https://github.com/bbielecki/wAInery/pull/26) ?→69780c7|atomic outbox/precision|
|[#29](https://github.com/bbielecki/wAInery/pull/29) ?→cd0388b|003 durable jobs/fencing|
|[#31](https://github.com/bbielecki/wAInery/pull/31) ?→10d7458|source/evidence/storage/usage ports|
|[#33](https://github.com/bbielecki/wAInery/pull/33) ?→5842616|004 inbox/outbox dispatcher|
|[#35](https://github.com/bbielecki/wAInery/pull/35) ?→b3988d8|Decisions advice/read/freshness|
|[#37](https://github.com/bbielecki/wAInery/pull/37) ?→9810d8f|startup/readiness/reload guard|
|[#38](https://github.com/bbielecki/wAInery/pull/38) ?→73ef3c1|architecture allowlist/runtime negatives|

Każdy przyrost: osobny bounded issue/branch, fetch/rebase, rzeczywiste testy,
niezależny natywny reviewer, obaCI success, squash. Szczegóły i ograniczenia w
agents/handoffs/G1-*.md oraz GitHubPR. P1/P2 naprawiane po deterministycznym RED.
Końcowy reviewer qa_g1_01 zamknął P2 quoting/precedence .env; własny AST/JSON/54
linki/nazwy regresji/whitespace PASS. IO wykonał demo177 PASS; finalCI wymagane
przed squash. Zweryfikowano cleanup pięciu znanychmergedczystych worktrees/gałęzi
po SHA i granicach Temp; pozostał jeden Rootworktree. Zespoły/ACK i pięć przyjętych
lekcji: [Learning Digest](GATE-1-learning-digest.md). Trzy istniejące skills reused,
nowych/retired brak, korzyści czasowej nie mierzono.

Koszt API produktu0, brak nowych płatnych usług. Koszt sesji kodowania niezmierzony;
lokalne Docker/CPU/dysk i publiczne pobrania pakietów pozostają kosztem środowiska.
Brak prywatnych tekstów/embeddings/sekretów w zmianach. Prawa, confirmation i limity
na DTO nie zastępują późniejszej egzekucji ownerów. Runtime dispatcher/joby nie mają
jeszcze biznesowych konsumentów w Host. Readiness nie audytuje całej fizycznej bazy.

Zewnętrzna zależność: main protected=false zweryfikowane przez API. Natywny review
nie jest formalnym GitHub approval innego konta. Konkretny
[pakiet administratora](../../infra/github/README.md) zawiera payload i checklistę;
plugin nie ma administration write. Commit tych plików nie włącza ochrony serwera.
Ta kontrola pozostaje otwarta do działania człowieka/admina, zgodnie z governance.

Następny zakres, nadal NIEZATWIERDZONY: małe zadania Vineyard/Weather, scoped impact,
P-001 i zapisany plan/raport offline według [DAG](GATE-0-plan.md). Przed rozpoczęciem
IO wymaga jawnego `APPROVE GATE 1`. STOP na odbiorze człowieka.
