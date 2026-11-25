# Spécification technico-fonctionnelle — TeamLeave

## 1. Contexte et objectifs

TeamLeave est une application interne destinée à une PME de 50 personnes. Elle centralise la gestion des congés : soumission des demandes, contrôle des soldes, validation managériale, visualisation des absences d’équipe, prévention des chevauchements critiques et notifications email.

Objectifs principaux :

- Remplacer les échanges informels/emails par un workflow traçable.
- Fiabiliser les soldes par employé et par type de congé.
- Permettre aux managers de décider rapidement avec un avertissement explicite en cas de chevauchement critique.
- Donner une visibilité d’équipe sur les absences approuvées.
- Sécuriser les accès par rôles via JWT.
- Produire un socle technique exploitable : ASP.NET Core, EF Core, base SQL, React + TypeScript, tests xUnit.

Critères de succès :

- Une demande est soumise, notifiée, décidée et historisée sans traitement manuel.
- Un solde n’est décrémenté qu’une seule fois, uniquement à l’approbation.
- Un chevauchement supérieur à 50 % est signalé au manager avant décision.
- Les droits d’accès respectent les rôles employé, manager, admin RH.

## 2. Périmètre (inclus / exclu)

### Inclus

- Authentification JWT et autorisation par rôles.
- Consultation de ses soldes et de ses demandes.
- Soumission de demandes de congé : dates de début/fin, type CP, RTT, maladie, autre — types paramétrables.
- Calcul des jours ouvrés demandés.
- Contrôle de disponibilité du solde à la soumission et à l’approbation.
- Workflow de décision manager : approbation ou refus.
- Commentaire obligatoire en cas de refus, optionnel en cas d’approbation.
- Détection et signalement d’un chevauchement critiques supérieur à 50 % avec des absences déjà approuvées de l’équipe.
- Calendrier des absences approuvées par équipe.
- Notifications email : soumission au manager, décision à l’employé.
- Administration RH des types de congé, des soldes et des jours fériés.
- Journal d’audit des décisions et ajustements de soldes.

### Exclu

- Paie, contrats de travail, gestion administrative des arrêts maladie.
- Dépôt et gestion de justificatifs médicaux ou de diagnostics.
- Demi-journées, congés en heures, congés récurrents.
- Workflow multi-niveaux, délégation de validation, validation par plusieurs managers.
- Annulation ou modification d’une demande déjà soumise dans le MVP.
- Gestion complète des utilisateurs et des équipes : provisionnement supposé externe/import.
- SSO, MFA, annuaire LDAP/AD.
- Reporting avancé, export paie, application mobile native.
- Gestion des chevauchements inter-équipes.

## 3. Acteurs et rôles (matrice des permissions)

Rôles cumulables :

- **Employé** : consulte ses données, soumet ses demandes, consulte le calendrier de son équipe.
- **Manager** : capacités employé + consultation et décision des demandes de ses équipes. Ne peut pas décider de sa propre demande.
- **Admin RH** : paramètre les types de congé, les soldes et les jours fériés ; consulte les données d’administration en lecture. Ne décide pas des demandes sauf s’il possède aussi le rôle manager.

| Action | Employé | Manager | Admin RH |
|---|---:|---:|---:|
| Se connecter | Oui | Oui | Oui |
| Consulter son profil | Oui, soi | Oui, soi | Oui, soi |
| Consulter ses soldes | Oui, soi | Oui, soi | Oui, soi |
| Soumettre sa demande | Oui | Oui | Oui |
| Consulter ses demandes | Oui, soi | Oui, soi | Oui, soi |
| Consulter les demandes en attente de son équipe | Non | Oui, équipes managées | Non, sauf rôle manager |
| Approuver/refuser une demande d’équipe | Non | Oui, équipes managées, hors demandes personnelles | Non, sauf rôle manager |
| Consulter le calendrier d’équipe | Oui, son équipe | Oui, équipes managées + son équipe | Oui, toutes équipes en lecture seule |
| Gérer les types de congé | Non | Non | Oui |
| Gérer les soldes | Non | Non | Oui |
| Gérer les jours fériés | Non | Non | Oui |
| Consulter le journal d’audit | Non | Non | Oui |

## 4. User stories numérotées, chacune avec critères d’acceptation testables

### US-01 — Authentification et autorisation par rôle

**En tant qu’utilisateur, je veux me connecter et accéder uniquement aux fonctionnalités de mon rôle.**

Règles appliquées : RB-01, RB-14.

- **Given** des identifiants valides, **When** l’utilisateur appelle `POST /api/v1/auth/login`, **Then** il reçoit un JWT signé contenant son identité, ses rôles et une date d’expiration.
- **Given** une requête sans JWT valide, **When** elle cible un endpoint protégé, **Then** l’API retourne `401`.
- **Given** un manager authentifié, **When** il tente de décider une demande d’une équipe qu’il ne manage pas, **Then** l’API retourne `403`.
- **Given** un JWT expiré, **When** l’utilisateur appelle un endpoint protégé, **Then** l’API retourne `401`.

### US-02 — Consulter ses soldes

**En tant qu’employé, je veux consulter mes soldes par type de congé et par année.**

Règles appliquées : RB-05, RB-12.

- **Given** un employé authentifié, **When** il appelle `GET /api/v1/me/balances?year=2025`, **Then** l’API retourne uniquement ses soldes, avec jours acquis, ajustements, jours utilisés et disponible calculé.
- **Given** aucun solde n’existe pour un type actif et une année donnée, **When** l’employé consulte ses soldes, **Then** le disponible retourné est `0`.
- **Given** un utilisateur tente de consulter les soldes d’un autre employé sans être admin RH, **When** il appelle l’API correspondante, **Then** l’API retourne `403`.

### US-03 — Soumettre une demande de congé

**En tant qu’employé, je veux soumettre une demande de congé pour qu’elle soit validée par mon manager.**

Règles appliquées : RB-02, RB-03, RB-04, RB-05, RB-06, RB-09.

- **Given** un employé actif rattaché à une équipe avec manager, un type actif, des dates valides intra-année et un solde suffisant, **When** il soumet une demande, **Then** la demande est créée au statut `PENDING`, la durée en jours ouvrés est calculée, le manager est notifié et le chevauchement est évalué.
- **Given** une demande dont la durée dépasse le solde disponible, **When** l’employé soumet la demande, **Then** l’API retourne `422` et aucune demande n’est créée.
- **Given** une date de début postérieure à la date de fin, une demande à cheval sur deux années civiles, ou un type inactif, **When** l’employé soumet la demande, **Then** l’API retourne `400` et aucune demande n’est créée.
- **Given** une demande dont le chevauchement calculé est strictement supérieur à 50 %, **When** elle est créée, **Then** elle porte l’indicateur de chevauchement à `true` avec le ratio calculé.

### US-04 — Calculer la durée en jours ouvrés

**En tant que système, je veux calculer les jours ouvrés d’une demande de façon déterministe.**

Règles appliquées : RB-04, RB-17.

- **Given** une demande du lundi au vendredi sans jour férié, **When** la durée est calculée, **Then** la durée vaut `5` jours ouvrés.
- **Given** une demande incluant un samedi et un dimanche, **When** la durée est calculée, **Then** le week-end n’est pas compté.
- **Given** un jour férié paramétré tombe dans l’intervalle, **When** la durée est calculée, **Then** ce jour n’est pas compté.
- **Given** une demande avec une demi-journée ou une durée horaire, **When** elle est soumise, **Then** elle est refusée car le MVP ne gère que des journées entières.

### US-05 — Signaler un chevauchement critique supérieur à 50 %

**En tant que manager, je veux être averti lorsqu’une demande chevauche plus de 50 % d’absences déjà approuvées dans l’équipe avant de décider.**

Règles appliquées : RB-06.

- **Given** une demande de 5 jours ouvrés dont 3 jours sont couverts par des absences approuvées d’autres membres de la même équipe, **When** le chevauchement est calculé, **Then** l’indicateur vaut `true` et le ratio vaut `0,6`.
- **Given** une demande de 4 jours ouvrés dont exactement 2 jours sont couverts, **When** le chevauchement est calculé, **Then** l’indicateur vaut `false`, car 50 % n’est pas strictement supérieur à 50 %.
- **Given** une absence approuvée de l’employé lui-même, une demande `PENDING`, une demande `REJECTED`, ou une absence d’une autre équipe, **When** le chevauchement est calculé, **Then** ces éléments sont ignorés.
- **Given** un chevauchement supérieur à 50 % recalculé avant décision, **When** le manager tente de décider sans acquitter l’avertissement, **Then** l’API retourne `409` ; avec acquittement explicite, la décision est acceptée.

### US-06 — Consulter les demandes en attente de son équipe

**En tant que manager, je veux voir les demandes en attente de mes équipes pour les traiter.**

Règles appliquées : RB-01, RB-06, RB-07.

- **Given** un manager authentifié, **When** il appelle la liste des demandes en attente de ses équipes, **Then** l’API retourne uniquement les demandes `PENDING` des équipes qu’il manage.
- **Given** une demande avec avertissement de chevauchement, **When** elle apparaît dans la liste, **Then** le manager voit l’indicateur, le ratio et les dates concernées.
- **Given** une demande d’une autre équipe, **When** le manager consulte sa liste, **Then** cette demande n’apparaît pas.

### US-07 — Approuver une demande

**En tant que manager, je veux approuver une demande valide pour la transmettre à l’employé et décrémenter le solde.**

Règles appliquées : RB-05, RB-06, RB-07, RB-08, RB-09, RB-13, RB-15.

- **Given** une demande `PENDING` d’une équipe managée, un solde suffisant et aucun avertissement bloquant non acquitté, **When** le manager approuve, **Then** la demande passe à `APPROVED`, le solde utilisé est incrémenté une seule fois, la décision est auditée et l’employé est notifié.
- **Given** le solde devient insuffisant au moment de l’approbation, **When** le manager approuve, **Then** l’API retourne `409`, la demande reste `PENDING` et aucun solde n’est modifié.
- **Given** un avertissement de chevauchement actif, **When** le manager approuve sans `acknowledgeOverlapWarning=true`, **Then** l’API retourne `409` et aucune décision n’est enregistrée.
- **Given** le manager est lui-même le demandeur, **When** il tente d’approuver sa demande, **Then** l’API retourne `403`.

### US-08 — Refuser une demande avec commentaire obligatoire

**En tant que manager, je veux refuser une demande avec un motif pour informer l’employé.**

Règles appliquées : RB-07, RB-09, RB-13.

- **Given** une demande `PENDING` d’une équipe managée et un commentaire non vide, **When** le manager refuse, **Then** la demande passe à `REJECTED`, le commentaire est enregistré, aucun solde n’est décrémenté et l’employé est notifié.
- **Given** un commentaire vide ou absent, **When** le manager refuse, **Then** l’API retourne `400` et la demande reste `PENDING`.
- **Given** une demande déjà `APPROVED` ou `REJECTED`, **When** le manager tente une nouvelle décision, **Then** l’API retourne `409` et la demande reste inchangée.
- **Given** le manager est le demandeur, **When** il tente de refuser sa demande, **Then** l’API retourne `403`.

### US-09 — Notifier par email

**En tant que système, je veux envoyer les notifications email nécessaires sans bloquer le workflow principal.**

Règles appliquées : RB-09.

- **Given** une demande soumise, **When** la transaction est validée, **Then** un email est envoyé au manager de l’équipe avec l’employé, le type, les dates et l’avertissement de chevauchement s’il existe.
- **Given** une demande approuvée ou refusée, **When** la décision est validée, **Then** un email est envoyé à l’employé avec la décision et le commentaire du manager s’il existe.
- **Given** un échec du fournisseur email, **When** l’envoi échoue, **Then** la demande ou la décision reste enregistrée, l’échec est journalisé et l’envoi est retenté jusqu’à 3 fois.

### US-10 — Consulter le calendrier d’équipe

**En tant qu’employé ou manager, je veux voir les absences approuvées de l’équipe pour organiser la continuité.**

Règles appliquées : RB-01, RB-10.

- **Given** un employé authentifié, **When** il consulte le calendrier de son équipe sur une période, **Then** l’API retourne uniquement les absences `APPROVED` de son équipe, avec nom, type, date de début et date de fin.
- **Given** une demande `PENDING` ou `REJECTED`, **When** le calendrier est consulté, **Then** elle n’apparaît pas.
- **Given** un manager, **When** il consulte le calendrier, **Then** il voit les absences approuvées des équipes qu’il manage.
- **Given** un admin RH, **When** il consulte le calendrier, **Then** il peut voir toutes les équipes en lecture seule.

### US-11 — Administrer les types de congé

**En tant qu’admin RH, je veux gérer les types de congé pour adapter l’application aux règles internes.**

Règles appliquées : RB-02, RB-11.

- **Given** un admin RH authentifié, **When** il crée un type avec un code unique et un libellé, **Then** le type est actif et utilisable pour les nouvelles demandes.
- **Given** un type existant, **When** l’admin RH modifie son libellé ou son statut actif, **Then** les nouvelles demandes utilisent le nouvel état et les demandes existantes restent inchangées.
- **Given** un type utilisé, **When** l’admin RH tente de le supprimer définitivement, **Then** l’opération est refusée et seule la désactivation est autorisée.
- **Given** un utilisateur sans rôle admin RH, **When** il tente de gérer les types, **Then** l’API retourne `403`.

### US-12 — Administrer les soldes

**En tant qu’admin RH, je veux configurer et ajuster les soldes avec audit pour corriger les droits à congés.**

Règles appliquées : RB-05, RB-12, RB-16.

- **Given** un admin RH authentifié, **When** il définit les jours acquis ou un ajustement signé avec motif pour un employé, un type et une année, **Then** le solde est enregistré, le disponible est recalculé et une entrée d’audit est créée.
- **Given** un ajustement qui rendrait le disponible négatif, **When** l’admin RH tente de l’enregistrer, **Then** l’API retourne `409` et le solde reste inchangé.
- **Given** un admin RH, **When** il tente de modifier directement les jours utilisés, **Then** l’opération est refusée ; les jours utilisés ne varient que par approbation de demande.
- **Given** un utilisateur sans rôle admin RH, **When** il tente d’administrer un solde, **Then** l’API retourne `403`.

### US-13 — Empêcher l’auto-approbation et sécuriser les décisions

**En tant que système, je veux garantir qu’une demande ne peut pas être validée par son demandeur et qu’elle n’est décrémentée qu’une fois.**

Règles appliquées : RB-07, RB-08, RB-13, RB-15.

- **Given** un manager qui a soumis sa propre demande, **When** il tente de l’approuver ou de la refuser, **Then** l’API retourne `403`.
- **Given** deux appels concurrents d’approbation sur la même demande, **When** ils sont traités, **Then** une seule décision est enregistrée et le solde n’est décrémenté qu’une fois ; l’autre appel retourne `409` ou l’état courant sans nouvelle décrémentation.
- **Given** une décision acceptée, **When** elle est enregistrée, **Then** une entrée d’audit contient l’acteur, la date, la demande, la décision et le commentaire.

### US-14 — Administrer les jours fériés

**En tant qu’admin RH, je veux paramétrer les jours fériés pour que les durées soient correctes.**

Règles appliquées : RB-04, RB-17.

- **Given** un admin RH, **When** il ajoute une date comme jour férié actif, **Then** cette date est exclue des calculs de jours ouvrés des demandes concernées.
- **Given** un jour férié supprimé ou désactivé, **When** une nouvelle demande est calculée, **Then** le jour n’est plus exclu.
- **Given** un utilisateur non admin RH, **When** il tente de gérer les jours fériés, **Then** l’API retourne `403`.

## 5. Règles métier numérotées

**RB-01 — Authentification et périmètre d’accès.**  
Toute API protégée exige un JWT valide. Les droits sont contrôlés par rôle et appartenance à l’équipe. Un utilisateur ne consulte que ses données, sauf manager pour ses équipes et admin RH selon la matrice de permissions.

**RB-02 — Types de congé.**  
Types initiaux : CP, RTT, MALADIE, AUTRE. L’admin RH peut créer, modifier, activer/désactiver un type. Le code est unique. Un type inactif ne peut pas être utilisé pour une nouvelle demande. Les demandes existantes conservent leur type. Dans ce MVP, les types actifs sont à suivi de solde.

**RB-03 — Validité d’une demande.**  
Une demande doit référencer un type actif, un utilisateur actif, une équipe avec manager, une date de début et une date de fin, avec dateDébut ≤ dateFin, mêmes année civile et durée > 0. Aucun délai de prévenance ni restriction passé/futur n’est imposé dans cette version.

**RB-04 — Calcul des jours ouvrés.**  
La durée est le nombre de jours du lundi au vendredi, dates de début et de fin incluses, moins les jours fériés actifs paramétrés. Les samedis, dimanches et jours fériés ne sont pas comptés. Aucune demi-journée ni durée horaire n’est gérée.

**RB-05 — Soldes annuels et disponibilité.**  
Le solde est défini par utilisateur, type de congé et année civile. Disponible = jours acquis + ajustements signés − jours utilisés. Les demandes `PENDING` ne sont pas déduites du solde. À la soumission, une demande est refusée si sa durée dépasse le disponible. À l’approbation, le disponible est recontrôlé ; si insuffisant, l’approbation est bloquée. Un solde ne peut pas devenir négatif. L’année de solde est l’année de la date de début ; une demande à cheval sur deux années est interdite dans le MVP.

**RB-06 — Chevauchement critique supérieur à 50 %.**  
Soit `D` la durée en jours ouvrés de la demande. Pour chaque jour ouvré de la demande, il est considéré comme chevauché si au moins une demande `APPROVED` d’un autre employé de la même équipe couvre ce jour. Soit `O` le nombre de jours ouvrés de la demande couverts par l’union de ces absences approuvées. Le ratio est `O / D`. L’avertissement est actif si et seulement si `O / D > 0,5`, soit `2 × O > D` sans arrondi flottant. Les absences de l’employé lui-même, les demandes `PENDING`/`REJECTED` et les autres équipes sont ignorées. Le calcul est fait à la soumission et recalculé avant décision. Si l’avertissement est actif, la décision manager exige un acquittement explicite ; sinon l’API retourne `409`. L’avertissement ne bloque pas l’approbation. L’acquittement est tracé.

**RB-07 — Workflow de décision.**  
Statuts autorisés : `PENDING`, `APPROVED`, `REJECTED`. Seul le manager de l’équipe enregistrée sur la demande peut décider. Une décision n’est possible que depuis `PENDING`. Le commentaire est obligatoire pour un refus et optionnel pour une approbation. Une demande décidée est immuable dans le MVP. Le manager ne peut jamais décider de sa propre demande. L’admin RH ne peut décider que s’il possède aussi le rôle manager de l’équipe concernée.

**RB-08 — Décrémentation transactionnelle à l’approbation.**  
L’approbation s’exécute dans une transaction : vérification du statut `PENDING`, recontrôle du solde, vérification de l’acquittement de chevauchement si nécessaire, incrémentation unique des jours utilisés, passage à `APPROVED`, enregistrement du décideur/date/commentaire, audit et mise en file de notification. Toute erreur annule l’ensemble. Une nouvelle tentative d’approbation sur une demande déjà approuvée ne décrémente pas à nouveau.

**RB-09 — Notifications email.**  
À la soumission, un email est envoyé au manager de l’équipe. À la décision, un email est envoyé à l’employé. Les emails sont envoyés de façon asynchrone après validation de la transaction métier. Un échec d’email ne bloque pas la soumission ou la décision ; il est journalisé et retenté jusqu’à 3 fois. Les emails ne contiennent pas de données médicales détaillées.

**RB-10 — Calendrier d’équipe.**  
Le calendrier affiche uniquement les absences `APPROVED`. Un employé voit son équipe. Un manager voit les équipes qu’il manage. L’admin RH peut voir toutes les équipes en lecture seule. Les données affichées sont limitées au nom, type, dates, sans commentaire de refus ni détail médical.

**RB-11 — Administration des types de congé.**  
L’admin RH peut créer, modifier et désactiver les types. Un type utilisé ne peut pas être supprimé définitivement. La désactivation n’affecte pas les demandes existantes et empêche seulement les nouvelles demandes.

**RB-12 — Administration des soldes.**  
L’admin RH peut définir les jours acquis et des ajustements signés avec motif, par utilisateur, type et année. Les jours utilisés ne sont jamais modifiables directement. Un ajustement est refusé s’il rend le disponible négatif. Toute modification est auditée.

**RB-13 — Interdiction d’auto-approbation.**  
Aucun utilisateur ne peut approuver ou refuser sa propre demande, même s’il est manager ou admin RH.

**RB-14 — Sécurité et confidentialité.**  
Les échanges se font en HTTPS. Les mots de passe, si authentification locale, sont hachés avec un algorithme robuste. Les JWT sont signés et expirent. Les contrôles d’autorisation sont systématiques côté API. Les logs et emails n’exposent pas de données sensibles inutiles.

**RB-15 — Concurrence et idempotence.**  
Les soldes et demandes utilisent une gestion de concurrence optimiste ou équivalente. Une décision concurrente sur une même demande ne doit pas produire de double décrémentation. En cas de conflit, l’API retourne `409` et l’état courant reste cohérent.

**RB-16 — Audit.**  
Les décisions, changements de statut, ajustements de soldes, modifications de types, modifications de jours fériés et acquittements de chevauchement sont audités avec acteur, date, entité, action, valeurs avant/après si pertinentes et motif pour les ajustements.

**RB-17 — Jours fériés.**  
L’admin RH peut ajouter, modifier ou désactiver des jours fériés. Ces jours sont exclus du calcul des jours ouvrés. Un jour férié inactif n’est pas exclu.

## 6. Modèle de données fonctionnel (entités, attributs, relations, sans SQL)

### Utilisateur

- Identifiant.
- Email unique.
- Nom, prénom.
- Hash du mot de passe si authentification locale.
- Rôles : `EMPLOYE`, `MANAGER`, `ADMIN_RH` — cumulables.
- Actif : oui/non.
- Équipe : 0 ou 1 équipe.
- Date de création, date de dernière modification.

Relations :

- Un utilisateur peut avoir plusieurs soldes.
- Un utilisateur peut soumettre plusieurs demandes.
- Un utilisateur peut décider plusieurs demandes en tant que manager.
- Un utilisateur peut être manager de plusieurs équipes.

### Équipe

- Identifiant.
- Nom.
- Manager : utilisateur référence.
- Active : oui/non.

Relations :

- Une équipe contient plusieurs utilisateurs.
- Une équipe a un manager principal dans le MVP.
- Une équipe est associée à plusieurs demandes via l’équipe enregistrée à la soumission.

### TypeConge

- Identifiant.
- Code unique : `CP`, `RTT`, `MALADIE`, `AUTRE`, etc.
- Libellé.
- Actif : oui/non.
- Suivi de solde : oui/non — dans le MVP, les types actifs sont suivis.
- Ordre d’affichage.
- Date de création, date de dernière modification.

Relations :

- Un type a plusieurs soldes.
- Un type a plusieurs demandes.

### SoldeConge

- Identifiant.
- Utilisateur.
- Type de congé.
- Année civile.
- Jours acquis.
- Ajustements signés.
- Jours utilisés.
- Version de concurrence.
- Contrainte fonctionnelle : unicité utilisateur + type + année.
- Disponible calculé : jours acquis + ajustements − jours utilisés.

Relations :

- Appartient à un utilisateur.
- Appartient à un type de congé.

### DemandeConge

- Identifiant.
- Employé demandeur.
- Équipe enregistrée à la soumission.
- Type de congé.
- Date de début.
- Date de fin.
- Nombre de jours ouvrés demandés.
- Statut : `PENDING`, `APPROVED`, `REJECTED`.
- Commentaire employé facultatif.
- Date de soumission.
- Décideur : utilisateur manager.
- Date de décision.
- Commentaire manager.
- Indicateur de chevauchement : oui/non.
- Ratio de chevauchement calculé.
- Acquittement du chevauchement : utilisateur, date.
- Version de concurrence.

Relations :

- Appartient à un employé.
- Référence un type de congé.
- Référence une équipe.
- Peut être décidée par un manager.
- Une demande approuvée alimente le calendrier d’équipe et les calculs de chevauchement.

### JourFerie

- Identifiant.
- Date.
- Libellé.
- Actif : oui/non.
- Date de création, date de dernière modification.

Relations :

- Utilisé par le calcul des jours ouvrés.

### JournalAudit

- Identifiant.
- Acteur.
- Action : décision, ajustement de solde, modification de type, modification de jour férié, acquittement de chevauchement, etc.
- Type d’entité.
- Identifiant de l’entité.
- Date.
- Détails fonctionnels ou valeurs avant/après.
- Motif si applicable.

Relations :

- Référence l’utilisateur acteur.
- Peut référencer une demande, un solde, un type ou un jour férié.

### NotificationEmail

- Identifiant.
- Destinataire.
- Modèle/template.
- Données utiles à l’email.
- Statut : à envoyer, envoyé, échec.
- Nombre de tentatives.
- Date de création, date de dernier envoi.
- Message d’erreur éventuel.

Relations :

- Peut être liée à une demande.

## 7. Exigences non fonctionnelles

### Performance

- Application dimensionnée pour 50 utilisateurs, avec une charge simultanée faible à modérée.
- Temps de réponse API cible : p95 < 500 ms pour les lectures/écritures courantes hors envoi email.
- Affichage du calendrier d’une équipe sur 12 mois : < 1 s pour 50 utilisateurs.
- Les emails sont asynchrones et ne bloquent pas les transactions métier.

### Sécurité

- HTTPS obligatoire.
- JWT signé, expiration courte, contrôle systématique des rôles et périmètres.
- Mots de passe hachés avec un algorithme robuste si authentification locale.
- Validation stricte des entrées, protection contre les accès directs non autorisés.
- Principe du moindre privilège.
- Pas de données médicales détaillées dans les emails, logs ou calendriers.
- Journalisation des actions sensibles sans exposer de secrets.

### Disponibilité

- Disponibilité cible : 99,5 % sur les heures ouvrées.
- Sauvegarde quotidienne de la base.
- RPO cible : 24 h ; RTO cible : 4 h.
- Les échecs d’envoi email ne bloquent pas le workflow métier.

### Accessibilité

- Interface React conforme aux bonnes pratiques WCAG 2.1 AA : navigation clavier, contrastes, labels, messages d’erreur explicites, tableaux accessibles.
- Les avertissements de chevauchement doivent être lisibles et compréhensibles sans dépendre uniquement de la couleur.

### RGPD et confidentialité

- Minimisation des données : pas de diagnostic médical, pas de justificatif stocké dans le MVP.
- Accès restreint aux soldes, demandes et calendriers selon la matrice de permissions.
- Journal d’audit conservé pour la traçabilité, avec durée de conservation paramétrable.
- Durées de conservation à valider avec le DPO/RH : demandes et soldes, logs techniques, notifications, audit.
- Droit d’accès et de rectification assuré via les procédures RH internes.
- Les emails contiennent uniquement les informations nécessaires.

### Maintenabilité et exploitation

- Backend ASP.NET Core dernière LTS, EF Core, SQL Server ou PostgreSQL à justifier en architecture.
- Frontend React + TypeScript Vite.
- Tests backend xUnit obligatoires sur les règles métier critiques : calcul jours ouvrés, chevauchement, soldes, décisions, autorisations.
- Logs structurés et suivi des notifications email.

## 8. Ébauche d’API (ressources et verbes, sans implémentation)

API REST JSON versionnée : `/api/v1`. Authentification par JWT Bearer.

### Authentification et profil

- `POST /api/v1/auth/login` : authentification, retourne un JWT.
- `GET /api/v1/me` : profil de l’utilisateur courant.

### Employé — soldes et demandes

- `GET /api/v1/me/balances?year={année}` : soldes personnels.
- `GET /api/v1/me/leave-requests?status={statut}&from={date}&to={date}` : demandes personnelles.
- `POST /api/v1/leave-requests` : soumettre une demande.
- `GET /api/v1/leave-requests/{id}` : consulter une demande si autorisé.

### Manager — décisions et équipe

- `GET /api/v1/teams/{teamId}/leave-requests?status=PENDING` : demandes en attente de l’équipe.
- `GET /api/v1/teams/{teamId}/calendar?from={date}&to={date}` : calendrier des absences approuvées.
- `POST /api/v1/leave-requests/{id}/decision` : décider d’une demande.
  - Corps fonctionnel attendu : `action` (`APPROVE`/`REJECT`), `comment`, `acknowledgeOverlapWarning`.

### Admin RH — types, soldes, jours fériés, audit

- `GET /api/v1/leave-types` : lister les types.
- `POST /api/v1/leave-types` : créer un type.
- `PUT /api/v1/leave-types/{id}` : modifier un type.
- `GET /api/v1/users/{userId}/balances?year={année}` : consulter les soldes d’un utilisateur.
- `POST /api/v1/users/{userId}/balances/{leaveTypeId}/adjustments` : créer un ajustement de solde.
- `PUT /api/v1/users/{userId}/balances/{leaveTypeId}/{year}` : définir les jours acquis et ajustements.
- `GET /api/v1/holidays?year={année}` : lister les jours fériés.
- `POST /api/v1/holidays` : créer un jour férié.
- `PUT /api/v1/holidays/{id}` : modifier/désactiver un jour férié.
- `GET /api/v1/audit` : consulter le journal d’audit avec filtres.

### Codes de retour principaux

- `200` / `201` : succès.
- `400` : requête invalide.
- `401` : non authentifié.
- `403` : non autorisé.
- `404` : ressource introuvable.
- `409` : conflit métier, concurrence, solde insuffisant au moment de la décision, chevauchement non acquitté.
- `422` : règle métier de validation non satisfaite.

## 9. Risques et points ouverts

### Hypothèses retenues

- **H-01 — Jours ouvrés** : les durées sont calculées en jours ouvrés entiers, du lundi au vendredi, hors jours fériés paramétrés. Pas de demi-journée ni d’horaire.
- **H-02 — Chevauchement** : le ratio correspond aux jours ouvrés de la demande couverts par l’union des absences approuvées d’autres membres de la même équipe. Le seuil est strictement supérieur à 50 %. L’avertissement n’est pas bloquant mais doit être acquitté avant décision.
- **H-03 — Soldes** : les soldes sont annuels et rattachés à l’année de la date de début. Les demandes à cheval sur deux années sont interdites dans le MVP. Les demandes en attente ne réservent pas de solde.
- **H-04 — Organisation** : un employé soumet dans une équipe disposant d’un manager. Un manager peut manager plusieurs équipes. Une demande est rattachée à l’équipe au moment de la soumission.
- **H-05 — Rôles cumulables** : manager et admin RH peuvent aussi soumettre des demandes. L’admin RH peut consulter tous les calendriers en lecture seule.
- **H-06 — Comptes et équipes** : le provisionnement des utilisateurs et équipes est supposé externe/importé. Le MVP ne couvre pas la gestion complète des comptes.
- **H-07 — Dates** : aucune restriction de préavis, de date passée ou de date future n’est imposée dans cette version.
- **H-08 — Maladie** : le type maladie est modélisé comme un type de congé à suivi de solde, conformément au brief. Aucun justificatif ni diagnostic n’est géré.
- **H-09 — Annulation/modification** : une demande soumise ne peut pas être annulée ou modifiée dans le MVP.
- **H-10 — Authentification** : JWT avec authentification locale email/mot de passe. SSO/MFA hors périmètre.
- **H-11 — Base SQL** : SQL Server ou PostgreSQL sera retenu en étape architecture avec justification. La spécification reste indépendante de la technologie SQL.
- **H-12 — Jours fériés** : bien que non cités explicitement dans le brief, les jours fériés sont paramétrables par l’admin RH pour garantir un calcul correct des jours ouvrés.
- **H-13 — Emails** : les notifications sont asynchrones, avec journalisation et 3 tentatives. Aucune garantie de livraison immédiate.
- **H-14 — Admin RH** : l’admin RH peut consulter les calendriers de toutes les équipes et les soldes, mais ne peut pas décider des demandes sans rôle manager.

### Risques

- **R-01 — Interprétation du chevauchement** : le calcul par union de jours peut ne pas correspondre à une interprétation RH plus stricte. L’UI devra expliquer clairement le ratio et les jours concernés.
- **R-02 — Concurrence sur soldes** : plusieurs approbations simultanées peuvent créer des conflits. À mitiger par validation transactionnelle et gestion de version.
- **R-03 — Email** : délivrabilité, spam, serveur SMTP indisponible. À mitiger par logs, retries et supervision.
- **R-04 — Données sensibles** : le type maladie peut être sensible. Éviter tout détail médical et restreindre l’accès.
- **R-05 — Migration** : reprise des soldes existants depuis fichiers RH à prévoir dans un lot ultérieur.
- **R-06 — Organisation** : changement de manager ou d’équipe en cours d’année. Le MVP fige l’équipe à la soumission ; une règle de réaffectation sera peut-être nécessaire.

### Points ouverts à trancher avec le métier

1. Faut-il autoriser les demi-journées ou les congés en heures ?
2. Faut-il autoriser les demandes rétroactives, notamment pour la maladie ?
3. Faut-il permettre l’annulation ou la modification d’une demande `PENDING` ?
4. Faut-il gérer les demandes à cheval sur deux années civiles avec split de solde ?
5. Faut-il gérer plusieurs managers par équipe ou une délégation de validation ?
6. Faut-il gérer des justificatifs maladie et leur confidentialité ?
7. Quelle est la source officielle des utilisateurs et équipes : import CSV, annuaire, saisie RH ?
8. Quelles durées de conservation RGPD retenir pour demandes, soldes, audit et notifications ?
9. Faut-il un export paie ou un reporting RH ?
10. Faut-il imposer un délai de prévenance pour certains types de congé ?