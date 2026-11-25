# ÉTAPE 3 — IMPLÉMENTATION BACKEND

Tu travailles dans le dossier courant. Lis d'abord `00_brief.md`, `step1_spec.md` (ta spécification) et `step2_arch.md` (ton dossier d'architecture).

Implémente le backend complet de TeamLeave, en suivant ton dossier d'architecture.

Périmètre exigé :
- Solution .NET compilable : API ASP.NET Core + EF Core + migrations ou script SQL.
- Tous les endpoints du contrat d'API de l'étape 2.
- Authentification JWT avec les trois rôles.
- Règle métier de chevauchement à 50 % implémentée et testée.
- Validation des entrées, gestion d'erreurs cohérente (ProblemDetails).
- Tests xUnit : au moins les règles métier critiques (solde, chevauchement, autorisation manager).

LIVRABLE (mode agent) :
- Écris le code dans le dossier `backend/` (la solution .sln à la racine de `backend/`, projets et tests dedans). Chemins relatifs, pas de chemin absolu.
- Le code doit compiler avec `dotnet build` et les tests passer avec `dotnet test` sur la dernière LTS .NET. NuGet officiels uniquement, versions épinglées.
- Tu peux lancer `dotnet build` et `dotnet test` toi-même et corriger tes erreurs.
- Écris aussi un court document `step3_backend.md` à la racine du dossier courant : liste des fichiers produits, section finale "NOTES DE RUN" (commandes pour restaurer, compiler, tester, lancer) et résultat réel du dernier `dotnet build` / `dotnet test` (copie de la sortie utile, ou "non lancé").

CONSIGNE DE PÉRIMÈTRE : ne dépasse pas le périmètre de cette étape. Ne touche pas à `frontend/`, ne modifie pas `step1_spec.md` ni `step2_arch.md`. Si la spec ou l'architecture est insuffisante, décide, et note la décision dans `step3_backend.md`.
