# ÉTAPE 3 — IMPLÉMENTATION BACKEND

Implémente le backend complet de TeamLeave, en suivant ton dossier d'architecture (ci-dessous).

Périmètre exigé :
- Solution .NET compilable : API ASP.NET Core + EF Core + migrations ou script SQL.
- Tous les endpoints du contrat d'API de l'étape 2.
- Authentification JWT avec les trois rôles.
- Règle métier de chevauchement à 50 % implémentée et testée.
- Validation des entrées, gestion d'erreurs cohérente (ProblemDetails).
- Tests xUnit : au moins les règles métier critiques (solde, chevauchement, autorisation manager).

FORMAT DE SORTIE OBLIGATOIRE — le harnais extrait les fichiers automatiquement :
- Un bloc par fichier, délimité exactement ainsi :

=== FILE: chemin/relatif/NomFichier.cs ===
<contenu du fichier>

- Aucun commentaire hors des blocs FILE sauf une courte section finale "NOTES DE RUN" (commandes pour restaurer, compiler, tester, lancer).
- Le code doit compiler avec `dotnet build` et les tests passer avec `dotnet test` sur la dernière LTS .NET.
- Pas de dépendances exotiques : NuGet officiels uniquement, versions épinglées.

Voici le brief :

---
{{BRIEF}}
---

Voici ta spécification (étape 1) :

---
{{SPEC}}
---

Voici ton dossier d'architecture (étape 2) :

---
{{ARCH}}
---
