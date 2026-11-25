# BRIEF PRODUIT (entrée identique pour les deux modèles)

Tu es un lead developer / architecte senior. L'entreprise est une PME de 50 personnes.
On te demande de concevoir et réaliser **TeamLeave**, une application interne de gestion des congés.

## Besoin métier
- Les employés soumettent des demandes de congés (dates de début/fin, type : CP, RTT, maladie, autre).
- Chaque employé a un solde de jours par type, décrémenté à l'approbation.
- Les managers valident ou refusent les demandes de leur équipe, avec un commentaire obligatoire en cas de refus.
- Un calendrier d'équipe montre les absences approuvées, pour éviter les chevauchements critiques.
- Règle métier : une demande qui chevauche plus de 50 % d'absences déjà approuvées sur l'équipe doit être signalée au manager avant décision.
- Notifications email à la soumission (au manager) et à la décision (à l'employé).
- Rôles : employé, manager, admin RH (paramétrage des soldes et des types).

## Contraintes techniques imposées
- Backend : ASP.NET Core (dernière LTS), EF Core, base SQL (SQL Server ou PostgreSQL, à justifier).
- Frontend : React + TypeScript (Vite).
- Tests : xUnit côté backend.
- Auth : JWT, avec rôles.
- Livrable exploitable : chaque modèle devra produire du code qui compile et des tests qui passent.

## Ce qu'on attend de toi (chaîne en 4 étapes, prompts séparés)
1. Spécification technico-fonctionnelle.
2. Architecture et conception système.
3. Implémentation backend.
4. Implémentation frontend.

Sois précis, exhaustif et professionnel. Cible : une équipe de dev qui reprend ton travail sans réunion de cadrage.

