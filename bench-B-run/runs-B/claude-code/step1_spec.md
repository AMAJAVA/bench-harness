# TeamLeave — Spécification technico-fonctionnelle

| Élément | Valeur |
|---|---|
| Produit | TeamLeave, application interne de gestion des congés |
| Client | PME de 50 personnes |
| Source | `00_brief.md` (brief produit) |
| Étape | 1 sur 4 (spécification). L'architecture (étape 2) et l'implémentation (étapes 3 et 4) ne sont pas traitées ici. |
| Version | 1.0 |

Conventions du document :

- **US-xx** : user story ; **CA-xx.y** : critère d'acceptation n° y de la US-xx.
- **RB-xx** : règle métier ; **H-xx** : hypothèse retenue faute de précision du brief (détail en section 9).
- **ENF-xx** : exigence non fonctionnelle.
- « Jour ouvré » est défini par RB-07. « Équipe » est défini par RB-03.
- Les noms d'entités et d'attributs de la section 6 sont fonctionnels ; le nommage technique sera fixé à l'étape 2.

---

## 1. Contexte et objectifs

### 1.1 Contexte

La PME (50 salariés) gère aujourd'hui ses congés sans outil dédié. Le brief demande une application interne, **TeamLeave**, qui couvre :

- la soumission des demandes de congés par les employés ;
- la validation ou le refus par le manager ;
- le suivi des soldes par type de congé ;
- la visibilité des absences de l'équipe ;
- les notifications email.

Contraintes techniques imposées par le brief, rappelées pour cadrage. Elles seront détaillées et justifiées à l'étape 2 :

- backend ASP.NET Core (dernière LTS) avec EF Core ;
- base SQL Server ou PostgreSQL, le choix étant à justifier à l'étape 2 ;
- frontend React + TypeScript (Vite) ;
- tests xUnit côté backend ;
- authentification JWT avec rôles.

### 1.2 Objectifs

| ID | Objectif | Indicateur de réussite |
|---|---|---|
| OBJ-1 | Dématérialiser le cycle de vie d'une demande de congé (soumission → décision → décompte). | 100 % des demandes passent par l'outil ; chaque demande a un statut et un historique consultables. |
| OBJ-2 | Fiabiliser les soldes. | Le solde n'est débité qu'à l'approbation (RB-12) ; chaque variation est tracée (RB-26) ; un solde n'est jamais négatif (RB-11). |
| OBJ-3 | Éviter les chevauchements critiques dans une équipe. | Toute demande qui dépasse le seuil de RB-16 est signalée au manager avant décision (RB-17, RB-18). |
| OBJ-4 | Informer les acteurs sans délai. | Un email est émis à la soumission (au manager) et à la décision (à l'employé) (RB-21, RB-22). |
| OBJ-5 | Donner à l'admin RH la maîtrise du paramétrage. | Les types de congé, les soldes, les utilisateurs et les jours fériés sont administrables sans intervention technique. |

---

## 2. Périmètre (inclus / exclu)

### 2.1 Inclus

| Domaine | Contenu |
|---|---|
| Authentification | Connexion par identifiant (email) et mot de passe, émission d'un jeton JWT portant les rôles, déconnexion (H-18). |
| Demandes de congé | Simulation (calcul des jours avant envoi), soumission, consultation, historique, annulation (H-12). |
| Types de congé | 4 types initiaux (CP, RTT, Maladie, Autre). L'admin RH peut créer, modifier ou désactiver un type. |
| Soldes | Un solde par employé et par type soumis à décompte. Opérations : allocation et ajustement par l'admin RH, débit à l'approbation, recrédit à l'annulation d'une demande approuvée. Toutes les opérations sont tracées. |
| Validation | Approbation ou refus par l'approbateur (RB-04). Le commentaire est obligatoire en cas de refus. |
| Détection de chevauchement | Calcul du taux d'absence de l'équipe, signalement au manager, acquittement avant approbation (RB-16 à RB-18). |
| Calendrier d'équipe | Vue des absences approuvées sur une période, filtrée selon le rôle (RB-23). |
| Notifications email | Soumission (au manager), décision (à l'employé), annulation d'une demande approuvée (au manager, H-13). |
| Administration RH | Utilisateurs (création, rôles, rattachement, désactivation), types de congé, soldes, jours fériés. |
| Traçabilité | Historique des statuts de demande, mouvements de solde, journal des actions d'administration. |

### 2.2 Exclu (hors périmètre de la version 1)

| Élément exclu | Justification / renvoi |
|---|---|
| Demi-journées et congés en heures | H-04 |
| Acquisition automatique des droits (ex. 2,08 j de CP par mois) et remise à zéro annuelle des soldes | H-08 |
| Délégation et intérim de manager | H-26 |
| SSO / annuaire d'entreprise (Azure AD, LDAP) | H-18 |
| Modification d'une demande déjà soumise (il faut l'annuler puis la resoumettre) | H-11 |
| Export paie, intégration SIRH, export comptable | Non demandé par le brief |
| Justificatifs (arrêt maladie, pièces jointes) | Le brief ne le demande pas. Il s'agit aussi de données de santé (ENF-RGPD). |
| Notifications autres qu'email (push, SMS, Teams/Slack) | Non demandé par le brief |
| Application mobile native (seule l'interface web responsive est prévue) | Non demandé par le brief |
| Multi-entités, multi-pays, calendriers de jours fériés multiples | H-25 |
| Paramétrage du seuil de chevauchement | H-27 (fixé à 50 % par le brief) |
| Reporting et statistiques RH | Non demandé par le brief |
| Interface multilingue (l'interface est en français uniquement) | H-25 |

---

## 3. Acteurs et rôles (matrice des permissions)

### 3.1 Acteurs

| Acteur | Description |
|---|---|
| **Employé** | Tout utilisateur actif. Il soumet et suit ses demandes, consulte ses soldes et le calendrier de son équipe. |
| **Manager** | Employé qui a au moins un subordonné direct. Il décide des demandes de son équipe (RB-04) et voit le détail des absences de son équipe. |
| **Admin RH** | Employé chargé du paramétrage : utilisateurs, types, soldes, jours fériés. Il est l'approbateur de secours des employés sans manager (RB-04). |
| **Système** | Acteur technique. Il calcule les jours et les chevauchements, envoie les emails et trace les opérations. |

Les rôles sont **cumulatifs** (RB-01). Tout utilisateur possède le rôle Employé. Un manager ou un admin RH reste un employé pour ses propres demandes.

### 3.2 Matrice des permissions

Légende :

- **O** : autorisé.
- **P** : autorisé sur un périmètre restreint, précisé dans la colonne « Périmètre ».
- **—** : interdit (réponse 403, ou 404 si l'existence de la ressource ne doit pas être révélée, voir ENF-S7).

| # | Action | Employé | Manager | Admin RH | Périmètre / règle |
|---|---|---|---|---|---|
| P-01 | Se connecter, consulter son profil | O | O | O | RB-02 |
| P-02 | Consulter ses propres soldes et mouvements | O | O | O | — |
| P-03 | Simuler / soumettre une demande pour soi | O | O | O | RB-05 à RB-11 |
| P-04 | Consulter ses propres demandes et leur historique | O | O | O | — |
| P-05 | Annuler sa propre demande | P | P | P | RB-14 |
| P-06 | Soumettre une demande pour le compte d'un autre | — | — | — | H-11 |
| P-07 | Consulter le calendrier d'équipe sans type d'absence (collègues de même manager) | O | O | O | RB-23 |
| P-08 | Consulter le calendrier de ses subordonnés directs avec type d'absence | — | P | — | P : ses subordonnés directs. RB-23 |
| P-09 | Consulter le calendrier de toute l'entreprise avec type d'absence | — | — | O | RB-23, H-30 |
| P-10 | Lister les demandes en attente qu'on doit décider | — | P | P | Manager : ses subordonnés directs. Admin RH : employés sans manager. RB-04 |
| P-11 | Consulter l'analyse de chevauchement d'une demande | — | P | P | Même périmètre que P-10, plus lecture RH globale. RB-19 |
| P-12 | Approuver / refuser une demande | — | P | P | RB-04, RB-13, RB-15, RB-18. Jamais sa propre demande. |
| P-13 | Consulter toutes les demandes de l'entreprise | — | — | O | H-30 |
| P-14 | Gérer les types de congé | — | — | O | RB-24 |
| P-15 | Allouer / ajuster un solde, consulter les soldes de tous | — | — | O | RB-25 |
| P-16 | Gérer les utilisateurs (création, rôles, rattachement, désactivation) | — | — | O | RB-02, RB-03, RB-27 |
| P-17 | Gérer les jours fériés | — | — | O | RB-07 |
| P-18 | Consulter le journal d'administration | — | — | O | RB-26 |

Les autorisations sont vérifiées **côté serveur** à chaque appel (ENF-S3). Masquer un élément dans l'interface ne vaut pas contrôle d'accès.

---

## 4. User stories numérotées, chacune avec critères d'acceptation testables (format Given/When/Then)

Conventions des critères d'acceptation :

- Sauf mention contraire, « aujourd'hui » vaut le lundi 2026-03-02 (fuseau Europe/Paris).
- Aucun jour férié n'est défini sur les dates citées, sauf mention contraire.
- Les codes HTTP renvoient à la section 8.
- Une seule issue observable par « Then ». Les « And » ajoutent des issues vérifiables.

### 4.1 Authentification

#### US-01 — Se connecter

> En tant qu'utilisateur, je veux me connecter avec mon email et mon mot de passe afin d'accéder aux fonctions de mes rôles.

Règles appliquées : RB-01, RB-02, RB-27

- **CA-01.1** — Given un utilisateur actif avec les rôles {Employé, Manager} et un mot de passe valide, When il soumet ses identifiants, Then la réponse est 200 avec un jeton JWT. And le jeton contient l'identifiant de l'utilisateur et les rôles Employé et Manager.
- **CA-01.2** — Given un mot de passe incorrect, When l'utilisateur soumet ses identifiants, Then la réponse est 401. And le message ne précise pas lequel de l'email ou du mot de passe est faux.
- **CA-01.3** — Given un utilisateur désactivé, When il soumet des identifiants corrects, Then la réponse est 401.
- **CA-01.4** — Given 5 échecs consécutifs sur le même compte en moins de 15 minutes, When une 6e tentative est faite, même avec le bon mot de passe, Then la réponse est 429 avec le code `ACCOUNT_LOCKED`. And toute connexion à ce compte est refusée pendant 15 minutes (ENF-S5).
- **CA-01.5** — Given un jeton expiré, When l'utilisateur appelle une ressource protégée, Then la réponse est 401.

### 4.2 Employé

#### US-02 — Consulter mes soldes

> En tant qu'employé, je veux voir mes soldes par type afin de savoir combien de jours je peux poser.

Règles appliquées : RB-09, RB-10, RB-11

- **CA-02.1** — Given un solde CP de 25 et une demande CP de 3 jours en attente, When je consulte mes soldes, Then le CP affiche solde = 25, en attente = 3, disponible = 22.
- **CA-02.2** — Given le type Maladie, qui n'est pas soumis à décompte, When je consulte mes soldes, Then le type Maladie n'apparaît pas dans la liste des soldes.
- **CA-02.3** — Given un autre employé, When j'appelle la ressource soldes de cet employé, Then la réponse est 403.

#### US-03 — Simuler une demande

> En tant qu'employé, je veux connaître le nombre de jours décomptés et mon solde restant avant de soumettre afin d'éviter une demande invalide.

Règles appliquées : RB-05, RB-06, RB-07, RB-10, RB-19

- **CA-03.1** — Given une période du vendredi 2026-03-06 au mardi 2026-03-10, When je lance la simulation pour le type CP, Then le nombre de jours décomptés est 3 (vendredi, lundi, mardi).
- **CA-03.2** — Given le lundi 2026-04-06 (lundi de Pâques) déclaré jour férié, When je simule du 2026-04-06 au 2026-04-08, Then le nombre de jours décomptés est 2.
- **CA-03.3** — Given une période du samedi 2026-03-07 au dimanche 2026-03-08, When je simule, Then la réponse est 422 avec le code d'erreur `NO_WORKING_DAY`.
- **CA-03.4** — Given une simulation valide, When elle est exécutée, Then aucune demande n'est créée et aucun email n'est envoyé.
- **CA-03.5** — Given une simulation valide, When le résultat est renvoyé, Then il ne contient aucune information sur les absences des collègues (RB-19).

#### US-04 — Soumettre une demande de congé

> En tant qu'employé, je veux soumettre une demande (dates de début/fin, type, commentaire facultatif) afin d'obtenir l'accord de mon manager.

Règles appliquées : RB-04, RB-05, RB-06, RB-07, RB-08, RB-09, RB-10, RB-13, RB-16, RB-21, RB-26

- **CA-04.1** — Given un solde CP disponible de 10, When je soumets une demande CP du 2026-03-09 au 2026-03-13, Then la réponse est 201. And la demande a le statut « En attente » et 5 jours décomptés. And le solde CP vaut toujours 10 et le disponible vaut 5.
- **CA-04.2** — Given un solde CP disponible de 2, When je soumets une demande CP de 3 jours ouvrés, Then la réponse est 422 avec le code `INSUFFICIENT_BALANCE`. And aucune demande n'est créée.
- **CA-04.3** — Given une date de fin antérieure à la date de début, When je soumets, Then la réponse est 400.
- **CA-04.4** — Given le type CP, qui n'autorise pas la rétroactivité, et une date de début au 2026-02-27 (passée), When je soumets, Then la réponse est 422 avec le code `PAST_DATE_NOT_ALLOWED`.
- **CA-04.5** — Given le type Maladie, qui autorise la rétroactivité, et une période du 2026-02-26 au 2026-02-27, When je soumets, Then la réponse est 201 avec le statut « En attente ».
- **CA-04.6** — Given une demande RTT approuvée le 2026-03-11, When je soumets une demande CP du 2026-03-10 au 2026-03-12, Then la réponse est 422 avec le code `OVERLAPS_OWN_REQUEST`.
- **CA-04.7** — Given un type désactivé, When je soumets une demande de ce type, Then la réponse est 422 avec le code `LEAVE_TYPE_INACTIVE`.
- **CA-04.8** — Given une soumission acceptée, When la demande est créée, Then l'analyse de chevauchement (RB-16) est calculée et son résultat (alerte oui/non, taux maximal) est enregistré sur la demande.
- **CA-04.9** — Given une soumission acceptée, When la demande est créée, Then une entrée d'historique « création → En attente » est enregistrée avec mon identifiant et l'horodatage (RB-26).
- **CA-04.10** — Given un solde CP disponible de 5, When je soumets simultanément deux demandes CP de 3 jours ouvrés sur des périodes distinctes, Then une seule est créée (201). And l'autre reçoit 422 avec le code `INSUFFICIENT_BALANCE`. And le disponible CP vaut 2 (ENF-I1).

#### US-05 — Consulter mes demandes

> En tant qu'employé, je veux voir la liste de mes demandes et leur historique afin de suivre leur traitement.

Règles appliquées : RB-13, RB-19, RB-26

- **CA-05.1** — Given 3 demandes (En attente, Approuvée, Refusée), When je consulte mes demandes, Then les 3 sont listées avec type, dates, jours décomptés et statut, triées par date de début décroissante.
- **CA-05.2** — Given une demande refusée, When j'en consulte le détail, Then le commentaire de refus, le nom du décideur et la date de décision sont affichés.
- **CA-05.3** — Given ma demande qui porte une alerte de chevauchement, When j'en consulte le détail, Then aucune information d'alerte ni aucun taux n'est affiché (RB-19).
- **CA-05.4** — Given une demande d'un autre employé, When j'en demande le détail par son identifiant, Then la réponse est 404.

#### US-06 — Annuler une demande

> En tant qu'employé, je veux annuler une demande en attente, ou une demande approuvée qui n'a pas commencé, afin de corriger mes plans.

Règles appliquées : RB-13, RB-14, RB-22, RB-25, RB-26

- **CA-06.1** — Given ma demande CP de 3 jours « En attente », When je l'annule, Then son statut devient « Annulée ». And le disponible CP augmente de 3. And le solde CP est inchangé.
- **CA-06.2** — Given ma demande CP de 3 jours « Approuvée » commençant le 2026-03-09 et un solde CP de 12, When je l'annule le 2026-03-02, Then son statut devient « Annulée ». And le solde CP vaut 15. And un mouvement « Recrédit » de +3 est tracé. And un email d'annulation est envoyé à mon manager (H-13).
- **CA-06.3** — Given ma demande approuvée dont la date de début est aujourd'hui ou passée, When je l'annule, Then la réponse est 422 avec le code `ALREADY_STARTED`. And le statut reste « Approuvée ».
- **CA-06.4** — Given ma demande « Refusée » ou « Annulée », When je l'annule, Then la réponse est 409.

#### US-07 — Consulter le calendrier de mon équipe

> En tant qu'employé, je veux voir les absences approuvées de mon équipe afin d'organiser mes demandes.

Règles appliquées : RB-03, RB-23

- **CA-07.1** — Given que je suis rattaché au manager M, qui a 4 subordonnés directs, When je consulte le calendrier sur un mois, Then je vois les absences approuvées de ces 4 personnes (moi compris), affichées « Absent » sans type de congé.
- **CA-07.2** — Given une demande « En attente » d'un collègue, When je consulte le calendrier, Then elle n'apparaît pas.
- **CA-07.3** — Given une période demandée de plus de 93 jours, When je consulte le calendrier, Then la réponse est 400 (ENF-P3).

### 4.3 Manager

#### US-08 — Lister les demandes à décider

> En tant que manager, je veux voir les demandes en attente de mon équipe afin de les traiter.

Règles appliquées : RB-03, RB-04, RB-19

- **CA-08.1** — Given 2 demandes en attente de mes subordonnés directs et 1 demande en attente d'un employé d'une autre équipe, When je consulte ma file, Then exactement 2 demandes sont listées.
- **CA-08.2** — Given une demande en attente dont l'analyse recalculée dépasse le seuil, When je consulte ma file, Then la ligne porte un indicateur visible « Chevauchement > 50 % ».
- **CA-08.3** — Given que je suis aussi employé et que j'ai une demande en attente, When je consulte ma file, Then ma propre demande n'y figure pas.

#### US-09 — Être alerté d'un chevauchement critique avant de décider

> En tant que manager, je veux être averti quand une demande ferait coïncider l'absence avec celles de plus de la moitié de l'équipe afin de décider en connaissance de cause.

Règles appliquées : RB-16, RB-17, RB-18, RB-19

- **CA-09.1** — Given l'équipe {A, B, C, D} du manager M, une demande de A du lundi au mercredi, B absent (approuvé) mardi et C absent (approuvé) mardi, When M ouvre la demande, Then l'alerte est affichée avec le mardi comme jour concerné, un taux de 67 % (2/3) et les noms de B et C.
- **CA-09.2** — Given l'équipe {A, B, C, D, E}, une demande de A et seuls B et C absents (approuvés) le même jour, When M ouvre la demande, Then aucune alerte n'est affichée (2/4 = 50 %, non strictement supérieur).
- **CA-09.3** — Given une demande sans alerte à la soumission, puis l'approbation d'absences de collègues qui portent le taux à 75 %, When M ouvre la demande, Then l'alerte est affichée (recalcul, RB-17).
- **CA-09.4** — Given une demande en alerte, When M tente de l'approuver sans acquittement explicite, Then la réponse est 422 avec le code `OVERLAP_NOT_ACKNOWLEDGED`. And la demande reste « En attente ».
- **CA-09.5** — Given une demande en alerte, When M l'approuve avec acquittement, Then la demande est « Approuvée ». And l'acquittement (auteur, horodatage, taux constaté) est enregistré.

#### US-10 — Approuver une demande

> En tant que manager, je veux approuver une demande de mon équipe afin de valider l'absence et débiter le solde.

Règles appliquées : RB-04, RB-11, RB-12, RB-13, RB-15, RB-18, RB-22, RB-25, RB-26

- **CA-10.1** — Given une demande CP de 5 jours en attente d'un subordonné direct dont le solde CP est 20, When je l'approuve, Then son statut devient « Approuvée ». And le solde CP vaut 15. And un mouvement « Débit » de −5 lié à la demande est tracé.
- **CA-10.2** — Given une approbation réussie, When elle est enregistrée, Then un email de décision est envoyé à l'employé.
- **CA-10.3** — Given une demande CP de 5 jours en attente et un solde CP de 20, When deux approbations de cette même demande arrivent simultanément, Then une seule réussit. And l'autre reçoit 409. And un seul mouvement « Débit » de −5 est tracé et le solde CP vaut 15 (RB-15, ENF-I1).
- **CA-10.4** — Given une demande Maladie, type non soumis à décompte, When je l'approuve, Then aucun mouvement de solde n'est créé.
- **CA-10.5** — Given une demande d'un employé qui n'est pas mon subordonné direct, When je l'approuve, Then la réponse est 403.
- **CA-10.6** — Given une demande déjà décidée, When je tente de l'approuver, Then la réponse est 409.
#### US-11 — Refuser une demande

> En tant que manager, je veux refuser une demande en motivant ma décision afin que l'employé comprenne le refus.

Règles appliquées : RB-04, RB-13, RB-15, RB-18, RB-22, RB-26

- **CA-11.1** — Given une demande en attente d'un subordonné direct, When je la refuse avec le commentaire « Période de clôture », Then son statut devient « Refusée ». And le commentaire est enregistré. And le solde est inchangé.
- **CA-11.2** — Given un commentaire vide ou composé uniquement d'espaces, When je refuse, Then la réponse est 400 avec le code `REJECTION_COMMENT_REQUIRED`. And la demande reste « En attente ».
- **CA-11.3** — Given un refus enregistré, When il est validé, Then un email de décision contenant le commentaire est envoyé à l'employé.
- **CA-11.4** — Given une demande en alerte de chevauchement, When je la refuse avec commentaire et sans acquittement, Then le refus est accepté (l'acquittement n'est exigé que pour l'approbation, RB-18).

#### US-12 — Consulter le calendrier détaillé de mon équipe

> En tant que manager, je veux voir les absences approuvées de mes subordonnés avec leur type afin de planifier la charge.

Règles appliquées : RB-03, RB-23

- **CA-12.1** — Given mes 4 subordonnés directs, When je consulte le calendrier « Mon équipe », Then je vois leurs absences approuvées avec le libellé du type (Maladie compris).
- **CA-12.2** — Given un employé d'une autre équipe, When je consulte le calendrier « Mon équipe », Then ses absences n'apparaissent pas.

### 4.4 Notifications

#### US-13 — Notifier le manager à la soumission

> En tant que manager, je veux recevoir un email à chaque nouvelle demande de mon équipe afin de la traiter rapidement.

Règles appliquées : RB-04, RB-17, RB-21, RB-28

- **CA-13.1** — Given la soumission d'une demande par un subordonné direct, When elle est créée, Then un email est envoyé au manager. Il contient le nom de l'employé, le type, les dates, les jours décomptés et un lien vers la demande.
- **CA-13.2** — Given une demande en alerte à la soumission, When l'email est généré, Then il mentionne « Chevauchement d'équipe supérieur à 50 % ».
- **CA-13.3** — Given un employé sans manager, When il soumet, Then l'email est envoyé à tous les admins RH actifs, sauf au demandeur.
- **CA-13.4** — Given un serveur SMTP indisponible, When une demande est soumise, Then la demande est créée (201) et l'email est mis en file pour nouvel essai (RB-28).

#### US-14 — Notifier l'employé à la décision

> En tant qu'employé, je veux recevoir un email quand ma demande est décidée afin d'être informé sans me connecter.

Règles appliquées : RB-15, RB-22, RB-28

- **CA-14.1** — Given une demande approuvée, When la décision est enregistrée, Then un email « Demande approuvée » est envoyé au demandeur avec type, dates et jours décomptés.
- **CA-14.2** — Given une demande refusée, When la décision est enregistrée, Then un email « Demande refusée » contenant le commentaire du manager est envoyé au demandeur.
- **CA-14.3** — Given une demande annulée alors qu'elle était « En attente », When l'annulation est enregistrée, Then aucun email de décision n'est envoyé.

### 4.5 Admin RH

#### US-15 — Gérer les types de congé

> En tant qu'admin RH, je veux créer, modifier et désactiver des types de congé afin d'adapter l'outil à la politique de l'entreprise.

Règles appliquées : RB-08, RB-24, RB-26

- **CA-15.1** — Given aucun type de code « FORM », When je crée le type « FORM / Formation » soumis à décompte, Then la réponse est 201 et le type est proposé à la soumission.
- **CA-15.2** — Given un type de code « CP » existant, When je crée un type de code « CP », Then la réponse est 409.
- **CA-15.3** — Given un type utilisé par au moins une demande, When je tente de le supprimer, Then la réponse est 409 et seule la désactivation est proposée.
- **CA-15.4** — Given un type désactivé, When un employé consulte la liste des types à la soumission, Then ce type n'y figure pas. And les demandes existantes de ce type restent consultables et décidables.
- **CA-15.5** — Given un utilisateur sans rôle Admin RH, When il appelle la création de type, Then la réponse est 403.

#### US-16 — Paramétrer les soldes

> En tant qu'admin RH, je veux allouer ou ajuster le solde d'un employé pour un type afin de refléter ses droits.

Règles appliquées : RB-09, RB-10, RB-11, RB-25, RB-26

- **CA-16.1** — Given un employé avec un solde CP de 0, When j'alloue +25 avec le motif « Droits 2026 », Then le solde CP vaut 25. And un mouvement « Allocation » de +25 est tracé avec mon identifiant et le motif.
- **CA-16.2** — Given un solde CP de 10 et 4 jours en attente, When j'ajuste de −8, Then la réponse est 422 avec le code `BALANCE_BELOW_PENDING`, car le disponible deviendrait −2.
- **CA-16.3** — Given un ajustement sans motif, When je le soumets, Then la réponse est 400.
- **CA-16.4** — Given le type Maladie (non soumis à décompte), When je tente d'allouer un solde, Then la réponse est 422 avec le code `LEAVE_TYPE_NOT_TRACKED`.
- **CA-16.5** — Given un employé, When je consulte son relevé, Then tous ses mouvements de solde sont listés par ordre chronologique, avec le solde résultant après chaque mouvement.

#### US-17 — Gérer les utilisateurs

> En tant qu'admin RH, je veux créer des comptes, attribuer des rôles et rattacher chaque employé à son manager afin que les équipes et les circuits de validation soient corrects.

Règles appliquées : RB-01, RB-02, RB-03, RB-04, RB-27

- **CA-17.1** — Given un email inexistant, When je crée l'utilisateur « Alice Martin » avec le rôle Employé et le manager M, Then la réponse est 201. And Alice apparaît dans l'équipe de M.
- **CA-17.2** — Given un email déjà utilisé, When je crée un utilisateur avec cet email, Then la réponse est 409.
- **CA-17.3** — Given que M est le manager de N, When je définis N comme manager de M, Then la réponse est 422 avec le code `HIERARCHY_CYCLE`.
- **CA-17.4** — Given un utilisateur avec 2 subordonnés actifs, When je le désactive, Then la réponse est 422 avec le code `HAS_ACTIVE_REPORTS`.
- **CA-17.5** — Given un utilisateur sans subordonné avec une demande « En attente », When je le désactive, Then il ne peut plus se connecter. And sa demande en attente passe à « Annulée » avec le motif « Compte désactivé ».
- **CA-17.6** — Given une demande en attente de E, dont le manager passe de M1 à M2, When M2 consulte sa file, Then la demande y figure. And M1 ne peut plus la décider (403).

#### US-18 — Gérer les jours fériés

> En tant qu'admin RH, je veux maintenir la liste des jours fériés afin que le décompte des jours soit exact.

Règles appliquées : RB-06, RB-07, RB-24

- **CA-18.1** — Given aucun jour férié au 2026-05-01, When je crée « 2026-05-01 Fête du travail », Then une simulation du 2026-04-30 au 2026-05-01 décompte 1 jour.
- **CA-18.2** — Given un jour férié existant au 2026-05-01, When j'en crée un second à la même date, Then la réponse est 409.
- **CA-18.3** — Given une demande déjà soumise avec 5 jours décomptés, When j'ajoute un jour férié dans sa période, Then le nombre de jours décomptés de cette demande reste 5 (RB-06).

#### US-19 — Consulter l'ensemble des demandes et le calendrier global

> En tant qu'admin RH, je veux consulter toutes les demandes et toutes les absences approuvées afin d'assurer le suivi RH.

Règles appliquées : RB-19, RB-23, RB-26

- **CA-19.1** — Given des demandes dans 3 équipes, When je liste les demandes avec le filtre statut = « En attente », Then toutes les demandes en attente de l'entreprise sont listées.
- **CA-19.2** — Given le calendrier global, When je le consulte sur un mois, Then toutes les absences approuvées s'affichent avec leur type, et un filtre par manager est disponible.
- **CA-19.3** — Given une action d'administration (création d'utilisateur, désactivation de type…), When je consulte le journal d'administration, Then l'action y figure avec auteur, horodatage et objet concerné.

### 4.6 Traçabilité US → RB

| RB | US qui l'appliquent |
|---|---|
| RB-01 | US-01, US-17 |
| RB-02 | US-01, US-17 |
| RB-03 | US-07, US-08, US-12, US-17 |
| RB-04 | US-04, US-08, US-10, US-11, US-13, US-17 |
| RB-05 | US-03, US-04 |
| RB-06 | US-03, US-04, US-18 |
| RB-07 | US-03, US-04, US-18 |
| RB-08 | US-04, US-15 |
| RB-09 | US-02, US-04, US-16 |
| RB-10 | US-02, US-03, US-04, US-16 |
| RB-11 | US-02, US-10, US-16 |
| RB-12 | US-10 |
| RB-13 | US-04, US-05, US-06, US-10, US-11 |
| RB-14 | US-06 |
| RB-15 | US-10, US-11, US-14 |
| RB-16 | US-04, US-09 |
| RB-17 | US-09, US-13 |
| RB-18 | US-09, US-10, US-11 |
| RB-19 | US-03, US-05, US-08, US-09, US-19 |
| RB-20 | Transverse (règle de calcul de taux appliquée par RB-16 ; testée via US-09) |
| RB-21 | US-04, US-13 |
| RB-22 | US-06, US-10, US-11, US-14 |
| RB-23 | US-07, US-12, US-19 |
| RB-24 | US-15, US-18 |
| RB-25 | US-06, US-10, US-16 |
| RB-26 | US-04, US-05, US-06, US-10, US-11, US-15, US-16, US-19 |
| RB-27 | US-01, US-17 |
| RB-28 | US-13, US-14 |

---

## 5. Règles métier numérotées (incluant la règle de chevauchement à 50 %)

### 5.1 Utilisateurs, rôles, équipes

**RB-01 — Rôles cumulatifs.** Trois rôles existent : Employé, Manager, Admin RH. Tout utilisateur actif possède le rôle Employé. Les rôles Manager et Admin RH s'y ajoutent. (H-01)

**RB-02 — Identité et authentification.**

- Un utilisateur est identifié par un email unique (insensible à la casse).
- Seul un utilisateur actif peut s'authentifier.
- Le jeton émis porte l'identifiant de l'utilisateur et ses rôles.
- Le jeton a une durée de validité limitée (ENF-S2).
- Les comptes sont créés par l'admin RH. Il n'y a pas d'auto-inscription (H-18).

**RB-03 — Rattachement hiérarchique et équipe.**

- Un utilisateur a **au plus un** manager direct.
- Ce manager doit être un utilisateur actif qui possède le rôle Manager.
- Un utilisateur ne peut pas être son propre manager.
- La hiérarchie ne contient aucun cycle.
- **L'équipe d'un manager M** est l'ensemble des utilisateurs actifs dont le manager direct est M. M lui-même n'en fait pas partie (H-02).
- **L'équipe d'un employé E** est l'équipe de son manager. Elle inclut E.

**RB-04 — Approbateur d'une demande.**

- L'approbateur d'une demande est le **manager direct actuel** du demandeur, au moment de la décision (H-22).
- Si le demandeur n'a pas de manager, tout admin RH actif autre que le demandeur est approbateur (H-03).
- Personne ne peut décider de sa propre demande.
- L'admin RH ne décide pas des demandes d'un employé qui a un manager (H-31).

### 5.2 Demande de congé : validité et décompte

**RB-05 — Validité des dates.**

- La demande porte sur des journées entières (H-04), avec une date de début et une date de fin incluses.
- La date de début est antérieure ou égale à la date de fin.
- Les dates s'entendent dans le fuseau Europe/Paris.
- La date de début doit être postérieure ou égale à aujourd'hui, sauf pour les types dont l'attribut « rétroactivité autorisée » est vrai (Maladie par défaut) (H-10).
- La période couvre au maximum 366 jours calendaires (`PERIOD_TOO_LONG`, H-34).
- La période ne doit recouper aucune autre demande du même demandeur au statut « En attente » ou « Approuvée » (`OVERLAPS_OWN_REQUEST`, H-33).

**RB-06 — Nombre de jours décomptés.**

- Le nombre de jours décomptés est le nombre de jours ouvrés (RB-07) entre la date de début et la date de fin incluses.
- Il doit être ≥ 1, sinon la demande est rejetée (`NO_WORKING_DAY`).
- Il est calculé à la soumission puis **figé**. Une modification ultérieure des jours fériés ne le change pas (H-05).

**RB-07 — Jour ouvré.** Un jour ouvré est un jour du lundi au vendredi qui ne figure pas dans la liste des jours fériés gérée par l'admin RH (H-05).

**RB-08 — Type actif.**

- Une demande ne peut être soumise que pour un type actif.
- La désactivation d'un type n'affecte pas les demandes existantes. Elles restent consultables, décidables et annulables.

**RB-09 — Soldes et décompte.**

- Un type est soit **soumis à décompte**, soit non.
- Valeurs par défaut des types initiaux (H-06) :
  - CP et RTT sont soumis à décompte ;
  - Maladie et Autre ne le sont pas.
- Pour chaque couple (employé, type soumis à décompte), il existe un solde, de valeur initiale 0.
- Les types non soumis à décompte n'ont pas de solde. Leurs demandes ne vérifient ni ne modifient aucun solde.

**RB-10 — Solde disponible et contrôle à la soumission.**

- Pour un type soumis à décompte : **disponible = solde − somme des jours décomptés des demandes « En attente » de ce type** (H-07).
- Une soumission est rejetée (`INSUFFICIENT_BALANCE`) si les jours décomptés dépassent le disponible.
- Les soldes sont exprimés en jours, avec au plus 2 décimales (H-09).

**RB-11 — Solde jamais négatif.**

- Aucune opération ne peut rendre un solde négatif, qu'il s'agisse d'un débit ou d'un ajustement.
- Un ajustement RH ne peut pas rendre le disponible négatif (`BALANCE_BELOW_PENDING`).

**RB-12 — Débit à l'approbation.**

- Le solde est débité des jours décomptés **au moment de l'approbation**, et seulement à ce moment-là (brief).
- Au moment de l'approbation, le système revérifie que solde ≥ jours décomptés. Si ce n'est pas le cas, l'approbation est rejetée (`INSUFFICIENT_BALANCE`) et la demande reste « En attente ». Ce contrôle est un garde-fou : RB-10 et RB-11 garantissent déjà en permanence que le solde est supérieur ou égal au total des jours en attente, et le cas n'est donc pas atteignable en fonctionnement normal.
- Le débit et le changement de statut sont atomiques : les deux réussissent ou aucun n'est appliqué.

### 5.3 Cycle de vie

**RB-13 — Statuts et transitions autorisées.**

| Statut de départ | Événement | Statut d'arrivée | Acteur |
|---|---|---|---|
| — | Soumission | En attente | Demandeur |
| En attente | Approbation | Approuvée | Approbateur (RB-04) |
| En attente | Refus | Refusée | Approbateur (RB-04) |
| En attente | Annulation | Annulée | Demandeur |
| En attente | Désactivation du compte du demandeur | Annulée | Système (RB-27) |
| Approuvée | Annulation avant la date de début | Annulée | Demandeur (RB-14) |

- Toute autre transition est rejetée (409).
- « Refusée » et « Annulée » sont des statuts terminaux.
- Une demande ne peut pas être modifiée après soumission (H-11).

**RB-14 — Annulation.**

- Le demandeur peut annuler :
  - une demande « En attente » à tout moment ;
  - une demande « Approuvée » dont la date de début est strictement postérieure à aujourd'hui (H-12). Sinon le système renvoie `ALREADY_STARTED`.
- L'annulation d'une demande approuvée soumise à décompte recrédite le solde des jours décomptés (mouvement « Recrédit »).
- L'annulation d'une demande approuvée déclenche un email au manager (H-13).

**RB-15 — Décision et commentaire.**

- Le refus exige un commentaire non vide après suppression des espaces de début et de fin, d'au plus 1 000 caractères (brief, H-29).
- Le commentaire est facultatif à l'approbation (même limite).
- Le décideur et l'horodatage de la décision sont enregistrés.
- Une demande ne peut être décidée qu'une fois. Une décision concurrente sur une demande déjà décidée est rejetée (409).

### 5.4 Chevauchement d'équipe

**RB-16 — Règle de chevauchement à 50 % (interprétation retenue : H-14).**

Soit une demande R de l'employé E, sur la période [début, fin].

1. **Population.**
   - Si E n'a pas de manager, la règle ne s'applique pas : pas d'alerte.
   - Sinon, on note **Collègues(E)** l'équipe de E (RB-03) privée de E, en ne retenant que les utilisateurs actifs.
   - **N = |Collègues(E)|**.
2. **Jours évalués.** On évalue uniquement les jours ouvrés (RB-07) de la période de R.
3. **Absences prises en compte.**
   - Seules comptent les demandes **« Approuvée »** des collègues, tous types confondus (brief).
   - Les demandes « En attente », « Refusée » et « Annulée » sont ignorées.
4. **Taux du jour.**
   - Pour chaque jour évalué J, **A(J)** est le nombre de collègues distincts qui ont une demande approuvée couvrant J.
   - **Taux(J) = A(J) / N** (RB-20).
   - Le demandeur ne figure **ni au numérateur ni au dénominateur**.
5. **Alerte.**
   - La demande est **en alerte** s'il existe au moins un jour évalué J tel que **Taux(J) > 50 %** (strictement).
   - Si N = 0, il n'y a pas d'alerte.
6. **Résultat de l'analyse.** L'analyse renvoie :
   - l'indicateur d'alerte ;
   - le taux maximal ;
   - la liste des jours en dépassement, avec pour chacun son taux et les collègues absents.

Exemples de référence (cas de test) :

| # | Équipe du manager | Demande de A | Absences approuvées des collègues | N | Taux max | Alerte |
|---|---|---|---|---|---|---|
| EX-1 | A, B, C, D | lun → mer | B : mar ; C : mar | 3 | mar : 2/3 = 66,7 % | **Oui** (mar) |
| EX-2 | A, B, C, D, E | lun | B : lun ; C : lun | 4 | 2/4 = 50 % | Non (non strictement > 50 %) |
| EX-3 | A, B | lun → ven | B : jeu | 1 | jeu : 1/1 = 100 % | **Oui** (jeu) |
| EX-4 | A seul | lun → ven | — | 0 | sans objet | Non |
| EX-5 | A, B, C | lun | B : lun approuvé ; C : lun **en attente** | 2 | 1/2 = 50 % | Non (la demande en attente est ignorée) |
| EX-6 | A, B, C, D | sam → dim | B, C, D : sam | 3 | aucun jour ouvré | Sans objet : la demande est rejetée par RB-06 |
| EX-7 | A, B, C | lun → mar | B : lun ; C : mar | 2 | lun : 1/2 ; mar : 1/2 | Non (évaluation jour par jour, pas sur la période) |

**RB-17 — Moments de calcul.**

- L'analyse est calculée :
  - à la soumission : le résultat est enregistré sur la demande et sert à l'email RB-21 ;
  - **à nouveau** à chaque affichage de la demande ou de la file pour l'approbateur ;
  - à nouveau au moment de l'approbation.
- Le résultat recalculé fait foi pour la décision (H-16).

**RB-18 — Signalement et acquittement avant décision.**

- Le manager voit l'alerte avant de décider :
  - dans sa file (indicateur) ;
  - dans le détail de la demande (jours, taux, collègues absents) ;
  - dans l'email de soumission si l'alerte existait déjà à la soumission.
- L'approbation d'une demande en alerte au sens du recalcul exige un **acquittement explicite** du décideur (`OVERLAP_NOT_ACKNOWLEDGED` sinon) (H-15).
- L'acquittement et le taux constaté sont enregistrés.
- Le refus n'exige pas d'acquittement.

**RB-19 — Confidentialité de l'analyse.**

- L'analyse de chevauchement n'est visible que par l'approbateur de la demande et par les admins RH (H-17).
- Le demandeur ne la voit pas, ni dans la simulation ni dans le détail de sa demande.

**RB-20 — Arrondi des taux.**

- Les taux sont calculés en valeur exacte (fraction) pour la comparaison au seuil.
- Ils sont affichés en pourcentage arrondi à l'entier le plus proche (ex. 2/3 → 67 %).
- La comparaison au seuil n'utilise jamais la valeur arrondie.

### 5.5 Notifications

**RB-21 — Email à la soumission.**

- À chaque soumission acceptée, un email est envoyé :
  - à l'approbateur, c'est-à-dire le manager ;
  - à défaut de manager, à tous les admins RH actifs sauf au demandeur.
- Contenu :
  - demandeur, type, dates, jours décomptés ;
  - mention de l'alerte de chevauchement le cas échéant ;
  - lien vers la demande.
- Format des emails : texte simple, en français (H-28).

**RB-22 — Email de décision et d'annulation.**

- À chaque décision, un email est envoyé au demandeur. Il contient la décision, le type, les dates et, en cas de refus, le commentaire.
- L'annulation d'une demande approuvée envoie un email au manager actuel du demandeur (H-13).
- L'annulation d'une demande en attente n'envoie aucun email.
- Les emails ne contiennent jamais l'analyse de chevauchement à destination du demandeur.
- La fiabilité d'envoi est régie par RB-28 (§5.8).

### 5.6 Calendrier

**RB-23 — Visibilité du calendrier.**

- Le calendrier n'affiche que les demandes « Approuvée » (brief, H-21).
- Visibilité selon le rôle :
  - **Employé** : son équipe (RB-03), avec l'affichage « Absent » sans type ;
  - **Manager** : en plus, ses subordonnés directs avec le type ;
  - **Admin RH** : tous les utilisateurs avec le type (H-20, H-30).
- Plage maximale par requête : 93 jours (ENF-P3).

### 5.7 Administration et traçabilité

**RB-24 — Référentiels administrables.**

- **Types de congé.**
  - Le code est unique (2 à 10 caractères alphanumériques majuscules) et le libellé est obligatoire.
  - Les types initiaux CP, RTT, Maladie et Autre ne peuvent pas être supprimés.
  - Un type utilisé par au moins une demande ne peut pas être supprimé, seulement désactivé.
  - Le passage de « soumis à décompte » à « non soumis » est interdit si des soldes non nuls existent pour ce type.
- **Jours fériés.** Il y a au plus un jour férié par date.

**RB-25 — Mouvements de solde.**

- Toute variation de solde résulte d'un mouvement typé :
  - **Allocation** (RH, positif) ;
  - **Ajustement** (RH, positif ou négatif, motif obligatoire) ;
  - **Débit** (système, à l'approbation) ;
  - **Recrédit** (système, à l'annulation d'une demande approuvée).
- Le solde est égal à la somme des mouvements.
- Un mouvement n'est jamais modifié ni supprimé. Une erreur se corrige par un ajustement inverse.

**RB-26 — Traçabilité.**

- Les éléments suivants sont horodatés et attribués à leur auteur, sans possibilité de modification :
  - chaque changement de statut d'une demande (historique) ;
  - chaque mouvement de solde ;
  - chaque action d'administration (utilisateurs, rôles, rattachements, types, jours fériés).

**RB-27 — Désactivation d'un utilisateur.**

- Un utilisateur qui a des subordonnés actifs ne peut pas être désactivé (`HAS_ACTIVE_REPORTS`).
- La désactivation produit les effets suivants :
  - l'utilisateur ne peut plus se connecter ;
  - ses jetons en cours sont refusés dès la requête suivante (ENF-S2) ;
  - ses demandes « En attente » passent à « Annulée » ;
  - ses demandes approuvées futures restent inchangées (H-23).
- Un utilisateur n'est jamais supprimé physiquement par l'application (purge : ENF-RGPD4).

### 5.8 Fiabilité des notifications

**RB-28 — Envoi non bloquant.**

- L'échec d'envoi d'un email n'annule ni ne bloque l'opération métier qui l'a déclenché.
- L'email est journalisé, puis renvoyé au moins 3 fois sur une durée d'au moins 1 heure.
- Après épuisement des tentatives, il est marqué « Échec » et reste consultable par l'admin RH (H-32).

---

## 6. Modèle de données fonctionnel (entités, attributs, relations, sans SQL)

### 6.1 Entités et attributs

Types fonctionnels utilisés : Identifiant, Texte(n), Email, Booléen, Date (jour civil), Horodatage (avec fuseau), Décimal(2), Entier, Énumération.

**Utilisateur**

| Attribut | Type | Obligatoire | Règle |
|---|---|---|---|
| identifiant | Identifiant | Oui | — |
| email | Email | Oui | Unique, insensible à la casse (RB-02) |
| nom, prénom | Texte(100) | Oui | — |
| rôles | Ensemble d'Énumération {Employé, Manager, AdminRH} | Oui | Contient toujours Employé (RB-01) |
| manager | Référence Utilisateur | Non | RB-03 |
| actif | Booléen | Oui | RB-27 |
| secret d'authentification | Valeur non réversible | Oui | Jamais exposé (ENF-S4) |
| compteur d'échecs de connexion, verrouillé jusqu'à | Entier, Horodatage | Non | ENF-S5 |
| créé le, désactivé le | Horodatage | Oui / Non | — |

**TypeConge**

| Attribut | Type | Obligatoire | Règle |
|---|---|---|---|
| identifiant | Identifiant | Oui | — |
| code | Texte(10) | Oui | Unique (RB-24) |
| libellé | Texte(100) | Oui | — |
| soumis à décompte | Booléen | Oui | RB-09 |
| rétroactivité autorisée | Booléen | Oui | RB-05 |
| système (type initial) | Booléen | Oui | Non supprimable (RB-24) |
| actif | Booléen | Oui | RB-08 |

**Solde**

| Attribut | Type | Obligatoire | Règle |
|---|---|---|---|
| utilisateur | Référence Utilisateur | Oui | Unicité du couple (utilisateur, type) |
| type | Référence TypeConge (soumis à décompte) | Oui | RB-09 |
| valeur | Décimal(2) | Oui | ≥ 0 (RB-11) ; = somme des mouvements (RB-25) |
| en attente | Décimal(2), **calculé** | — | RB-10 |
| disponible | Décimal(2), **calculé** | — | valeur − en attente (RB-10) |

**MouvementSolde** (non modifiable)

| Attribut | Type | Obligatoire | Règle |
|---|---|---|---|
| identifiant | Identifiant | Oui | — |
| solde | Référence Solde | Oui | — |
| nature | Énumération {Allocation, Ajustement, Débit, Recrédit} | Oui | RB-25 |
| quantité | Décimal(2), signé | Oui | ≠ 0 |
| solde après | Décimal(2) | Oui | ≥ 0 |
| motif | Texte(500) | Oui pour Allocation et Ajustement | RB-25 |
| demande liée | Référence DemandeConge | Oui pour Débit et Recrédit | — |
| auteur | Référence Utilisateur | Oui | Pour Débit et Recrédit : l'utilisateur qui a déclenché l'opération |
| horodatage | Horodatage | Oui | — |

**DemandeConge**

| Attribut | Type | Obligatoire | Règle |
|---|---|---|---|
| identifiant | Identifiant | Oui | — |
| demandeur | Référence Utilisateur | Oui | — |
| type | Référence TypeConge | Oui | RB-08 |
| date de début, date de fin | Date | Oui | RB-05 |
| jours décomptés | Entier | Oui | ≥ 1, figé (RB-06) |
| commentaire du demandeur | Texte(500) | Non | Avertissement RGPD affiché (ENF-RGPD3) |
| statut | Énumération {EnAttente, Approuvée, Refusée, Annulée} | Oui | RB-13 |
| soumise le | Horodatage | Oui | — |
| décideur, décidée le | Référence Utilisateur, Horodatage | Si décidée | RB-15 |
| commentaire de décision | Texte(1000) | Oui si Refusée | RB-15 |
| alerte à la soumission, taux max à la soumission | Booléen, Décimal | Oui | RB-17 |
| acquittement : par, le, taux constaté | Référence Utilisateur, Horodatage, Décimal | Si approuvée en alerte | RB-18 |
| annulée par, annulée le, motif d'annulation | Référence Utilisateur, Horodatage, Texte(200) | Si Annulée | RB-14, RB-27 |

**HistoriqueDemande** (non modifiable)

| Attribut | Type | Obligatoire |
|---|---|---|
| demande | Référence DemandeConge | Oui |
| statut avant (vide à la création), statut après | Énumération | Oui |
| auteur | Référence Utilisateur (ou « Système ») | Oui |
| horodatage | Horodatage | Oui |
| commentaire | Texte(1000) | Non |

**JourFerie**

| Attribut | Type | Obligatoire | Règle |
|---|---|---|---|
| date | Date | Oui | Unique (RB-24) |
| libellé | Texte(100) | Oui | — |

**Notification** (journal des emails)

| Attribut | Type | Obligatoire |
|---|---|---|
| identifiant | Identifiant | Oui |
| événement | Énumération {Soumission, Approbation, Refus, AnnulationApprouvée} | Oui |
| demande | Référence DemandeConge | Oui |
| destinataire | Email | Oui |
| statut d'envoi | Énumération {EnFile, Envoyé, Échec} | Oui |
| tentatives | Entier | Oui |
| créée le, dernière tentative le, envoyée le | Horodatage | Oui / Non / Non |
| dernière erreur | Texte(500) | Non |

**JournalAdministration** (non modifiable)

| Attribut | Type | Obligatoire |
|---|---|---|
| auteur | Référence Utilisateur | Oui |
| action | Énumération (création, modification, désactivation…) | Oui |
| objet | Type d'entité + identifiant | Oui |
| détail | Texte : valeurs avant/après, hors secret d'authentification | Oui |
| horodatage | Horodatage | Oui |

**AnalyseChevauchement** (objet calculé, non persisté en tant que tel, RB-16)

| Attribut | Type |
|---|---|
| demande | Référence DemandeConge (ou période simulée) |
| effectif évalué N | Entier |
| en alerte | Booléen |
| taux maximal | Décimal |
| jours en dépassement | Liste de {date, A(J), taux, collègues absents (identifiant, nom)} |

### 6.2 Relations et cardinalités

| Relation | Cardinalité | Remarque |
|---|---|---|
| Utilisateur (manager) — Utilisateur (subordonné) | 0..1 — 0..n | Hiérarchie sans cycle (RB-03) |
| Utilisateur — DemandeConge (demandeur) | 1 — 0..n | — |
| Utilisateur — DemandeConge (décideur) | 0..1 — 0..n | — |
| TypeConge — DemandeConge | 1 — 0..n | — |
| Utilisateur — Solde | 1 — 0..n | Un solde par type soumis à décompte |
| TypeConge — Solde | 1 — 0..n | Uniquement si soumis à décompte |
| Solde — MouvementSolde | 1 — 0..n | — |
| DemandeConge — MouvementSolde | 0..1 — 0..2 | Au plus un Débit et un Recrédit par demande |
| DemandeConge — HistoriqueDemande | 1 — 1..n | — |
| DemandeConge — Notification | 1 — 0..n | — |
| Utilisateur — JournalAdministration (auteur) | 1 — 0..n | — |

### 6.3 Données initiales

- Types :
  - CP (soumis à décompte, sans rétroactivité) ;
  - RTT (soumis à décompte, sans rétroactivité) ;
  - Maladie (non soumis, rétroactivité autorisée) ;
  - Autre (non soumis, sans rétroactivité).
- Tous les types initiaux ont l'attribut système = vrai.
- Jours fériés légaux de France métropolitaine pour l'année en cours et l'année suivante (H-25).
- Un compte Admin RH initial, dont l'identifiant est fourni au déploiement et dont le mot de passe doit être changé à la première connexion.

---

## 7. Exigences non fonctionnelles (performance, sécurité, disponibilité, accessibilité, RGPD)

### 7.1 Performance et volumétrie

| ID | Exigence |
|---|---|
| ENF-P1 | Volumétrie de dimensionnement : 50 utilisateurs (croissance ×2 tolérée sans changement d'architecture), ~1 500 demandes par an, 10 utilisateurs simultanés. |
| ENF-P2 | Temps de réponse côté serveur au 95e percentile, données de volumétrie ENF-P1 sur 5 ans : < 300 ms pour les lectures unitaires ; < 500 ms pour la soumission, la décision et l'analyse de chevauchement ; < 1 s pour un calendrier de 93 jours. |
| ENF-P3 | Les listes sont paginées (taille par défaut 20, maximum 100). Le calendrier est limité à 93 jours par requête. |
| ENF-P4 | L'envoi d'email est asynchrone et n'entre pas dans le temps de réponse des opérations (RB-28). |
| ENF-P5 | Premier affichage de l'interface < 3 s sur un poste de bureau standard en réseau d'entreprise. |

### 7.2 Sécurité

| ID | Exigence |
|---|---|
| ENF-S1 | Tous les échanges passent en HTTPS (TLS 1.2 minimum). HSTS est activé. |
| ENF-S2 | Le jeton JWT est signé, porte l'identifiant et les rôles, et a une durée de vie courte. La valeur proposée est 60 minutes ; elle sera fixée à l'étape 2 (H-19). Le serveur vérifie à chaque requête que l'utilisateur est toujours actif. |
| ENF-S3 | Chaque endpoint est protégé par un contrôle de rôle et de périmètre (matrice §3.2), côté serveur. L'accès par défaut est refusé. |
| ENF-S4 | Les mots de passe sont stockés avec un algorithme de hachage adaptatif et salé, choisi à l'étape 2. Ils ne sont jamais journalisés ni renvoyés. Politique : 12 caractères minimum. |
| ENF-S5 | Après 5 échecs de connexion en 15 minutes, le compte est verrouillé 15 minutes. Le point d'entrée de connexion est limité en débit par adresse IP. |
| ENF-S6 | Les entrées sont validées côté serveur (types, longueurs, formats). Les sorties sont encodées. L'application est protégée contre les risques OWASP Top 10, en particulier injection, XSS, contrôle d'accès défaillant et IDOR. |
| ENF-S7 | Un accès à une demande hors périmètre de lecture renvoie 404, pour ne pas révéler son existence. Une action interdite sur une ressource visible renvoie 403. |
| ENF-S8 | Les secrets (clé de signature, identifiants SMTP, chaîne de connexion) sont fournis par la configuration d'environnement, jamais dans le code source. |
| ENF-S9 | Les journaux techniques ne contiennent ni mot de passe, ni jeton, ni commentaire libre de demande. |

### 7.3 Intégrité et cohérence

| ID | Exigence |
|---|---|
| ENF-I1 | La décision, le débit de solde et l'historique sont enregistrés de façon atomique. Les opérations concurrentes sur une même demande ou un même solde ne produisent ni double décision (CA-10.3), ni dépassement du disponible par des soumissions simultanées (CA-04.10), ni solde négatif, ni solde incohérent avec la somme des mouvements (RB-10, RB-11, RB-15, RB-25). |
| ENF-I2 | Toutes les règles RB-01 à RB-28 sont appliquées côté serveur. Les contrôles côté client sont uniquement ergonomiques. |
| ENF-I3 | Chaque règle métier est couverte par au moins un test automatisé xUnit. Les exemples EX-1 à EX-7 de RB-16 sont des cas de test obligatoires. |

### 7.4 Disponibilité et exploitation

| ID | Exigence |
|---|---|
| ENF-D1 | Disponibilité cible de 99,5 % sur les heures ouvrées (lun–ven, 8 h–19 h, Europe/Paris). Les interruptions planifiées ont lieu hors de ces heures. |
| ENF-D2 | Sauvegarde quotidienne de la base, conservée 30 jours. RPO ≤ 24 h, RTO ≤ 4 h. Une restauration est testée au moins une fois par an. |
| ENF-D3 | L'indisponibilité du serveur SMTP n'interrompt pas le service (RB-28). |
| ENF-D4 | Journalisation structurée des erreurs et un point de contrôle de santé (« health check ») pour la supervision. |
| ENF-D5 | Compatibilité navigateurs : les 2 dernières versions majeures de Chrome, Edge, Firefox et Safari. Interface responsive jusqu'à 360 px de large. |

### 7.5 Accessibilité

| ID | Exigence |
|---|---|
| ENF-A1 | Conformité RGAA 4.1 (WCAG 2.1 niveau AA) pour toutes les pages. |
| ENF-A2 | Navigation complète au clavier, focus visible, libellés associés à tous les champs, messages d'erreur annoncés aux lecteurs d'écran. |
| ENF-A3 | Le calendrier ne repose pas uniquement sur la couleur : texte ou motif en plus. Il propose une vue alternative en liste. |
| ENF-A4 | L'alerte de chevauchement est présentée sous forme de texte explicite (pas uniquement une icône ou une couleur). Contraste ≥ 4,5:1. |

### 7.6 RGPD

| ID | Exigence |
|---|---|
| ENF-RGPD1 | Le traitement est inscrit au registre des traitements de l'entreprise. Sa finalité est la gestion des absences du personnel ; sa base légale est l'exécution du contrat de travail et les obligations légales de l'employeur. |
| ENF-RGPD2 | Minimisation : seules les données des entités de la section 6 sont collectées. Aucun motif médical ni justificatif n'est demandé. |
| ENF-RGPD3 | Le type « Maladie » peut révéler une donnée de santé. Il n'est visible que par le demandeur, son approbateur et les admins RH (RB-23). Le champ commentaire affiche un avertissement : « N'indiquez aucune information médicale ». |
| ENF-RGPD4 | Durée de conservation : les données des demandes et des soldes sont conservées pendant la durée du contrat, puis 5 ans après la sortie. Ensuite, elles sont anonymisées ou purgées. La durée est à valider par le DPO / la direction (H-24). |
| ENF-RGPD5 | Droits des personnes : l'admin RH peut extraire toutes les données d'un utilisateur dans un format lisible (droit d'accès et de portabilité) et procéder à la rectification. |
| ENF-RGPD6 | Hébergement dans l'Union européenne. Les accès aux données personnelles sont journalisés (RB-26). |

---

## 8. Ébauche d'API (ressources et verbes, sans implémentation)

### 8.1 Conventions

- Base : `/api/v1`. Format JSON. Dates au format ISO 8601 (`YYYY-MM-DD`). Horodatages en ISO 8601 avec fuseau.
- Authentification : en-tête `Authorization: Bearer <jeton>` sur toutes les ressources sauf `POST /auth/login`.
- Les erreurs suivent le format Problem Details (RFC 9457) : `type`, `title`, `status`, `detail`, plus un champ `code` métier stable (ex. `INSUFFICIENT_BALANCE`) et, pour les erreurs de validation, un champ `errors` par champ.
- Codes HTTP :

| Code | Usage |
|---|---|
| 200 | Succès avec contenu |
| 201 | Création |
| 204 | Succès sans contenu |
| 400 | Requête mal formée ou validation de format |
| 401 | Non authentifié ou jeton invalide |
| 403 | Authentifié mais action interdite |
| 404 | Ressource inexistante ou hors périmètre de lecture (ENF-S7) |
| 409 | Conflit d'état : transition interdite, doublon, demande déjà décidée |
| 422 | Règle métier violée, avec un `code` métier |
| 429 | Trop de tentatives |

- Pagination : paramètres `page` et `pageSize`. La réponse contient `items` et `totalCount`.

### 8.2 Ressources

**Authentification et profil**

| Verbe | Ressource | Rôle | Description | US |
|---|---|---|---|---|
| POST | `/auth/login` | Public | Échange email + mot de passe contre un jeton | US-01 |
| GET | `/me` | Tous | Profil courant (identité, rôles, manager) | US-01 |
| PUT | `/me/password` | Tous | Changement de mot de passe (ancien + nouveau) | US-01 |
| GET | `/me/balances` | Tous | Soldes (solde, en attente, disponible) par type soumis à décompte | US-02 |
| GET | `/me/balances/{typeId}/movements` | Tous | Mouvements de son solde | US-02 |

**Demandes de congé**

| Verbe | Ressource | Rôle | Description | US |
|---|---|---|---|---|
| POST | `/leave-requests/simulate` | Tous | Calcule les jours décomptés et le disponible restant, sans rien créer | US-03 |
| POST | `/leave-requests` | Tous | Soumet une demande pour soi (type, début, fin, commentaire) | US-04 |
| GET | `/leave-requests?scope=mine` | Tous | Ses demandes, filtrables par statut, type et période | US-05 |
| GET | `/leave-requests?scope=to-decide` | Manager, Admin RH | Demandes en attente dont l'utilisateur est approbateur, avec indicateur d'alerte recalculé | US-08 |
| GET | `/leave-requests?scope=all` | Admin RH | Toutes les demandes, filtrables par statut, type, période, manager et employé | US-19 |
| GET | `/leave-requests/{id}` | Selon périmètre | Détail et historique. L'analyse de chevauchement est incluse seulement pour l'approbateur et l'admin RH. | US-05, US-09 |
| GET | `/leave-requests/{id}/overlap` | Approbateur, Admin RH | Analyse de chevauchement recalculée (RB-16, RB-17) | US-09 |
| POST | `/leave-requests/{id}/approve` | Approbateur | Corps : commentaire facultatif, `overlapAcknowledged` (booléen) | US-09, US-10 |
| POST | `/leave-requests/{id}/reject` | Approbateur | Corps : commentaire obligatoire | US-11 |
| POST | `/leave-requests/{id}/cancel` | Demandeur | Annulation (RB-14) | US-06 |

**Calendrier**

| Verbe | Ressource | Rôle | Description | US |
|---|---|---|---|---|
| GET | `/calendar?from=&to=&scope=team` | Tous | Absences approuvées de son équipe, sans type | US-07 |
| GET | `/calendar?from=&to=&scope=reports` | Manager | Absences approuvées de ses subordonnés directs, avec type | US-12 |
| GET | `/calendar?from=&to=&scope=all&managerId=` | Admin RH | Absences approuvées de toute l'entreprise, avec type | US-19 |

**Administration (Admin RH)**

| Verbe | Ressource | Description | US |
|---|---|---|---|
| GET, POST | `/leave-types` | Lister (GET accessible à tous, actifs seulement pour les non-RH) / créer | US-15 |
| PUT, DELETE | `/leave-types/{id}` | Modifier (libellé, actif, décompte, rétroactivité) / supprimer si jamais utilisé | US-15 |
| GET, POST | `/users` | Lister (filtres actif, manager, rôle) / créer | US-17 |
| GET, PUT | `/users/{id}` | Consulter / modifier (identité, rôles, manager) | US-17 |
| POST | `/users/{id}/deactivate` | Désactiver (RB-27) | US-17 |
| POST | `/users/{id}/reset-password` | Réinitialiser le mot de passe (changement imposé à la connexion suivante) | US-17 |
| GET | `/users/{id}/export` | Export des données personnelles (ENF-RGPD5) | US-17 |
| GET | `/users/{id}/balances` | Soldes d'un utilisateur | US-16 |
| GET | `/users/{id}/balances/{typeId}/movements` | Relevé des mouvements | US-16 |
| POST | `/users/{id}/balances/{typeId}/movements` | Allocation ou ajustement (nature, quantité, motif) | US-16 |
| GET, POST | `/holidays?year=` | Lister / créer un jour férié | US-18 |
| DELETE | `/holidays/{date}` | Supprimer un jour férié | US-18 |
| GET | `/notifications?status=` | Journal des emails (dont les échecs) | US-13, US-14 |
| GET | `/audit-log` | Journal d'administration | US-19 |

### 8.3 Codes d'erreur métier (champ `code`)

| Code | HTTP | Règle |
|---|---|---|
| `NO_WORKING_DAY` | 422 | RB-06 |
| `PAST_DATE_NOT_ALLOWED` | 422 | RB-05 |
| `PERIOD_TOO_LONG` | 422 | RB-05 |
| `LEAVE_TYPE_INACTIVE` | 422 | RB-08 |
| `LEAVE_TYPE_NOT_TRACKED` | 422 | RB-09 |
| `INSUFFICIENT_BALANCE` | 422 | RB-10, RB-12 |
| `BALANCE_BELOW_PENDING` | 422 | RB-11 |
| `OVERLAPS_OWN_REQUEST` | 422 | RB-05 |
| `ACCOUNT_LOCKED` | 429 | RB-02, ENF-S5 |
| `ALREADY_STARTED` | 422 | RB-14 |
| `OVERLAP_NOT_ACKNOWLEDGED` | 422 | RB-18 |
| `REJECTION_COMMENT_REQUIRED` | 400 | RB-15 |
| `INVALID_STATUS_TRANSITION` | 409 | RB-13 |
| `HIERARCHY_CYCLE` | 422 | RB-03 |
| `HAS_ACTIVE_REPORTS` | 422 | RB-27 |
| `SELF_APPROVAL_FORBIDDEN` | 403 | RB-04 |

---

## 9. Risques et points ouverts (liste explicite des ambiguïtés du brief et des hypothèses retenues)

### 9.1 Ambiguïtés du brief et hypothèses retenues

Chaque hypothèse est appliquée par la spécification. Elle reste à confirmer par le commanditaire. Les hypothèses marquées « à valider en priorité » ont l'impact le plus fort.

| ID | Ambiguïté du brief | Hypothèse retenue | Alternatives écartées / impact si invalidée |
|---|---|---|---|
| H-01 | Les rôles sont-ils exclusifs ? | Rôles cumulatifs ; tout utilisateur est Employé (RB-01). | Rôles exclusifs : un manager ne pourrait pas poser de congé, ce qui est irréaliste. |
| H-02 | Qu'est-ce qu'une « équipe » ? | L'ensemble des subordonnés directs d'un manager, sans le manager. Pas d'entité « Équipe » distincte (RB-03). | Entité Équipe indépendante de la hiérarchie, avec appartenance multiple : plus souple mais complexifie validation et chevauchement. À revoir si des équipes transverses existent. |
| H-03 | Qui valide la demande d'un employé sans manager (dirigeant, RH) ? | Les admins RH actifs, sauf le demandeur (RB-04). Cas limite : un admin RH sans manager et seul admin RH ne peut faire décider aucune de ses demandes, qui restent « En attente ». Il faut donc que tout admin RH soit rattaché à un manager, ou qu'il existe au moins deux admins RH (contrôle de reprise, R-06). | Validation automatique : écartée faute de contrôle. |
| H-04 | Granularité des congés. | Journées entières uniquement. | Demi-journées (courantes pour les RTT) : modifient RB-06, RB-16 et le modèle. **À valider en priorité.** |
| H-05 | Comment compter les jours (ouvrés, ouvrables, calendaires) ? | Jours ouvrés lun–ven hors jours fériés gérés par l'admin RH, figés à la soumission (RB-06, RB-07). | Jours ouvrables (lun–sam), qui est le décompte légal par défaut des CP en France : ajouter un paramètre par type. Recalcul à la décision : écarté (le solde affiché à l'employé changerait). |
| H-06 | « Un solde par type » s'applique-t-il à Maladie et Autre ? | Non : Maladie et Autre ne sont pas soumis à décompte par défaut. Le comportement est paramétrable par type (RB-09). | Solde Maladie : n'a pas de sens juridique (l'arrêt maladie n'est pas un droit à consommer). |
| H-07 | Le solde est-il contrôlé à la soumission alors qu'il n'est décrémenté qu'à l'approbation ? | Oui, contre le disponible, qui réserve les jours en attente (RB-10). Le débit reste à l'approbation (brief, RB-12). | Aucun contrôle avant approbation : risque de sur-réservation. |
| H-08 | Comment les soldes sont-ils alimentés (acquisition, période de référence, report) ? | Uniquement par allocation et ajustement manuels de l'admin RH. Pas d'acquisition automatique ni de remise à zéro. | Acquisition mensuelle et période de référence juin–mai pour les CP : lot ultérieur. |
| H-09 | Précision des soldes. | Décimal à 2 décimales (allocations fractionnaires possibles). Les demandes décomptent des entiers. | — |
| H-10 | Peut-on poser un congé dans le passé ? | Seulement pour les types « rétroactivité autorisée » (Maladie par défaut). | Interdiction totale : bloquerait la régularisation des absences maladie. |
| H-11 | Modification d'une demande ; saisie pour un tiers. | Non : annuler puis resoumettre. Pas de saisie pour le compte d'autrui. | Saisie par RH pour un tiers (maladie déclarée par téléphone) : besoin probable, lot ultérieur. |
| H-12 | L'annulation n'est pas mentionnée dans le brief. | Annulation par le demandeur : demande en attente, ou approuvée non commencée, avec recrédit (RB-14). | Pas d'annulation : une absence abandonnée resterait au calendrier et débitée. |
| H-13 | Le brief ne prévoit que 2 emails. | Ajout d'un email au manager lors de l'annulation d'une demande approuvée (RB-22). | Sans cet email, le manager n'est pas informé qu'un collaborateur sera finalement présent. Retirer si non souhaité. |
| H-14 | **Sens de « chevauche plus de 50 % d'absences déjà approuvées sur l'équipe ».** **À valider en priorité.** | Formule de RB-16 : analyse jour par jour, sur les jours ouvrés ; taux = collègues absents (demandes approuvées) / collègues actifs de l'équipe, demandeur exclu des deux termes ; alerte si > 50 % strictement un jour au moins. Justification : un chevauchement est « critique » quand la capacité de l'équipe est réduite le même jour. Exclure le demandeur mesure la présence restante des collègues qui assurent la continuité. | (a) **Taux projeté** (demandeur inclus) : (A(J)+1)/(N+1) > 50 %. Il anticipe l'effectif restant, mais le brief parle d'absences « déjà approuvées ». (b) **Évaluation sur la période** : part des collègues ayant au moins un jour d'absence pendant la demande. Écartée : deux absences de jours différents seraient comptées comme simultanées (cf. EX-7). (c) **Part des jours de la demande** couverts par au moins une absence d'équipe. Écartée : une seule absence sur une équipe de 10 déclencherait l'alerte. La formule est isolée dans une seule règle, donc un changement d'interprétation n'impacte que RB-16 et ses tests. |
| H-15 | « Signalée au manager avant décision » : simple information ou blocage ? | Information visible (file, détail, email) plus acquittement explicite obligatoire pour approuver (RB-18). La décision reste au manager. | Simple affichage : impossible de prouver que le manager a vu l'alerte. Blocage de l'approbation : non demandé. |
| H-16 | À quel moment l'alerte est-elle évaluée ? | À la soumission (pour l'email), puis recalcul à l'affichage et à l'approbation ; le recalcul fait foi (RB-17). | Calcul unique à la soumission : les absences approuvées entre-temps seraient ignorées. |
| H-17 | Qui voit l'alerte ? | Approbateur et admin RH uniquement (RB-19). | Visible par l'employé dès la simulation : utile mais expose indirectement les absences des collègues. Lot ultérieur possible. |
| H-18 | Origine des comptes et des identifiants (« Auth : JWT »). | Comptes locaux créés par l'admin RH, email + mot de passe, jeton JWT émis par l'application. | SSO d'entreprise (Entra ID / Google) : préférable si existant. Le JWT serait alors émis par le fournisseur d'identité. À confirmer à l'étape 2. |
| H-19 | Durée de session. | Jeton court (60 min proposé), sans jeton de rafraîchissement en v1. Réauthentification à expiration. | Jeton de rafraîchissement : décision d'architecture, étape 2. |
| H-20 | Le calendrier d'équipe expose-t-il le type d'absence ? | Employé : « Absent » sans type. Manager (pour ses subordonnés) et admin RH : avec type (RB-23). | Type visible par tous : expose une donnée de santé (Maladie). |
| H-21 | Le calendrier montre-t-il les demandes en attente ? | Non, uniquement les approuvées (brief). Le manager voit les demandes en attente dans sa file. | Affichage hachuré des demandes en attente pour le manager : amélioration possible. |
| H-22 | Changement de manager pendant qu'une demande est en attente. | L'approbateur est le manager actuel au moment de la décision (RB-04). | Approbateur figé à la soumission : bloquerait les demandes en cas de départ du manager. |
| H-23 | Effet de la désactivation d'un employé sur ses demandes. | Demandes en attente annulées ; demandes approuvées conservées (RB-27). Soldes non modifiés (solde de tout compte hors périmètre). | — |
| H-24 | Durée de conservation des données. | Durée du contrat + 5 ans, puis anonymisation (ENF-RGPD4). | À valider par le DPO / la direction. |
| H-25 | Contexte géographique et linguistique. | Entité unique, France métropolitaine, interface en français, fuseau Europe/Paris. | Alsace-Moselle (2 jours fériés supplémentaires) : gérable par la liste des jours fériés si un seul site. |
| H-26 | Absence du manager (qui décide pendant ses congés ?). | Pas de délégation en v1 : les demandes attendent le retour du manager ou un changement de rattachement par l'admin RH. | Délégation temporaire : lot ultérieur. Risque R-03. |
| H-27 | Le seuil de 50 % est-il paramétrable ? | Non, fixé à 50 % comme dans le brief. | Paramètre RH : trivial à ajouter si demandé. |
| H-28 | Contenu des emails. | Défini par RB-21 et RB-22, en texte simple et en français, avec un lien vers l'application. | — |
| H-29 | Longueur et forme du commentaire de refus. | Non vide après suppression des espaces, ≤ 1 000 caractères (RB-15). | Longueur minimale (ex. 10 caractères) : écartée, car non demandée. |
| H-30 | Périmètre de lecture de l'admin RH. | Lecture de toutes les demandes et de tous les calendriers avec type. | — |
| H-31 | L'admin RH peut-il décider à la place d'un manager ? | Non, sauf pour les employés sans manager (RB-04). | Pouvoir de substitution RH : pallie H-26 mais court-circuite la ligne managériale. |
| H-32 | Comportement si l'email échoue. | Non bloquant, nouvel essai, journal consultable (RB-28). | Opération bloquée : écartée, car un incident SMTP paralyserait l'outil. |
| H-33 | Un employé peut-il avoir deux demandes qui se chevauchent ? | Non : refus si la période recoupe une de ses demandes « En attente » ou « Approuvée » (RB-05, `OVERLAPS_OWN_REQUEST`). | Autoriser : risque de double décompte. |
| H-34 | Durée maximale d'une demande. | 366 jours calendaires au plus (RB-05), pour borner les calculs et les erreurs de saisie. | Aucune limite : saisies erronées (ex. année fausse) non détectées. |

### 9.2 Risques

| ID | Risque | Probabilité | Impact | Mitigation |
|---|---|---|---|---|
| R-01 | L'interprétation de la règle des 50 % (H-14) ne correspond pas à l'attente métier. | Moyenne | Moyen | Validation par le commanditaire avant l'étape 3, sur les exemples EX-1 à EX-7. Formule isolée dans RB-16 et couverte par des tests. |
| R-02 | Écart avec la législation sur le décompte des CP (jours ouvrables vs ouvrés, demi-journées, acquisition). | Moyenne | Moyen | H-04, H-05 et H-08 à valider par la RH. Le paramétrage par type limite le coût d'évolution. |
| R-03 | Demandes bloquées en l'absence du manager (pas de délégation). | Moyenne | Faible | Changement de rattachement par l'admin RH (RB-04 dynamique). Délégation en lot ultérieur. |
| R-04 | Exposition de données de santé (type Maladie, commentaires libres). | Faible | Élevé | RB-23, ENF-RGPD3, avertissement sur le champ commentaire, journalisation des accès. |
| R-05 | Emails non délivrés (SMTP, filtres anti-spam), ce qui retarde les décisions. | Moyenne | Moyen | RB-28, journal des notifications ; la file du manager reste la source de vérité. |
| R-06 | Données de référence incomplètes au démarrage (rattachements, soldes initiaux). | Élevée | Élevé | Reprise initiale des soldes et de la hiérarchie par l'admin RH avant ouverture (procédure à définir). Contrôle des employés sans manager. |
| R-07 | Concurrence sur les soldes et les décisions (soumissions ou approbations simultanées). | Faible | Élevé | ENF-I1, tests CA-04.10 et CA-10.3. |
| R-08 | Gestion d'identité locale moins sûre qu'un SSO existant. | Moyenne | Moyen | H-18 à arbitrer à l'étape 2 ; exigences ENF-S2, ENF-S4, ENF-S5. |

### 9.3 Points ouverts à trancher par le commanditaire

1. Confirmer l'interprétation de la règle des 50 % (H-14), le caractère strict du seuil et l'acquittement obligatoire (H-15).
2. Confirmer le décompte : jours ouvrés ou ouvrables, et le besoin de demi-journées (H-04, H-05).
3. Confirmer que Maladie et Autre n'ont pas de solde (H-06).
4. Décider du mode d'alimentation des soldes et de leur reprise initiale (H-08, R-06).
5. Valider l'email d'annulation (H-13) et la politique d'annulation (H-12).
6. Indiquer s'il existe un fournisseur d'identité d'entreprise (H-18).
7. Valider la durée de conservation avec le DPO (H-24).
8. Indiquer s'il existe des équipes transverses qui ne suivent pas la hiérarchie (H-02).
9. Confirmer qu'aucune délégation ni substitution RH n'est requise en v1 (H-26, H-31).

### 9.4 Renvoi à l'étape 2

Ces décisions ne relèvent pas de la présente spécification :

- le choix de la base (SQL Server ou PostgreSQL) ;
- l'algorithme de signature du JWT et la stratégie de rafraîchissement ;
- l'algorithme de hachage des mots de passe ;
- le mécanisme d'envoi asynchrone des emails ;
- la stratégie de gestion de la concurrence ;
- le découpage en couches.
