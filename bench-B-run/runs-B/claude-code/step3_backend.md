# TeamLeave : étape 3, implémentation backend

| Élément | Valeur |
|---|---|
| Entrées | `00_brief.md`, `step1_spec.md`, `step2_arch.md` (non modifiés) |
| Livrable | `backend/` : solution `TeamLeave.sln`, 4 projets applicatifs et 3 projets de tests |
| Cible | .NET 10 (LTS), ASP.NET Core 10, EF Core 10.0.15. `backend/global.json` épingle le SDK 10.0.x (`rollForward: latestFeature`), parce que le SDK 11 (STS) est aussi installé sur le poste. |
| Paquets | Paquets NuGet officiels uniquement, versions épinglées dans `backend/Directory.Packages.props` (gestion centralisée, épinglage transitif activé). |

## 1. Fichiers produits

Tous les chemins sont relatifs à `backend/`.

**Racine**

| Fichier | Rôle |
|---|---|
| `TeamLeave.sln` | Solution (format `.sln`). |
| `global.json` | Épinglage du SDK .NET 10. |
| `Directory.Build.props` | `net10.0`, nullable, usings implicites, pour tous les projets. |
| `Directory.Packages.props` | Versions NuGet épinglées. Inclut `System.Security.Cryptography.Pkcs` 10.0.15, épinglé en dépendance transitive pour supprimer un avis de vulnérabilité NuGet. |
| `database/teamleave-schema.sql` | Script SQL Server idempotent généré depuis les migrations (`dotnet ef migrations script --idempotent`). |
| `database/grants.sql` | Droits de l'identité applicative (`DENY` sur les journaux, §4.3), avec l'écart D-07 ci-dessous. |

**`src/TeamLeave.Domain`** (aucune dépendance hors BCL)

| Fichier | Contenu |
|---|---|
| `Common/DomainException.cs` | `DomainException` (code stable), `NotFoundException`, `ForbiddenException`, `UnauthenticatedException`, `ValidationException`. |
| `Common/ErrorCodes.cs` | Les 38 codes de §5.8. |
| `Common/DayAmount.cs`, `Common/DateRange.cs`, `Common/IVersioned.cs` | Centièmes de jour (2 décimales au plus), période bornes incluses (≤ 366 jours), jeton de version et « touch ». |
| `Users/User.cs` | Utilisateur, rôles cumulatifs (RB-01), tampon de sécurité (GAP-07), verrouillage après 5 échecs en 15 min (ENF-S5, GAP-19), désactivation (RB-27). |
| `Users/ApproverPolicy.cs` | RB-04, H-22, H-31 (`SELF_APPROVAL_FORBIDDEN`, `NOT_APPROVER`). |
| `Users/HierarchyValidator.cs` | RB-03 : contrôle du cycle avant celui du manager invalide (CA-17.3). |
| `Users/PasswordPolicy.cs` | ENF-S4 : 12 à 128 caractères, différent de l'email, liste noire. |
| `Leave/LeaveType.cs`, `Leave/Balance.cs`, `Leave/LeaveRequest.cs` | Types (RB-08, RB-24), solde et mouvements (RB-09 à RB-12, RB-25), machine à états de la demande et historique (RB-13 à RB-15, RB-18). |
| `Calendar/WorkingDayCalculator.cs` | Jours ouvrés (RB-06, RB-07), jours fériés, calcul de Pâques (GAP-16). |
| `Overlap/OverlapAnalyzer.cs` | **Seule implémentation de la règle des 50 %** (RB-16, RB-20). Taux exact `Rate(Absent, TeamSize)`, seuil `2 × A > N`, pourcentage arrondi réservé à l'affichage. |
| `Notifications/Notification.cs` | Ligne d'outbox et calendrier des tentatives t0, +5, +20, +65 min, puis `Failed` (RB-28, ADR-03). |
| `Audit/AuditEntry.cs` | Journal d'administration en ajout seul (RB-26). |

**`src/TeamLeave.Application`** (dépend du Domain et des abstractions EF Core, sans ASP.NET Core ni fournisseur SQL)

| Fichier | Contenu |
|---|---|
| `Abstractions/Ports.cs` | `ITeamLeaveDbContext`, `ICurrentUser`, `IPasswordHasher`, `ITokenIssuer`, `IEmailSender`, `UniqueConstraintViolationException`. |
| `Abstractions/Clock.cs` | UTC tronqué à la ms, « aujourd'hui » en Europe/Paris, à partir de `TimeProvider`. |
| `Abstractions/UseCaseExecutor.cs` | Une transaction par cas d'usage ; rejeu complet jusqu'à 3 tentatives, puis 409 `CONCURRENCY_CONFLICT` (§6.2 M4, ADR-01). |
| `Contracts/Dtos.cs` | Tous les DTO d'entrée (DataAnnotations → 400) et de sortie du contrat §5. |
| `Common/*.cs` | `OverlapService` (charge l'équipe et les absences approuvées), `BalanceService` (disponible = solde − en attente), `NotificationComposer` (emails RB-21/RB-22 en texte simple, en français), `AuditWriter`, `HolidayCalendar`, mapping. |
| `LeaveRequests/LeaveRequestService.cs` | Simulation, soumission, listes (`mine` / `to-decide` / `all`), détail, analyse, approbation, refus, annulation. |
| `Auth/AuthService.cs` | Connexion, `GET /me`, changement de mot de passe, soldes personnels. |
| `Calendar/CalendarService.cs` | Portées `team` / `reports` / `all` (RB-23, GAP-03). |
| `Admin/LeaveTypeService.cs`, `Admin/UserAdminService.cs`, `Admin/HolidayAndJournalServices.cs` | Types, utilisateurs et soldes, jours fériés, journaux. |
| `Notifications/OutboxProcessor.cs` | Traitement d'un lot de l'outbox : réservation avec contrôle de version, envoi, mise à jour. |
| `DependencyInjection.cs` | Enregistrement des services. |

**`src/TeamLeave.Infrastructure`**

| Fichier | Contenu |
|---|---|
| `Persistence/TeamLeaveDbContext.cs` | Incrément du jeton `Version`, interdiction de modifier les journaux, traduction des violations d'unicité SQL Server et SQLite. |
| `Persistence/Configurations.cs` | Mapping Fluent conforme au schéma §4.3 : noms de contraintes, `CHECK`, index filtrés, `datetime2(3)` en UTC, énumérations en `varchar`. |
| `Persistence/Migrations/20261125201834_InitialCreate*.cs`, `TeamLeaveDbContextModelSnapshot.cs` | Migration SQL Server initiale. |
| `Persistence/DesignTimeDbContextFactory.cs` | Fabrique de conception pour `dotnet ef`. |
| `Persistence/DataSeeder.cs` | Amorçage idempotent : 4 types initiaux à identifiants fixes, jours fériés sur table vide (GAP-16), admin RH initial (GAP-17). |
| `Security/Security.cs` | PBKDF2-HMAC-SHA512 (210 000 itérations par défaut), clés HS256 courante et précédente, émission du JWT (`sub`, `role`, `sst`, `mcp`, 60 min). |
| `Email/EmailSenders.cs` | `SmtpEmailSender` (STARTTLS), `LoggingEmailSender` (développement), `OutboxDispatcher` (`BackgroundService`). |
| `Options.cs`, `DependencyInjection.cs` | Options de configuration (§9.3) ; choix du fournisseur SqlServer ou Sqlite. |

**`src/TeamLeave.Api`**

| Fichier | Contenu |
|---|---|
| `Program.cs` | Composition, pipeline §3.4, politique de repli « authentifié », `/health/live`, `/health/ready`, OpenAPI hors production, repli SPA si `wwwroot/index.html` existe. |
| `Infrastructure/Authentication.cs` | Configuration JwtBearer (HS256 seul, `iss`/`aud`, durée de vie calculée avec `TimeProvider`, contrôle `IsActive` + `sst` en base à chaque requête), réponses 401 et 403 avec `code`, middleware `PASSWORD_CHANGE_REQUIRED`, `ICurrentUser`. |
| `Infrastructure/Problems.cs` | Catalogue code → statut, Problem Details (`code`, `traceId`, `errors`), `IExceptionHandler`. |
| `Infrastructure/Hosting.cs` | Limiteur de débit sur la connexion (10 requêtes/min par IP → 429 `RATE_LIMITED`), contrôle de santé de la base, en-têtes de sécurité (CSP…), réponse 400 `VALIDATION_ERROR`. |
| `Controllers/AuthControllers.cs`, `Controllers/LeaveRequestsController.cs`, `Controllers/AdminControllers.cs` | Les 33 couples verbe/chemin de §5. |
| `appsettings.json`, `appsettings.Development.json`, `Properties/launchSettings.json` | Configuration ; secrets vides en base. Le profil Développement utilise SQLite et une clé JWT réservée au développement. |

**`tests/`**

| Projet | Contenu | Tests |
|---|---|---|
| `TeamLeave.Domain.Tests` | EX-1 à EX-7, arrondi et seuil exact, jours ouvrés (CA-03.1, CA-03.2, CA-03.3, CA-18.1), solde (disponible, débit, `BALANCE_BELOW_PENDING`, centièmes), machine à états (transitions, `ALREADY_STARTED`, commentaire de refus, acquittement), `ApproverPolicy`, `HierarchyValidator`, verrouillage, politique de mot de passe, calendrier de l'outbox. | 89 |
| `TeamLeave.IntegrationTests` | Tests HTTP avec `WebApplicationFactory`, SQLite dans un fichier temporaire par test, `FakeTimeProvider` fixé au 2026-03-02 09:00 (Paris), expéditeur SMTP factice. Ils couvrent les CA de US-01 à US-19, les matrices 403/404 de §5.4, et les courses S1 (CA-04.10) et S2 (CA-10.3) par entrelacement déterministe (`InterleavingInterceptor`), avec contrôle de réconciliation solde = somme des mouvements. | 78 |
| `TeamLeave.ArchitectureTests` | Règles de dépendance §3.3 par réflexion, aucun contrôleur n'expose d'entité ni n'injecte le DbContext, aucune lecture de `DateTime.Now`/`UtcNow` dans Domain et Application, chaque règle RB-01 à RB-28 référencée par au moins un `[Trait("Rule", …)]`. | 6 |

## 2. Couverture du périmètre exigé

| Exigence de l'étape | Où |
|---|---|
| Solution .NET compilable, API ASP.NET Core, EF Core, migrations et script SQL | `TeamLeave.sln`, migration `InitialCreate`, `database/teamleave-schema.sql` |
| Tous les endpoints du contrat §5 | 33 actions : auth/profil 5, demandes 8, calendrier 1, types 4, utilisateurs 7, soldes RH 3, jours fériés 3, journaux 2 ; plus `/health/live` et `/health/ready`. |
| JWT avec trois rôles | `Employee`, `Manager`, `HrAdmin` en claims `role` ; politiques `ManagerOrHrAdmin` et `HrAdmin` ; politique de repli « authentifié ». |
| Règle de chevauchement à 50 %, implémentée et testée | `OverlapAnalyzer` + `OverlapService`. Calcul à la soumission (enregistré, mentionné dans l'email), recalcul à l'affichage (détail, file `to-decide`, `scope=all`) et à l'approbation, acquittement obligatoire (422 `OVERLAP_NOT_ACKNOWLEDGED`). Tests EX-1 à EX-7 (unitaires) et CA-04.8, CA-05.3, CA-08.2, CA-09.1 à CA-09.5, CA-11.4, CA-13.2 (HTTP). |
| Validation et erreurs cohérentes (Problem Details) | DataAnnotations → 400 `VALIDATION_ERROR` + `errors` en camelCase ; `DomainException` → statut selon le catalogue §5.8 ; 401, 403 et 429 avec `code` ; 500 sans détail technique ; `traceId` partout. |
| Tests xUnit des règles critiques (solde, chevauchement, autorisation manager) | Domaine (`BalanceTests`, `OverlapAnalyzerTests`, `ApproverPolicyTests`) et HTTP (`SubmissionTests`, `DecisionTests` : CA-10.5, CA-17.6, H-31, auto-approbation, employé sans rôle). |

## 3. Décisions prises à l'étape 3 (insuffisances ou ajustements)

| ID | Constat | Décision |
|---|---|---|
| D-01 | « NuGet officiels uniquement » : l'architecture cite MailKit et NetArchTest, qui sont des bibliothèques tierces. | SMTP via `System.Net.Mail.SmtpClient` (BCL) derrière `IEmailSender`. Tests d'architecture écrits en réflexion et analyse des sources, sans NetArchTest. Le fournisseur d'email reste interchangeable. |
| D-02 | §8.3 prévoit une suite `TeamLeave.SqlServerTests` (Testcontainers + Docker). | **Non livrée.** Testcontainers est tiers et Docker n'est pas garanti ; `dotnet test` doit réussir sans Docker. Le schéma SQL Server est fourni par la migration et le script idempotent, mais n'a pas été exécuté sur un moteur SQL Server réel lors de ce run (voir § Limites). |
| D-03 | Concurrence (ADR-01) : lors de deux approbations simultanées, l'index unique filtré `UX_BalanceMovements_Request_Kind` (garde-fou M5) peut se déclencher **avant** le contrôle de version, selon l'ordre des commandes du lot. Le perdant recevait alors 409 `CONCURRENCY_CONFLICT` au lieu de 409 `INVALID_STATUS_TRANSITION` (CA-10.3). | Cette violation d'unicité est ajoutée aux exceptions rejouées, comme `UQ_Balances_User_LeaveType`. Le rejeu restitue l'erreur métier exacte. Test : `CA_10_3_concurrent_double_approval…`. |
| D-04 | §3.1 décrit « une classe de handler par cas d'usage ». | Les cas d'usage sont regroupés par ressource (`LeaveRequestService`, `UserAdminService`…) : une méthode par cas d'usage, même orchestration (charger → contrôler → domaine → une transaction → outbox → journal). Cela réduit la cérémonie sans changer les responsabilités ; chaque méthode reste testable isolément. |
| D-05 | La durée de vie du jeton doit être testable avec l'horloge factice (CA-01.5). | `LifetimeValidator` personnalisé qui utilise le `TimeProvider` injecté, avec la même tolérance de 30 s. |
| D-06 | Connexion : le compteur d'échecs doit être enregistré même quand la réponse est une erreur. | Le cas d'usage renvoie un résultat (jeton ou erreur) ; la transaction est validée, puis l'erreur est levée. Le verrouillage renvoie 429 avec `lockoutEndUtc` et l'en-tête `Retry-After`. |
| D-07 | `DELETE /leave-types/{id}` d'un type soumis à décompte jamais utilisé : GAP-15 crée d'office un solde à 0 par utilisateur, ce qui rend la suppression impossible avec `DENY DELETE ON Balances` (§4.3). | « Utilisé » signifie : au moins une demande, **ou** au moins un mouvement de solde (sinon 409 `LEAVE_TYPE_IN_USE`). Un type non utilisé est supprimé avec ses soldes à 0 sans mouvement. `database/grants.sql` ne pose donc pas `DENY DELETE` sur `Balances`. |
| D-08 | Passage d'un type à « non soumis » avec des soldes nuls : que faire des lignes `Balances` ? | Elles sont conservées, à 0. `GET /me/balances` ne liste que les types soumis à décompte. Un retour à « soumis » complète les soldes manquants (GAP-15). |
| D-09 | `GET /leave-requests/{id}/overlap` sur une demande déjà décidée : non précisé (GAP-24 ne vise que le champ `overlap` du détail). | L'analyse est recalculée et renvoyée quel que soit le statut, pour l'approbateur et l'admin RH ; le demandeur reçoit 403 `FORBIDDEN`. |
| D-10 | Sans hôte SMTP configuré (développement). | `LoggingEmailSender` : l'envoi est simulé et journalisé, sans contenu (ENF-S9). |
| D-11 | Coût de PBKDF2 en test et limiteur de débit (TestServer partage une seule IP). | Itérations (`PasswordHashing:Iterations`, 210 000 par défaut) et limite de connexion (`RateLimiting:LoginPermitLimit`, 10 par défaut) configurables. Les tests utilisent 1 000 et 1 000. |
| D-12 | Tri des listes : départage à date égale non précisé. | Second critère : date de soumission décroissante (`mine`, `all`). |
| D-13 | `appsettings.Development.json` contient une clé JWT et un mot de passe d'admin initial. | Valeurs **réservées au développement local** (préfixe « DEV-ONLY » dans la clé). Production : Key Vault (§9.3), clés vides dans `appsettings.json`. |
| D-14 | OpenTelemetry / Application Insights (§2.5). | Non câblés à cette étape : ils relèvent de la configuration d'exploitation (paquet Azure Monitor à ajouter au déploiement). Les journaux structurés `ILogger` n'écrivent ni corps, ni jeton, ni commentaire. |
| D-15 | `TreatWarningsAsErrors` (§8.4). | Laissé à `false` dans `Directory.Build.props`. Le dernier build produit 0 avertissement ; la CI peut l'activer sans changement de code. |

Limites connues de ce livrable :

- La migration et le script SQL Server n'ont pas été appliqués sur un SQL Server réel. Seul le modèle EF a été exercé, sur SQLite (`EnsureCreated`).
- Les courses S3, S5, S6 et S7 (§6.3) sont couvertes par le mécanisme (touch du demandeur, des soldes, des ascendants et des admins RH), mais seuls S1 et S2 ont un test dédié.
- Démarrage réel en environnement Development (`dotnet run`, SQLite dans un fichier temporaire) : le journal montre « Compte admin RH initial créé (…) » puis « Now listening on: http://localhost:5089 », sans exception ; le processus a ensuite été arrêté. Aucune requête HTTP n'a été envoyée à ce processus, car l'environnement d'exécution exigeait une approbation pour les appels réseau locaux. Le pipeline HTTP complet (authentification, autorisation, Problem Details, contrôleurs) est exercé par les 78 tests d'intégration via `WebApplicationFactory`.

## NOTES DE RUN

Prérequis : SDK .NET 10.0.x (10.0.100 minimum, imposé par `backend/global.json` ; un SDK 11 seul ne convient pas, le roll-forward reste dans la ligne 10.0) et accès à nuget.org pour la restauration. Les commandes se lancent depuis `backend/`.

```bash
cd backend

# Restaurer
dotnet restore TeamLeave.sln

# Compiler
dotnet build TeamLeave.sln

# Tester (sans Docker ni SQL Server : SQLite dans des fichiers temporaires)
dotnet test TeamLeave.sln

# Lancer en développement (SQLite ./teamleave-dev.db, schéma créé et données amorcées au démarrage)
dotnet run --project src/TeamLeave.Api --launch-profile http
#   API     : http://localhost:5080/api/v1
#   OpenAPI : http://localhost:5080/openapi/v1.json
#   Santé   : http://localhost:5080/health/ready
#   Compte initial : admin.rh@pme.local / ChangeMe-Dev-2026! (changement de mot de passe exigé)

# Lancer sur SQL Server (exemple local)
dotnet run --project src/TeamLeave.Api --no-launch-profile -- \
  --ConnectionStrings:TeamLeave "Server=(localdb)\\MSSQLLocalDB;Database=TeamLeave;Trusted_Connection=True;TrustServerCertificate=True" \
  --Database:Provider SqlServer --Database:AutoMigrate true \
  --Jwt:SigningKeys:Current "<clé Base64 de 32 octets ou plus>"

# Migrations (outil dotnet-ef)
dotnet ef migrations add <Nom> --project src/TeamLeave.Infrastructure --startup-project src/TeamLeave.Infrastructure --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project src/TeamLeave.Infrastructure --startup-project src/TeamLeave.Infrastructure --output database/teamleave-schema.sql
```

### Résultat réel du dernier `dotnet build` (2026-11-25, SDK 10.0.404)

```
  TeamLeave.Domain -> ...\backend\src\TeamLeave.Domain\bin\Debug\net10.0\TeamLeave.Domain.dll
  TeamLeave.Application -> ...\backend\src\TeamLeave.Application\bin\Debug\net10.0\TeamLeave.Application.dll
  TeamLeave.Domain.Tests -> ...\backend\tests\TeamLeave.Domain.Tests\bin\Debug\net10.0\TeamLeave.Domain.Tests.dll
  TeamLeave.Infrastructure -> ...\backend\src\TeamLeave.Infrastructure\bin\Debug\net10.0\TeamLeave.Infrastructure.dll
  TeamLeave.Api -> ...\backend\src\TeamLeave.Api\bin\Debug\net10.0\TeamLeave.Api.dll
  TeamLeave.ArchitectureTests -> ...\backend\tests\TeamLeave.ArchitectureTests\bin\Debug\net10.0\TeamLeave.ArchitectureTests.dll
  TeamLeave.IntegrationTests -> ...\backend\tests\TeamLeave.IntegrationTests\bin\Debug\net10.0\TeamLeave.IntegrationTests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:01:01.35
```

### Résultat réel du dernier `dotnet test` (2026-11-25)

```
Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 202 ms - TeamLeave.ArchitectureTests.dll (net10.0)
Passed!  - Failed:     0, Passed:    89, Skipped:     0, Total:    89, Duration: 370 ms - TeamLeave.Domain.Tests.dll (net10.0)
Passed!  - Failed:     0, Passed:    78, Skipped:     0, Total:    78, Duration: 1 m 28 s - TeamLeave.IntegrationTests.dll (net10.0)
```

Total : 173 tests, 173 réussis, 0 échec, 0 ignoré.
