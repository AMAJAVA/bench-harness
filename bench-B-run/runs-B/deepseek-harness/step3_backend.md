# TeamLeave — Implémentation backend

**Étape** : 3 — Implémentation backend  
**Sources** : `00_brief.md`, `step1_spec.md`, `step2_arch.md`  
**Périmètre** : uniquement `backend/` et ce document. Aucun fichier de `frontend/`, `step1_spec.md` ou `step2_arch.md` n'a été modifié.

## 1. Résultat

Le backend TeamLeave est une solution .NET 10 / ASP.NET Core Minimal APIs / EF Core 10 / PostgreSQL 17, découpée en quatre projets et une suite xUnit v3.

- Solution : `backend/TeamLeave.sln`
- API : `backend/src/TeamLeave.Api`
- Domaine : `backend/src/TeamLeave.Domain`
- Application : `backend/src/TeamLeave.Application`
- Infrastructure : `backend/src/TeamLeave.Infrastructure`
- Tests : `backend/tests/TeamLeave.Domain.Tests` (67 tests)
- Schéma SQL : `backend/database/teamleave.sql`
- Environnement local PostgreSQL : `backend/docker-compose.yml`

Le contrat d'API de l'étape 2 est couvert : authentification, profil, soldes, demandes employé, décisions manager, calendrier, types de congés, comptes, équipes, soldes RH, notifications, audit et health checks.

## 2. Fichiers produits

### Racine de `backend/`

| Fichier | Rôle |
|---|---|
| `TeamLeave.sln` | Solution .sln avec les 5 projets |
| `global.json` | SDK .NET 10 épinglé |
| `Directory.Build.props` | `net10.0`, nullable, usings implicites |
| `Directory.Packages.props` | Versions NuGet centralisées et épinglées |
| `docker-compose.yml` | PostgreSQL 17 local, schéma appliqué au premier démarrage |

### `src/TeamLeave.Domain/`

- `Enums/` : `LeaveStatus`, `HalfDay`, `RoleNames`, `BalanceMovementNature`, `NotificationType`, `NotificationStatus`, `OverlapTriggerKind`, `AuditActorType`, `CancellationReasons`
- `Entities/` : `User`, `Team`, `LeaveType`, `LeaveRequest`, `LeaveRequestAllocation`, `AnnualBalance`, `BalanceMovement`, `OverlapEvaluation`, `Notification`, `RefreshToken`, `AuditEntry`
- `Services/` : `LeaveDurationCalculator` (RB-06), `LeaveYearSplitter` (RB-04), `OverlapAnalyzer` (RB-12), `BalancePolicy` (RB-05/RB-19), `DecisionPolicy` (RB-09 à RB-13), `CancellationPolicy` (RB-14)
- `ValueObjects/` : `LeaveHalfDay`, `OverlapResult`, `ApprovedAbsence`, `BalanceShortage`, `DecisionOutcome`, `CancellationOutcome`
- `Exceptions/DomainException.cs`

### `src/TeamLeave.Application/`

- `Abstractions/` : ports `IAuthService`, `ILeaveRequestService`, `IManagerService`, `ICalendarService`, `IAdminService`, `IPasswordHasher`, `ITokenService`, `IEmailSender`, `IClock`, `ICurrentUser`, options JWT
- `Common/` : `Result<T>`, `AppError`, `ErrorCodes`, `ErrorFactory`, `PagedResult<T>`, `Pagination`, `PasswordPolicy`
- `Contracts/` : contrats HTTP de login, profil, demandes, soldes, décisions, calendrier, administration, notifications et audit

### `src/TeamLeave.Infrastructure/`

- `Persistence/TeamLeaveDbContext.cs` : mapping EF Core complet, index, clés composites, conversions d'enums
- `Persistence/DbErrorHelper.cs` : traduction des erreurs PostgreSQL (`23P01`, `23505`, `23514`)
- `Services/AuthService.cs` : login, rafraîchissement rotatif, logout, changement de mot de passe, profil
- `Services/LeaveRequestService.cs` : soumission, liste, détail, annulation, soldes personnels
- `Services/ManagerService.cs` : file, détail avec signal recalculé, soldes des managés, approbation, refus
- `Services/CalendarService.cs` : calendrier d'équipe et masquage du type aux pairs
- `Services/AdminService.cs` : types, comptes, équipes, soldes, ajustements, demandes, notifications, audit
- `Services/OverlapService.cs` : effectif d'équipe et évaluation RB-12
- `Services/BalanceQueryService.cs` : projections de soldes
- `Identity/JwtTokenService.cs` : JWT HS256, rôles, `kid`, `security_stamp`, `mustChangePassword`
- `Identity/Pbkdf2PasswordHasher.cs` : hachage des mots de passe
- `Identity/SecureTokens.cs` : refresh tokens opaques et mots de passe temporaires
- `Notifications/NotificationWorker.cs` : outbox, reprise et échec
- `Notifications/SmtpEmailSender.cs`, `LoggingEmailSender.cs`, `EmailOptions.cs`
- `Audit/AuditWriter.cs`, `Mapping/ContractMapper.cs`, `Time/SystemClock.cs`
- `DependencyInjection.cs` : composition de l'infrastructure

### `src/TeamLeave.Api/`

- `Program.cs` : DI, JWT, autorisation, rate limiting, CORS, middlewares, OpenAPI
- `Endpoints/AuthEndpoints.cs`, `MeEndpoints.cs`, `ManagerEndpoints.cs`, `CalendarEndpoints.cs`, `LeaveTypeEndpoints.cs`, `AdminEndpoints.cs`, `HealthEndpoints.cs`
- `Infrastructure/ApiProblem.cs` : ProblemDetails, `code`, `traceId`
- `Infrastructure/ExceptionHandlingMiddleware.cs`, `CorrelationMiddleware.cs`, `SecurityHeadersMiddleware.cs`, `PasswordChangeGateMiddleware.cs`, `OriginGuardMiddleware.cs`
- `Infrastructure/HttpCurrentUser.cs`, `RefreshCookie.cs`
- `Bootstrap/AdminBootstrapper.cs` : commande `--create-admin`
- `appsettings.json`, `appsettings.Development.json`, `Properties/launchSettings.json`

### `tests/TeamLeave.Domain.Tests/`

- `LeaveDurationCalculatorTests.cs` : exemples RB-06, week-ends, jour férié, année bissextile
- `LeaveYearSplitterTests.cs` : demande à cheval sur deux années
- `OverlapAnalyzerTests.cs` : exemples RB-12, seuil strict, N=1, N=2, demi-journées, doublons
- `BalancePolicyTests.cs` : soldes exacts/insuffisants, multi-années, ajustements
- `DecisionPolicyTests.cs` : auto-décision, manager hors périmètre, Admin RH non manager, refus, acquittement
- `CancellationPolicyTests.cs` : PENDING, APPROVED futur/aujourd'hui/passé, états terminaux
- `LeaveRequestStateTests.cs` : machine à états et invariants de solde
- `PasswordPolicyTests.cs`, `Pbkdf2PasswordHasherTests.cs`, `JwtTokenServiceTests.cs`
- `DbContextModelTests.cs` : validation du modèle EF sans base
- `TestData.cs` : builders de demi-journées et d'absences

### `database/teamleave.sql`

Script PostgreSQL 17 de référence :

- extensions `citext` et `btree_gist` ;
- 11 tables, clés étrangères, index et contraintes `CHECK` ;
- contrainte d'exclusion `ex_leave_requests_personal_overlap` sur `(employee_id, period)` pour les statuts `PENDING`/`APPROVED` ;
- colonne générée `period int4range` ;
- triggers `updated_at`, immutabilité du code de type, transitions de demande, journaux append-only, cohérence différée des allocations ;
- données de référence : équipe `Direction`, types `CP`, `RTT`, `MALADIE`, `AUTRE`.

## 3. Règles métier implémentées

- **RB-05** : `disponible = octroyé − consommé`, jamais négatif, contrôle à la soumission et à l'approbation, mouvement par année.
- **RB-06** : décompte à la demi-journée, week-ends exclus, début/fin ouvrés, multi-années.
- **RB-08** : pré-contrôle du chevauchement personnel + contrainte d'exclusion PostgreSQL en filet de sécurité.
- **RB-09 à RB-13** : manager direct courant uniquement, auto-décision interdite, refus motivé, signal recalculé et acquittement obligatoire.
- **RB-12** : `(A(h) + 1) / N > 0,50`, seuil strict, N = 1 sans signal, seules les absences `APPROVED` comptent.
- **RB-14/RB-21** : annulation par le titulaire, restauration des soldes, désactivation avec annulation des demandes en attente et des congés futurs.
- **RB-16** : outbox transactionnelle `SUBMISSION:{id}` et `DECISION:{id}`, worker hors transaction, reprises et statut `FAILED`.
- **RB-18/RB-19** : audit des actions sensibles, mouvements de solde immuables, ajustements motivés.
- **RB-22** : transactions, verrous consultatifs PostgreSQL, `FOR UPDATE`, unicité des mouvements et contrôle conditionnel des soldes.

## 4. Décisions et écarts documentés

1. **Schéma par script SQL, pas migration EF.** L'étape 2 autorise « migrations EF ou script SQL ». Le script `database/teamleave.sql` est la référence physique, y compris les objets que EF ne modélise pas (exclusion GiST, triggers, contrainte différée).
2. **Hachage des mots de passe.** L'architecture cible Argon2id. Le paquet NuGet Argon2 n'était pas disponible dans l'environnement hors ligne de cette exécution ; l'implémentation utilise **PBKDF2-HMAC-SHA256, 600 000 itérations, sel 16 octets, sortie 32 octets**, derrière `IPasswordHasher`. Le remplacement par Argon2id ne touchera que `Pbkdf2PasswordHasher` et la composition DI. Cette décision doit être confirmée avant production.
3. **xUnit v3 + Microsoft.Testing.Platform.** xUnit v2/testhost était bloqué par le bac à sable Windows (accès processus), xUnit v3 exécute le projet comme un exécutable autonome. La suite est donc directement exécutable et standard `dotnet test` en dehors du bac à sable.
4. **`clearManager` sur `PATCH /admin/users/{id}`.** Ajout d'un champ explicite pour permettre de retirer un manager ; le contrat ne précisait pas comment distinguer « champ absent » de « null explicite ».
5. **Email.** Le worker d'outbox est réel ; le transport est SMTP (`Email:Enabled=true` + `Email:Host`) ou journalisation locale en développement. Azure Communication Services pourra remplacer `IEmailSender` sans toucher au worker ni à la transaction métier. Le contenu ne contient ni type de congé ni commentaire.
6. **JWT de développement.** Sans `Jwt:SigningKey`, une clé éphémère est générée uniquement en environnement `Development`. En production, la clé et la chaîne de connexion sont obligatoires.
7. **Signal de consultation.** Le signal est recalculé à la volée sur `GET /manager/leave-requests/{id}` et n'est pas persisté (décision E-05 de l'architecture) ; il est persisté à la soumission et à la décision.

## NOTES DE RUN

### Prérequis

- .NET SDK 10 (LTS), testé avec `10.0.404`
- PostgreSQL 17
- Optionnel : Docker pour `docker-compose.yml`

### Restaurer, compiler, tester, lancer

Depuis `backend/` :

```powershell
dotnet restore TeamLeave.sln
dotnet build TeamLeave.sln --no-restore
dotnet test tests/TeamLeave.Domain.Tests/TeamLeave.Domain.Tests.csproj
dotnet run --project src/TeamLeave.Api
```

Si le flux NuGet n'est pas joignable (environnement hors ligne), utiliser `dotnet restore TeamLeave.sln -p:RestoreIgnoreFailedSources=true` lorsque les paquets sont déjà présents dans le cache local.

Base locale et schéma :

```powershell
# Option A — Docker
docker compose up -d

# Option B — PostgreSQL déjà installé
psql -U teamleave -d teamleave -f database/teamleave.sql
```

Compte initial (jamais de mot de passe en clair dans le code ou les logs) :

```powershell
$env:TEAMLEAVE_ADMIN_EMAIL = "admin@pme.example"
$env:TEAMLEAVE_ADMIN_PASSWORD = "MotDePasse!2026"
$env:TEAMLEAVE_ADMIN_FIRSTNAME = "Admin"
$env:TEAMLEAVE_ADMIN_LASTNAME = "RH"
dotnet run --project src/TeamLeave.Api -- --create-admin
```

Configuration minimale hors développement :

```text
ConnectionStrings__TeamLeave=Host=...;Database=teamleave;Username=...;Password=...
Jwt__SigningKey=<clé de 32 caractères minimum, idéalement 64>
Email__Enabled=true
Email__Host=smtp.example
Email__From=no-reply@example
Spa__AllowedOrigins__0=https://app.teamleave.example
```

### Dernier `dotnet build`

Commande réellement exécutée (réseau NuGet indisponible pour l'audit de vulnérabilités) :

```powershell
dotnet build TeamLeave.sln --no-restore -p:NuGetAudit=false
```

Sortie utile :

```text
  TeamLeave.Domain -> ...\TeamLeave.Domain.dll
  TeamLeave.Application -> ...\TeamLeave.Application.dll
  TeamLeave.Infrastructure -> ...\TeamLeave.Infrastructure.dll
  TeamLeave.Api -> ...\TeamLeave.Api.dll
  TeamLeave.Domain.Tests -> ...\TeamLeave.Domain.Tests.dll

Build succeeded.

    3 Warning(s)
    0 Error(s)
```

Les 3 avertissements sont uniquement `NU1900` : le flux d'audit `https://api.nuget.org/v3/index.json` n'était pas joignable depuis l'environnement d'exécution. Ils ne concernent ni la compilation ni les tests.

### Dernier `dotnet test`

Le projet utilise xUnit v3 / Microsoft.Testing.Platform. Dans le bac à sable Windows de cette session, `dotnet test` est bloqué **avant l'exécution des tests** par l'interdiction des named pipes :

```text
error run failed: Unhandled exception. System.UnauthorizedAccessException: Access to the path is denied.
   at System.IO.Pipes.NamedPipeClientStream.TryConnect(...)
   at Microsoft.Testing.Platform.IPC.NamedPipeClient.ConnectAsync(...)
```

Le même exécutable de test, lancé sans le canal MSBuild du bac à sable, exécute bien toute la suite :

```powershell
tests\TeamLeave.Domain.Tests\bin\Debug\net10.0\TeamLeave.Domain.Tests.exe --no-ansi
```

```text
Test run summary: Passed!
  total: 67
  failed: 0
  succeeded: 67
  skipped: 0
  duration: 5s 260ms
```

Hors bac à sable, la commande standard `dotnet test tests/TeamLeave.Domain.Tests/TeamLeave.Domain.Tests.csproj` utilise le même exécutable et le même assemblage de tests.

### Vérifications de fumée effectuées

- `GET /health/live` → `200 {"status":"Healthy"}`.
- `GET /openapi/v1.json` → `200`, 33 chemins exposés.
- `GET /api/v1/me` sans jeton → `401 application/problem+json`, code `UNAUTHENTICATED`.
- Le modèle EF Core a été construit sans base dans `DbContextModelTests` (aucune erreur de mapping).

**Fin du document.**
