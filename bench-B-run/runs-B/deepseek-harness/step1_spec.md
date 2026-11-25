# TeamLeave — Spécification technico-fonctionnelle

**Version** : 1.0  
**Source** : `00_brief.md`  
**Étape** : 1 — Spécification  
**Statut** : proposition à valider par le commanditaire

> Ce document décrit le comportement attendu de TeamLeave. Il ne contient ni choix d'architecture, ni modèle physique de base de données, ni code : ces éléments relèvent des étapes 2 à 4.

## 1. Contexte et objectifs

### 1.1 Contexte

TeamLeave est une application interne destinée à une PME d'environ 50 personnes. Elle centralise le cycle de vie des congés : déclaration d'une absence par un employé, contrôle du solde, décision du manager, mise à jour du calendrier d'équipe et notification des intéressés.

Le brief ne décrit pas l'outil utilisé aujourd'hui. La présente spécification retient qu'il n'existe pas de source unique fiable pour les demandes, les décisions et les soldes, et que la v1 doit remplacer les échanges informels par un flux tracé. Cette hypothèse est reprise en section 9.

Le produit couvre quatre types de congés par défaut — CP, RTT, maladie et autre — avec un solde annuel par employé et par type. Une demande est soumise avec un statut « en attente », puis approuvée ou refusée par le manager. Le solde n'est décrémenté qu'à l'approbation. Un calendrier d'équipe expose les absences approuvées et un signal est présenté au manager lorsque l'absence demandée ferait dépasser 50 % d'absents dans l'équipe sur une demi-journée.

### 1.2 Objectifs

- **O1 — Source unique** : toute demande, décision, annulation et correction de solde est enregistrée dans TeamLeave avec son auteur et son horodatage.
- **O2 — Soldes fiables** : le solde disponible est calculé par employé, type et année ; il ne peut jamais devenir négatif ; il n'est décrémenté qu'à l'approbation et restauré en cas d'annulation valide.
- **O3 — Décision éclairée** : le manager dispose, avant de décider, des dates, du nombre de jours, du solde de l'employé et du signal de chevauchement à 50 %.
- **O4 — Visibilité d'équipe** : chaque membre voit les absences approuvées de son équipe sur un calendrier, sans accéder aux données confidentielles des autres.
- **O5 — Notifications** : la soumission notifie le manager et la décision notifie l'employé, sans que l'échec d'un email ne remette en cause la décision.
- **O6 — Administration maîtrisée** : l'Admin RH paramètre les types, attribue et ajuste les soldes, gère les comptes, les équipes et les lignes de management.

### 1.3 Indicateurs de succès de la v1

- Aucun solde négatif n'est observable, y compris en cas d'approbations concurrentes.
- 100 % des demandes approuvées ou refusées possèdent un décideur, une date de décision et, en cas de refus, un commentaire non vide.
- 100 % des demandes soumises produisent une notification au manager ou une trace d'échec consultable.
- Le calendrier d'équipe ne contient aucune demande en attente, refusée ou annulée.
- Le signal de chevauchement est calculé à la soumission et recalculé juste avant la décision.

### 1.4 Définitions

| Terme | Définition retenue |
|---|---|
| **Jour ouvré** | Du lundi au vendredi. Les jours fériés ne sont pas gérés en v1 et sont comptés comme des jours ouvrés (hypothèse H-04). |
| **Demi-journée** | Matin (`AM`) ou après-midi (`PM`) d'un jour ouvré. Une journée complète vaut 1,0 jour ; une demi-journée vaut 0,5 jour. |
| **Année de congé** | Année civile, du 1er janvier au 31 décembre, en fuseau `Europe/Paris` (H-03). |
| **Équipe** | Ensemble des utilisateurs actifs partageant le même identifiant d'équipe. L'équipe sert au calendrier et au calcul du chevauchement. |
| **Managé direct** | Utilisateur dont le champ « manager direct » référence l'utilisateur considéré. Le manager traite les demandes de ses managés directs. |
| **Demande active** | Demande au statut `PENDING` ou `APPROVED`. Une demande `REJECTED` ou `CANCELLED` n'est pas active. |
| **Solde disponible** | Jours octroyés, y compris ajustements, moins les jours consommés par des demandes approuvées et non annulées, pour un employé, un type et une année. |
| **Chevauchement critique** | Situation où le taux d'absence projeté de l'équipe, demande incluse, dépasse strictement 50 % sur au moins une demi-journée demandée (RB-12). |

## 2. Périmètre (inclus / exclu)

### 2.1 Inclus dans la v1

1. Authentification des utilisateurs, gestion de leur propre mot de passe et contrôle d'accès par rôle.
2. Gestion par l'Admin RH des comptes, des équipes, des lignes de management et des rôles.
3. Paramétrage des types de congés, avec les types par défaut CP, RTT, maladie et autre.
4. Attribution annuelle des soldes par l'Admin RH et ajustements motivés, avec journal d'audit.
5. Soumission d'une demande de congé par un employé pour lui-même : type, dates, demi-journées de début et de fin, commentaire facultatif.
6. Contrôles de validité, de solde et de chevauchement personnel à la soumission.
7. Décision du manager : approbation ou refus avec commentaire obligatoire en cas de refus.
8. Calcul du signal de chevauchement à 50 %, affichage au manager et acquittement explicite avant approbation lorsqu'il est actif.
9. Décrément du solde à l'approbation, restauration en cas d'annulation valide.
10. Calendrier d'équipe des absences approuvées, avec demi-journées.
11. Annulation d'une demande en attente et d'un congé approuvé à venir par son titulaire.
12. Notifications email de soumission au manager et de décision à l'employé.
13. Journal d'audit des décisions, annulations, ajustements de solde et changements administratifs.
14. Consultation par l'employé de son historique et de ses soldes ; consultation par le manager de son équipe ; consultation globale par l'Admin RH.

### 2.2 Exclu de la v1

- Paie, calcul de rémunération, indemnités et interface avec un logiciel de paie.
- Acquisition automatique des droits (ancienneté, prorata temporis), report automatique d'une année sur l'autre et régularisation de fin d'année.
- Gestion des jours fériés, des fermetures collectives et des calendriers légaux par pays ou région.
- Travail à temps partiel, horaires variables, absences en heures et gestion des quotités.
- Approbation à plusieurs niveaux, délégation temporaire, suppléance et circuits d'escalade automatiques.
- Dépôt de justificatifs, arrêt de travail, certificat médical et pièce jointe.
- Saisie rétroactive, y compris pour la maladie ; correction d'une absence déjà commencée.
- Modification directe d'une demande après soumission, brouillon, réouverture d'une demande refusée ou annulée.
- Actions groupées, import/export de masse, import d'un annuaire.
- Reporting analytique avancé, tableaux de bord RH, export comptable ou paie.
- Authentification unique SSO/LDAP, auto-inscription, mot de passe oublié par email.
- Application mobile native, synchronisation avec des agendas externes (Outlook, Google).
- Multi-langue, multi-société et multi-devise.
- Notifications autres que les deux événements obligatoires : relances, rappels, annulations, résumés hebdomadaires.
- API publique destinée à des tiers, webhooks et intégrations externes.
- Suppression définitive d'un utilisateur, d'une équipe, d'un type ou d'une demande via l'application ; seules la désactivation et la conservation de l'historique sont prévues.
- Approbation par l'Admin RH en tant que rôle RH ; l'Admin RH n'approuve que s'il est aussi manager direct du demandeur.
- Annulation d'un congé déjà commencé, y compris par l'Admin RH ; une correction de solde reste possible via un ajustement motivé.

Toute fonctionnalité non mentionnée dans la liste « inclus » est hors périmètre de la v1.

## 3. Acteurs et rôles (matrice des permissions)

### 3.1 Acteurs

| Acteur | Description | Droits de base |
|---|---|---|
| **Employé** | Tout utilisateur actif. Le rôle Employé est porté par tous les comptes. | Accès à ses propres demandes, soldes, profil et calendrier de son équipe. |
| **Manager** | Utilisateur ayant au moins un managé direct actif. Le rôle peut être cumulé avec Employé. | Consultation et décision sur les demandes de ses managés directs, hors sa propre demande. |
| **Admin RH** | Utilisateur chargé du paramétrage et de l'administration. | Gestion des types, soldes, comptes, équipes et audit ; consultation de toutes les données. |
| **Système** | Composant applicatif, non connecté. | Calculs, contrôle des règles, génération des notifications et écriture des traces. |

Un utilisateur peut cumuler Manager et Admin RH. Ses droits sont alors l'addition de ses rôles, sans dépasser les interdictions ci-dessous. Un rôle Manager sans managé direct ne donne aucun droit de décision.

### 3.2 Portées d'accès

- **Soi** : l'utilisateur lui-même, ses demandes, son solde et son profil.
- **Managés directs** : les utilisateurs actifs dont le manager direct est l'utilisateur considéré.
- **Équipe** : les utilisateurs actifs partageant la même équipe que l'utilisateur, pour le calendrier et le calcul de chevauchement.
- **Tous** : l'ensemble des données de l'entreprise, réservé à l'Admin RH.

### 3.3 Matrice des permissions

| Action | Employé | Manager | Admin RH | Précisions |
|---|---|---|---|---|
| Se connecter, changer son mot de passe | Oui (soi) | Oui (soi) | Oui (soi) | Aucune auto-inscription. |
| Soumettre une demande pour soi | Oui (soi) | Oui (soi) | Oui (soi) | Tout compte est aussi employé. |
| Consulter ses demandes et ses soldes | Oui (soi) | Oui (soi) | Oui (soi) | Aucun accès aux données d'un autre employé hors périmètre. |
| Annuler sa demande en attente ou son congé futur approuvé | Oui (soi) | Oui (soi) | Oui (soi) | Selon RB-14. |
| Consulter les demandes de ses managés directs | Non | Oui (managés) | Oui (tous) | L'Admin RH consulte tout, sans droit de décision. |
| Approuver ou refuser une demande | Non | Oui (managés, jamais soi) | Non* | *Sauf si l'Admin RH est aussi manager direct du demandeur. |
| Consulter les soldes de ses managés directs | Non | Oui (managés, lecture) | Oui (tous) | Lecture seule pour le manager. |
| Consulter le calendrier de son équipe | Oui (équipe) | Oui (équipe) | Oui (toutes) | Absences approuvées uniquement. |
| Consulter le calendrier d'une autre équipe | Non | Oui si manager d'au moins un membre | Oui | Sinon refus. |
| Configurer les types de congés | Non | Non | Oui | Création, libellé, couleur, activation/désactivation. |
| Créer et ajuster les soldes | Non | Non | Oui | Ajustements motivés et audités. |
| Gérer les comptes, équipes et lignes de management | Non | Non | Oui | Création, modification, désactivation. |
| Consulter le journal d'audit | Non | Non | Oui | Lecture seule. |
| Voir le type et les commentaires d'une demande | Soi | Managés | Tous | Le calendrier masque le type aux pairs (RB-17). |
| Voir le signal de chevauchement à 50 % | Non | Managés | Tous | Le signal est destiné au décideur et à l'Admin RH. |
| Modifier ou supprimer une demande | Non | Non | Non | Immutabilité ; annulation puis nouvelle demande (RB-15). |

**Règles transverses de la matrice :**

- Un manager ne peut jamais approuver ni refuser sa propre demande, même si une configuration erronée le désignait comme son propre manager. Sa demande est traitée par son propre manager.
- Un Admin RH ne peut pas décider d'une demande au seul titre de son rôle RH.
- Toute vérification de permission est faite côté serveur. Le masquage dans l'interface n'est pas une mesure de sécurité.
- Une ressource hors périmètre est renvoyée comme inexistante (`404`) ; un rôle authentifié mais insuffisant pour l'action est renvoyé en `403`.

## 4. User stories numérotées, chacune avec critères d'acceptation testables (format Given/When/Then)

### US-01 — Se connecter et accéder à son espace selon son rôle

**En tant qu'** utilisateur actif, **je veux** me connecter avec mon compte professionnel, **afin d'** accéder à mes demandes, à mes soldes et aux fonctions correspondant à mon rôle.

**Règles appliquées :** RB-01, RB-17.

**Critères d'acceptation :**

- **CA-01.1** — **Given** un employé actif avec des identifiants valides, **When** il se connecte, **Then** la réponse est `200` avec un jeton d'accès, un jeton de rafraîchissement et ses rôles, sans aucun hash de mot de passe.
- **CA-01.2** — **Given** un mot de passe erroné ou un compte désactivé, **When** une tentative de connexion est faite, **Then** la réponse est `401` avec un message générique, sans révéler si l'email existe.
- **CA-01.3** — **Given** un employé sans rôle Manager, **When** il appelle un endpoint de décision manager, **Then** la réponse est `403`.
- **CA-01.4** — **Given** un jeton d'accès expiré et un jeton de rafraîchissement valide, **When** le rafraîchissement est demandé, **Then** un nouveau jeton d'accès est délivré.
- **CA-01.5** — **Given** un compte désactivé et un jeton de rafraîchissement encore valide, **When** le rafraîchissement est demandé, **Then** la réponse est `401`.

### US-02 — Consulter mes soldes par type et par année

**En tant qu'** employé, **je veux** consulter mes soldes disponibles, **afin de** savoir combien de jours je peux encore demander avant de soumettre une demande.

**Règles appliquées :** RB-04, RB-05, RB-17.

**Critères d'acceptation :**

- **CA-02.1** — **Given** un solde CP 2026 de 20 jours octroyés et 3,5 jours consommés, **When** l'employé consulte ses soldes 2026, **Then** la réponse contient 20,0 octroyés, 3,5 consommés et 16,5 disponibles.
- **CA-02.2** — **Given** aucun solde n'a été attribué pour un type et une année, **When** l'employé consulte ce type et cette année, **Then** le disponible retourné est 0,0 et non une erreur.
- **CA-02.3** — **Given** une demande de 2 jours au statut `PENDING`, **When** l'employé consulte son solde avant décision, **Then** les jours consommés et le disponible sont inchangés.
- **CA-02.4** — **Given** un employé A authentifié, **When** il demande le solde de l'employé B, **Then** la réponse est `404`.

### US-03 — Soumettre une demande de congé valide

**En tant qu'** employé, **je veux** soumettre une demande avec un type et des dates, **afin que** mon manager puisse la traiter et que mon solde soit débité après approbation.

**Règles appliquées :** RB-02, RB-03, RB-06, RB-07, RB-08, RB-12, RB-16.

**Critères d'acceptation :**

- **CA-03.1** — **Given** un employé avec 10 jours de CP disponibles, un manager actif, aucun chevauchement personnel et une période du lundi au mercredi entièrement ouvrée, **When** il soumet une demande CP complète, **Then** la réponse est `201`, le statut est `PENDING`, le nombre de jours est 3,0, le solde est inchangé et une notification de soumission est créée pour le manager.
- **CA-03.2** — **Given** une demande du vendredi après-midi au lundi matin, **When** elle est soumise, **Then** le nombre de jours calculé est 1,0 (le samedi et le dimanche sont exclus) et les deux demi-journées sont enregistrées.
- **CA-03.3** — **Given** une date de fin antérieure à la date de début, une date de début ou de fin un samedi/dimanche, ou une date de début passée, **When** la demande est soumise, **Then** la réponse est `422` et aucune demande n'est créée.
- **CA-03.4** — **Given** un solde disponible insuffisant sur au moins une des années concernées, **When** la demande est soumise, **Then** la réponse est `422` avec le détail du solde manquant et aucune demande n'est créée.
- **CA-03.5** — **Given** une demande existante au statut `PENDING` couvrant le mardi matin, **When** l'employé soumet une nouvelle demande couvrant le mardi, **Then** la réponse est `409` avec le code `PERSONAL_OVERLAP`.
- **CA-03.6** — **Given** un employé sans manager direct actif, **When** il soumet une demande, **Then** la réponse est `409` avec le code `NO_APPROVER` et aucune demande n'est créée.

### US-04 — Être alerté lorsqu'une demande ferait dépasser 50 % d'absents dans l'équipe

**En tant que** manager, **je veux** voir un signal avant de décider lorsqu'une demande ferait passer l'équipe au-delà de 50 % d'absents sur une demi-journée, **afin d'** éviter un chevauchement critique.

**Règles appliquées :** RB-12, RB-13, RB-16.

**Critères d'acceptation :**

- **CA-04.1** — **Given** une équipe de 6 membres actifs, dont 3 autres membres ont un congé approuvé toute la journée, **When** un employé demande la même journée complète, **Then** le taux projeté est `(3 + 1) / 6 = 66,7 %`, le signal est actif, le détail indique 4 absents sur 6 et le manager voit le signal avant de décider.
- **CA-04.2** — **Given** la même équipe de 6, mais seulement 2 autres membres absents toute la journée, **When** la demande porte sur la même journée complète, **Then** le taux projeté est `(2 + 1) / 6 = 50,0 %` et le signal est inactif, car le seuil est strictement supérieur à 50 %.
- **CA-04.3** — **Given** 2 autres membres absents toute la journée et 2 autres membres absents le matin seulement, **When** la demande porte sur la journée complète, **Then** le matin est signalé à `(4 + 1) / 6 = 83,3 %`, l'après-midi ne l'est pas à `(2 + 1) / 6 = 50,0 %`, et le signal global de la demande est actif.
- **CA-04.4** — **Given** 5 autres membres avec une demande au statut `PENDING` couvrant la journée, **When** un employé demande cette journée, **Then** ces demandes en attente ne sont pas comptées et le signal reste inactif si aucune demande approuvée ne couvre la période.
- **CA-04.5** — **Given** une équipe de 2 membres actifs dont l'autre membre est approuvé absent, **When** l'employé demande la même journée, **Then** le taux projeté est 100 % et le signal est actif.
- **CA-04.6** — **Given** une équipe d'un seul membre actif, **When** cet employé soumet une demande, **Then** aucun signal de chevauchement n'est produit.

### US-05 — Consulter l'historique de mes demandes

**En tant qu'** employé, **je veux** consulter mes demandes et leur statut, **afin de** suivre leur traitement et retrouver les décisions passées.

**Règles appliquées :** RB-07, RB-15, RB-17.

**Critères d'acceptation :**

- **CA-05.1** — **Given** un employé ayant des demandes aux statuts `PENDING`, `APPROVED` et `REJECTED`, **When** il liste ses demandes, **Then** il ne voit que les siennes, triées de la plus récente à la plus ancienne.
- **CA-05.2** — **Given** une demande de l'employé, **When** il en consulte le détail, **Then** il voit le type, les dates, les demi-journées, le nombre de jours, le statut, le commentaire de décision et la chronologie des événements.
- **CA-05.3** — **Given** l'identifiant d'une demande d'un autre employé, **When** l'employé tente d'y accéder, **Then** la réponse est `404`.
- **CA-05.4** — **Given** une demande refusée, **When** l'employé soumet une nouvelle demande sur les mêmes dates, **Then** la nouvelle demande est acceptée si les autres règles sont respectées, car une demande refusée n'est pas active.

### US-06 — Consulter les demandes en attente de mon équipe

**En tant que** manager, **je veux** voir les demandes en attente de mes managés directs, **afin de** les traiter sans attendre un email.

**Règles appliquées :** RB-02, RB-09, RB-12, RB-17.

**Critères d'acceptation :**

- **CA-06.1** — **Given** un manager M avec deux managés directs A et B, et des demandes en attente pour A, B et un employé d'une autre équipe, **When** M consulte sa file, **Then** il voit uniquement les demandes de A et B.
- **CA-06.2** — **Given** une demande en attente de M lui-même, **When** M consulte sa file, **Then** sa propre demande n'y figure pas.
- **CA-06.3** — **Given** une demande d'un managé direct, **When** M en consulte le détail, **Then** il voit le solde disponible actuel, le type, les dates, le nombre de jours et le signal de chevauchement recalculé à l'instant de la consultation.
- **CA-06.4** — **Given** une demande d'un employé qui n'est pas un managé direct de M, **When** M tente d'y accéder, **Then** la réponse est `404`.

### US-07 — Approuver une demande

**En tant que** manager, **je veux** approuver une demande de mon managé direct, **afin de** valider son absence et de décrémenter son solde.

**Règles appliquées :** RB-05, RB-09, RB-10, RB-12, RB-13, RB-16, RB-22.

**Critères d'acceptation :**

- **CA-07.1** — **Given** une demande de 2 jours en attente, un solde suffisant et aucun signal de chevauchement, **When** le manager approuve, **Then** le statut devient `APPROVED`, le solde consommé augmente de 2,0, une notification de décision est créée pour l'employé et le décideur ainsi que la date sont enregistrés.
- **CA-07.2** — **Given** une approbation concurrente a consommé le solde et qu'il ne reste qu'un jour disponible pour une demande de 2 jours, **When** le manager approuve, **Then** la réponse est `409` avec le code `INSUFFICIENT_BALANCE_AT_APPROVAL`, la demande reste `PENDING` et aucun jour n'est décrémenté.
- **CA-07.3** — **Given** un signal de chevauchement actif, **When** le manager approuve sans acquittement, **Then** la réponse est `409` avec le code `OVERLAP_ACK_REQUIRED` et la demande reste `PENDING`.
- **CA-07.4** — **Given** un signal de chevauchement actif, **When** le manager approuve avec `overlapAcknowledged = true`, **Then** l'approbation est acceptée et l'acquittement est tracé.
- **CA-07.5** — **Given** un employé qui n'est pas le manager direct du demandeur, **When** il tente d'approuver, **Then** la réponse est `403` s'il n'a pas le rôle manager ou `404` si la demande est hors de son périmètre.
- **CA-07.6** — **Given** une demande déjà approuvée, **When** une seconde approbation est tentée, **Then** la réponse est `409` et le solde n'est pas décrémenté une seconde fois.
- **CA-07.7** — **Given** une demande du 31 décembre au 1er janvier, **When** elle est approuvée, **Then** les jours sont répartis entre les deux années de congé et chaque solde annuel est décrémenté de sa part ; si l'un des deux soldes est insuffisant, **Then** l'approbation échoue en `409` sans aucun débit partiel.
- **CA-07.8** — **Given** une demande dont le demandeur est le manager lui-même, **When** il tente de l'approuver, **Then** la réponse est `403`, même si les données le désignent comme manager.

### US-08 — Refuser une demande avec un motif obligatoire

**En tant que** manager, **je veux** refuser une demande en expliquant ma décision, **afin que** l'employé comprenne le refus et puisse s'organiser.

**Règles appliquées :** RB-09, RB-11, RB-16.

**Critères d'acceptation :**

- **CA-08.1** — **Given** une demande en attente, **When** le manager tente de la refuser avec un commentaire vide ou composé uniquement d'espaces, **Then** la réponse est `422` et la demande reste `PENDING`.
- **CA-08.2** — **Given** une demande en attente, **When** le manager la refuse avec un commentaire non vide, **Then** le statut devient `REJECTED`, le solde reste inchangé, le commentaire est enregistré et une notification de décision est créée pour l'employé.
- **CA-08.3** — **Given** une demande refusée du périmètre du manager, **When** il tente de l'approuver, **Then** la réponse est `409` et le statut reste `REJECTED`.
- **CA-08.4** — **Given** un utilisateur sans droit sur la demande, **When** il tente de la refuser, **Then** la réponse est `403` ou `404` selon la portée.

### US-09 — Consulter le calendrier des absences approuvées de l'équipe

**En tant que** membre d'une équipe, **je veux** voir les absences approuvées de mon équipe, **afin de** planifier mon travail et éviter les chevauchements.

**Règles appliquées :** RB-17, RB-20.

**Critères d'acceptation :**

- **CA-09.1** — **Given** une équipe avec une absence approuvée à la demi-journée et plusieurs demandes en attente, refusées ou annulées sur le même mois, **When** un pair consulte le calendrier, **Then** seule l'absence approuvée apparaît, avec la demi-journée correcte, sans type ni commentaire.
- **CA-09.2** — **Given** le manager direct du membre concerné ou un Admin RH, **When** il consulte le calendrier, **Then** le type de congé de ce membre est visible en plus des dates et de l'employé.
- **CA-09.3** — **Given** un employé d'une autre équipe, **When** il tente d'accéder au calendrier, **Then** la réponse est `404`.
- **CA-09.4** — **Given** un mois sans aucune absence approuvée, **When** le calendrier est consulté, **Then** la réponse est `200` avec une liste vide, sans erreur.
- **CA-09.5** — **Given** une absence approuvée annulée, **When** le calendrier est rechargé, **Then** l'absence n'y figure plus.
- **CA-09.6** — **Given** un manager qui manage au moins un membre de l'équipe mais pas l'employé concerné, **When** il consulte le calendrier, **Then** l'absence de cet employé est affichée sans son type de congé.

### US-10 — Annuler une demande en attente

**En tant qu'** employé, **je veux** annuler une demande que je viens de soumettre, **afin de** corriger une erreur sans attendre la décision du manager.

**Règles appliquées :** RB-14, RB-15, RB-18.

**Critères d'acceptation :**

- **CA-10.1** — **Given** une demande de l'employé au statut `PENDING`, **When** il l'annule, **Then** le statut devient `CANCELLED`, le solde reste inchangé et la demande disparaît de la file du manager.
- **CA-10.2** — **Given** une demande qui vient d'être approuvée par le manager, **When** l'employé tente de l'annuler comme une demande en attente, **Then** la réponse est `409` et le statut reste `APPROVED`.
- **CA-10.3** — **Given** la demande d'un autre employé, **When** l'employé tente de l'annuler, **Then** la réponse est `404`.
- **CA-10.4** — **Given** une annulation réussie, **When** l'Admin RH consulte l'audit, **Then** l'acteur, la date et la demande annulée sont présents.

### US-11 — Annuler un congé approuvé à venir

**En tant qu'** employé, **je veux** annuler un congé déjà approuvé mais non commencé, **afin de** récupérer mes jours et de libérer la période pour l'équipe.

**Règles appliquées :** RB-05, RB-14, RB-15, RB-18.

**Critères d'acceptation :**

- **CA-11.1** — **Given** un congé approuvé dont la date de début est strictement postérieure à aujourd'hui, **When** l'employé l'annule, **Then** le statut devient `CANCELLED`, les jours sont restaurés sur la ou les années concernées et le calendrier d'équipe est mis à jour.
- **CA-11.2** — **Given** un congé approuvé dont la date de début est aujourd'hui ou passée, **When** l'employé tente de l'annuler, **Then** la réponse est `409` et aucun jour n'est restauré.
- **CA-11.3** — **Given** un congé annulé, **When** l'employé tente de l'annuler à nouveau, **Then** la réponse est `409` et le statut reste `CANCELLED`.
- **CA-11.4** — **Given** une annulation réussie, **When** l'employé soumet une nouvelle demande sur les mêmes dates, **Then** elle est acceptée si les autres règles sont respectées.

### US-12 — Paramétrer les types de congés

**En tant qu'** Admin RH, **je veux** créer, modifier et désactiver des types de congés, **afin d'** adapter l'application aux règles de l'entreprise.

**Règles appliquées :** RB-01, RB-03, RB-17, RB-18.

**Critères d'acceptation :**

- **CA-12.1** — **Given** un Admin RH authentifié, **When** il crée un type de code `PARENTAL` avec un libellé et une couleur, **Then** le type est créé, actif et proposé aux employés.
- **CA-12.2** — **Given** un type dont le code existe déjà, quelle que soit la casse, **When** l'Admin RH tente de créer un doublon, **Then** la réponse est `409`.
- **CA-12.3** — **Given** un employé sans rôle Admin RH, **When** il tente de créer ou modifier un type, **Then** la réponse est `403`.
- **CA-12.4** — **Given** un type actif utilisé par des demandes existantes, **When** l'Admin RH le désactive, **Then** aucune nouvelle demande ne peut l'utiliser, mais les demandes en attente et approuvées existantes restent inchangées et consultables.
- **CA-12.5** — **Given** un type existant, **When** une suppression définitive est tentée, **Then** l'opération est refusée : seule la désactivation est autorisée.

### US-13 — Attribuer et ajuster les soldes

**En tant qu'** Admin RH, **je veux** attribuer un solde annuel et l'ajuster avec un motif, **afin de** corriger une situation sans modifier l'historique des demandes.

**Règles appliquées :** RB-04, RB-05, RB-17, RB-18, RB-19.

**Critères d'acceptation :**

- **CA-13.1** — **Given** un employé sans solde CP pour 2026, **When** l'Admin RH lui accorde 20 jours, **Then** un mouvement d'octroi de +20 est créé, le disponible devient 20,0 et l'opération est auditée.
- **CA-13.2** — **Given** un solde CP 2026 de 20 jours octroyés et 3 jours consommés, **When** l'Admin RH applique un ajustement de +2 avec le motif « régularisation 2025 », **Then** le disponible devient 19,0 et l'employé le voit.
- **CA-13.3** — **Given** un ajustement négatif qui rendrait le disponible inférieur à 0, **When** l'Admin RH tente de l'appliquer, **Then** la réponse est `422` et le solde reste inchangé.
- **CA-13.4** — **Given** un ajustement sans motif ou avec un motif vide, **When** l'Admin RH tente de l'appliquer, **Then** la réponse est `422`.
- **CA-13.5** — **Given** un manager ou un employé, **When** il tente d'ajuster un solde, **Then** la réponse est `403`. Le manager peut seulement lire les soldes de ses managés.
- **CA-13.6** — **Given** un ajustement de −1 sur un solde de 3 jours consommés et 20 octroyés, **When** il est appliqué, **Then** le disponible passe de 17 à 16 et l'ajustement est tracé avec son auteur et son motif.

### US-14 — Gérer les comptes, les équipes et les lignes de management

**En tant qu'** Admin RH, **je veux** créer et mettre à jour les comptes et leurs rattachements, **afin que** chaque employé ait un manager et une équipe corrects.

**Règles appliquées :** RB-01, RB-02, RB-17, RB-18, RB-21.

**Critères d'acceptation :**

- **CA-14.1** — **Given** un Admin RH authentifié, **When** il crée un utilisateur avec un email unique, une équipe, un manager actif et le rôle Employé, **Then** le compte est créé, actif et peut se connecter après définition de son mot de passe.
- **CA-14.2** — **Given** un email déjà utilisé, **When** l'Admin RH tente de créer un compte, **Then** la réponse est `409`.
- **CA-14.3** — **Given** un manager renseigné comme son propre manager, **When** l'Admin RH enregistre, **Then** la réponse est `422` et aucun rattachement n'est créé.
- **CA-14.4** — **Given** un employé avec une demande en attente et un congé approuvé à venir, **When** l'Admin RH le désactive, **Then** la demande en attente et le congé futur sont annulés avec un motif système, les jours du congé futur sont restaurés, l'utilisateur ne peut plus se connecter et son historique passé reste consultable.
- **CA-14.5** — **Given** un employé avec une demande en attente, **When** l'Admin RH change son manager ou son équipe, **Then** la demande en attente est désormais visible et décidable par le nouveau manager, et le calcul de chevauchement utilise la nouvelle équipe.
- **CA-14.6** — **Given** le dernier Admin RH actif, **When** il tente de se désactiver ou de retirer son propre rôle Admin RH, **Then** la réponse est `409` et le compte reste actif.
- **CA-14.7** — **Given** un employé sans rôle Admin RH, **When** il tente de modifier une équipe, un rôle ou un manager, **Then** la réponse est `403`.

### US-15 — Recevoir les notifications de soumission et de décision

**En tant qu'** utilisateur concerné, **je veux** recevoir un email à la soumission et à la décision, **afin d'** être informé sans consulter l'application en permanence.

**Règles appliquées :** RB-09, RB-10, RB-11, RB-16.

**Critères d'acceptation :**

- **CA-15.1** — **Given** une demande soumise et acceptée par les contrôles, **When** la transaction est commitée, **Then** une notification de soumission est créée pour le manager direct, avec un lien vers la demande, sans type de congé ni commentaire dans le corps de l'email.
- **CA-15.2** — **Given** une demande approuvée ou refusée, **When** la décision est commitée, **Then** une notification de décision est créée pour l'employé, avec le statut et un lien vers la demande.
- **CA-15.3** — **Given** le fournisseur d'email indisponible, **When** la notification échoue, **Then** la demande reste `PENDING` ou `APPROVED` selon le cas, l'échec est enregistré et une nouvelle tentative est planifiée ; aucun rollback métier n'a lieu.
- **CA-15.4** — **Given** une notification en échec puis réessayée avec succès, **When** l'employé consulte ses emails, **Then** il ne reçoit qu'un seul email pour l'événement, pas de doublon.
- **CA-15.5** — **Given** une annulation de demande ou de congé, **When** elle est effectuée, **Then** aucun email n'est obligatoire en v1 ; l'information est visible dans l'application.
- **CA-15.6** — **Given** un employé sans manager, **When** il tente de soumettre, **Then** la demande est refusée avant toute notification avec le code `NO_APPROVER`.

## 5. Règles métier numérotées (incluant la règle de chevauchement à 50 %)

### RB-01 — Comptes, authentification et rôles

- Un compte est créé par l'Admin RH. Il n'existe ni auto-inscription ni création par un employé.
- L'email est unique, insensible à la casse et sert d'identifiant de connexion.
- Chaque compte possède le rôle de base **Employé**. Les rôles **Manager** et **Admin RH** sont additifs.
- Un compte désactivé ne peut plus s'authentifier et ses jetons en cours sont invalidés.
- Le dernier Admin RH actif ne peut pas être désactivé ni perdre son rôle Admin RH.
- Un Admin RH peut réinitialiser le mot de passe d'un compte ; l'utilisateur doit changer ce mot de passe à la première connexion.
- Toute création, modification de rôle, réinitialisation de mot de passe ou désactivation est auditée.

### RB-02 — Organisation, équipes et lignes d'approbation

- L'Admin RH crée les équipes et rattache chaque employé actif à exactement une équipe. Une équipe inactive ne peut plus recevoir de nouveau membre ; les membres existants restent rattachés et consultables.
- Chaque employé actif peut avoir au plus un manager direct, qui doit être un utilisateur actif et différent de lui-même.
- Le manager d'un employé approuve ou refuse ses demandes. Cette autorité est évaluée au moment de la décision, à partir du manager direct courant.
- Un utilisateur ne peut jamais approuver sa propre demande. La demande d'un manager est traitée par son propre manager.
- Le rôle Manager ne donne de droit que sur les managés directs ; il ne donne aucun droit sur l'ensemble de l'équipe si des membres ont un autre manager.
- L'équipe sert au calendrier et au calcul du chevauchement. L'approbation est fondée sur la relation de management direct. Dans l'organisation nominale, tous les membres d'une équipe ont le même manager direct ; si ce n'est pas le cas, les deux périmètres restent distincts et s'appliquent séparément.
- Si un employé n'a pas de manager direct actif, il ne peut pas soumettre de demande : le code `NO_APPROVER` est renvoyé. L'Admin RH doit désigner un manager.
- Un changement de manager ou d'équipe s'applique aux demandes encore en attente ; aucune copie figée de l'ancien manager n'est utilisée pour la décision.

### RB-03 — Types de congés

- Les types par défaut sont **CP**, **RTT**, **Maladie** et **Autre**. Ils sont créés à l'initialisation et ne peuvent pas être supprimés définitivement.
- Chaque type possède un code unique et immuable, un libellé, une couleur et un état actif/inactif.
- L'Admin RH peut créer des types supplémentaires, modifier le libellé, la couleur et l'état, mais pas le code après création.
- Un type inactif ne peut plus être utilisé pour une nouvelle demande. Les demandes existantes et les soldes associés restent inchangés et consultables.
- Tous les types consomment un solde. La v1 n'accorde aucun traitement particulier à la maladie : pas de justificatif, pas de circuit dérogatoire, pas de saisie rétroactive.
- Un type utilisé ne peut pas être supprimé ; seule la désactivation est autorisée.

### RB-04 — Année de congé et soldes annuels

- L'année de congé va du 1er janvier au 31 décembre, en fuseau `Europe/Paris`.
- Il existe au plus un solde par employé, type de congé et année.
- L'Admin RH attribue les jours octroyés. Aucune acquisition automatique, aucun prorata temporis et aucun report automatique ne sont calculés en v1.
- Un solde non attribué vaut 0,0. Une demande sur un solde à 0,0 est refusée pour solde insuffisant.
- Le report d'une année sur l'autre s'effectue uniquement par un ajustement manuel motivé de l'Admin RH.
- Les soldes d'un type désactivé restent consultables et conservent leur historique.
- Une demande peut chevaucher plusieurs années civiles ; elle est alors répartie entre les années de congé concernées et chaque solde est contrôlé et mouvementé séparément.

### RB-05 — Calcul et contrôle du solde disponible

- Pour un employé, un type et une année : `disponible = jours octroyés − jours consommés`.
- Les **jours octroyés** incluent l'attribution initiale et tous les ajustements, positifs ou négatifs, de l'année.
- Les **jours consommés** sont la somme des jours des demandes approuvées et non annulées. Les demandes en attente ne sont ni consommées ni réservées.
- Les valeurs sont exprimées en jours avec une précision de 0,5.
- Le disponible ne peut jamais être négatif.
- Le contrôle est effectué à la soumission et de nouveau à l'approbation. À la soumission, un solde insuffisant renvoie `422`. À l'approbation, un solde insuffisant renvoie `409` avec `INSUFFICIENT_BALANCE_AT_APPROVAL` et la demande reste en attente.
- Aucune approbation concurrente ne peut rendre un solde négatif : la vérification et la décrémentation sont atomiques (RB-22).

### RB-06 — Décompte des jours et des demi-journées

- Seuls les jours ouvrés du lundi au vendredi sont décomptés. Les samedis et dimanches sont exclus. Les jours fériés ne sont pas gérés et sont comptés comme des jours ouvrés (H-04).
- Une journée est divisée en deux demi-journées : `AM` (matin) et `PM` (après-midi). Une journée complète vaut 1,0 jour ; une demi-journée vaut 0,5 jour.
- La date de début et la date de fin doivent être des jours ouvrés. Une demande commençant ou se terminant un week-end est refusée.
- Le nombre de jours est calculé à partir de la première demi-journée incluse jusqu'à la dernière demi-journée incluse, en excluant les week-ends.
- Exemples : lundi `AM` à lundi `PM` = 1,0 jour ; lundi `PM` à mardi `AM` = 1,0 jour ; lundi `AM` à mardi `PM` = 2,0 jours ; vendredi `PM` à lundi `AM` = 1,0 jour.
- Une demande dont la date de fin est antérieure à la date de début est invalide. Le cas « même jour, début `PM`, fin `AM` » est invalide.
- Une demande peut traverser une fin d'année ; les jours sont alors répartis par année civile pour les contrôles de solde et les mouvements.

### RB-07 — Soumission d'une demande

- Une demande est créée par un employé pour lui-même. Aucune soumission pour autrui n'est autorisée.
- Les champs obligatoires sont : type de congé, date de début, demi-journée de début, date de fin, demi-journée de fin. Un commentaire facultatif de 0 à 1 000 caractères est accepté.
- Le type doit être actif.
- La date de début doit être aujourd'hui ou une date future, en `Europe/Paris`. Aucune saisie rétroactive n'est acceptée en v1, y compris pour la maladie (H-15).
- La demande est contrôlée contre le solde, le chevauchement personnel et l'existence d'un manager direct actif.
- Une demande créée passe directement au statut `PENDING`. Il n'existe pas de brouillon.
- La soumission ne réserve pas de solde. Deux demandes non chevauchantes peuvent donc être soumises avec le même solde ; l'approbation reste le point de contrôle définitif.
- Une demande soumise n'est plus modifiable. Pour la corriger, l'employé l'annule et en soumet une nouvelle.
- Une seule demande est créée par appel ; les actions groupées sont hors périmètre.

### RB-08 — Interdiction de chevauchement personnel

- Un employé ne peut pas avoir deux demandes actives, c'est-à-dire `PENDING` ou `APPROVED`, qui se chevauchent sur une même demi-journée, quel que soit le type de congé.
- Un chevauchement d'une demi-journée suffit à refuser la soumission avec `409` et le code `PERSONAL_OVERLAP`.
- Deux demandes adjacentes sont autorisées : par exemple une demande terminée le mardi `AM` et une autre commençant le mardi `PM`.
- Les demandes `REJECTED` et `CANCELLED` ne bloquent pas une nouvelle demande sur les mêmes dates.

### RB-09 — Autorité et forme de la décision

- Seul le manager direct courant du demandeur peut approuver ou refuser une demande au statut `PENDING`.
- Le manager ne peut pas décider de sa propre demande, même si les données le désignent comme son manager.
- L'Admin RH ne peut pas décider au titre de son rôle RH. Il peut décider uniquement s'il est le manager direct du demandeur.
- Un refus exige un commentaire non vide après suppression des espaces, de 1 à 1 000 caractères.
- Une approbation accepte un commentaire facultatif de 0 à 1 000 caractères.
- La décision est définitive : elle ne peut pas être modifiée, réouverte ou annulée par un autre utilisateur.
- Une demande en attente peut être décidée même si sa date de début est passée ; aucune expiration automatique n'est appliquée en v1.
- Les décisions groupées sont hors périmètre.

### RB-10 — Effets de l'approbation

- L'approbation vérifie à nouveau : l'autorité du manager, le statut `PENDING`, la présence d'un solde suffisant sur chaque année concernée, l'absence de chevauchement personnel nouveau et, si le signal de chevauchement est actif, l'acquittement du manager (RB-13).
- En cas de succès, dans une même transaction : le statut passe à `APPROVED`, le décideur et la date sont enregistrés, les jours sont décomptés par année et par type, les mouvements de solde sont créés, l'audit est écrit et la notification de décision est planifiée.
- L'approbation est atomique : aucun débit partiel n'est possible si une année est insuffisante.
- Une approbation ne peut pas être appliquée deux fois. Une seconde tentative renvoie `409` et ne produit aucun débit supplémentaire.
- L'approbation est possible quel que soit le nombre de jours, sans plafond de durée en v1.
- L'approbation ne nécessite pas de confirmation supplémentaire de l'employé.

### RB-11 — Effets du refus

- Le refus exige un commentaire non vide (RB-09) et fait passer la demande de `PENDING` à `REJECTED`.
- Le refus ne modifie pas le solde.
- Le décideur, la date et le commentaire sont enregistrés et audités.
- L'employé peut soumettre une nouvelle demande sur les mêmes dates, sous réserve des autres règles.
- Une demande refusée est terminale : elle ne peut plus être approuvée.

### RB-12 — Règle de chevauchement à 50 %

**Principe.** Une demande est signalée lorsqu'elle ferait dépasser à son équipe un taux d'absence strictement supérieur à 50 % sur au moins une demi-journée qu'elle couvre. Le taux est calculé **demande incluse**, car le signal doit permettre au manager d'éviter un chevauchement critique.

**Définitions et formule.**

Pour une demande d'un employé E de l'équipe T, et pour chaque demi-journée ouvrée `h` couverte par la demande :

- `N` = nombre de membres actifs de l'équipe T à la date d'évaluation, E inclus.
- `A(h)` = nombre d'autres membres actifs de T ayant une demande **approuvée** couvrant `h`. Un membre est compté une seule fois par demi-journée, qu'il soit absent toute la journée ou seulement cette demi-journée.
- `T(h) = (A(h) + 1) / N`. Le `+1` représente l'absence de E qui résulterait de l'approbation de la demande.
- La demande est signalée si `max(T(h)) > 0,50` sur au moins une demi-journée `h` et si `N ≥ 2`. Si `N = 1`, aucun signal n'est produit.

**Précisions.**

- Seules les demandes au statut `APPROVED` sont comptées. Les demandes `PENDING`, `REJECTED` et `CANCELLED` sont ignorées.
- Tous les types de congés sont comptés, sans exception.
- Une demande approuvée d'une demi-journée ne compte que sur la demi-journée concernée. Une demande approuvée complète compte sur le matin et l'après-midi.
- Le demandeur est exclu de `A(h)` et ajouté une seule fois via le `+1`.
- Les membres inactifs ne sont pas comptés dans `N` ni dans `A(h)`.
- Le seuil est strict : exactement 50 % ne déclenche pas le signal.
- Les week-ends ne sont pas évalués. Les jours fériés sont évalués comme des jours ordinaires en v1.
- Le signal est une aide à la décision, jamais un blocage automatique. Le manager peut approuver après acquittement (RB-13).

**Exemples de calcul.**

| Équipe | Absences approuvées des autres sur la demi-journée | Demande sur la demi-journée | Taux projeté | Signal |
|---|---|---|---|---|
| 6 membres | 2 absents toute la journée | journée complète | (2 + 1) / 6 = 50,0 % | Non |
| 6 membres | 3 absents toute la journée | journée complète | (3 + 1) / 6 = 66,7 % | Oui |
| 6 membres | 4 absents toute la journée | journée complète | (4 + 1) / 6 = 83,3 % | Oui |
| 6 membres | 2 toute la journée + 2 le matin | journée complète | matin : (4 + 1) / 6 = 83,3 % ; après-midi : (2 + 1) / 6 = 50,0 % | Oui (matin) |
| 6 membres | 2 toute la journée + 2 le matin | après-midi seulement | (2 + 1) / 6 = 50,0 % | Non |
| 2 membres | 1 absent toute la journée | journée complète | (1 + 1) / 2 = 100 % | Oui |
| 1 membre | aucun autre membre | journée complète | non applicable | Non |

**Détail conservé avec le signal.** L'application conserve, à chaque évaluation, l'équipe évaluée, `N`, le taux maximal, les demi-journées en dépassement et l'horodatage. L'Admin RH peut consulter ce détail ; l'employé ne le voit pas.

### RB-13 — Affichage, acquittement et cycle de vie du signal de chevauchement

- Le signal est calculé à la soumission et enregistré avec la demande.
- Il est recalculé à chaque consultation du détail par le manager et juste avant une décision, afin de tenir compte des approbations intervenues entre-temps.
- Si le signal est actif au moment de l'approbation et que le manager n'a pas transmis `overlapAcknowledged = true`, l'approbation est refusée avec `409` et le code `OVERLAP_ACK_REQUIRED`. La demande reste `PENDING`.
- Si le manager acquitte le signal, l'acquittement est horodaté et associé au manager ; le serveur recalcule le signal au moment de l'approbation et enregistre le détail de cette évaluation avec l'acquittement.
- Un refus ne nécessite aucun acquittement, même si le signal est actif.
- Le signal, son détail et l'acquittement sont visibles du manager du demandeur et de l'Admin RH. Ils ne sont pas exposés à l'employé dans ses propres réponses.
- L'email de soumission peut mentionner qu'une alerte de capacité existe, sans exposer le type de congé ni les commentaires.
- Le signal n'est jamais un refus automatique et ne modifie jamais le solde à lui seul.

### RB-14 — Annulation

- Une demande `PENDING` peut être annulée par son titulaire à tout moment. Elle passe à `CANCELLED`, sans effet sur le solde. Si une décision a été enregistrée entre-temps, l'annulation échoue en `409`.
- Une demande `APPROVED` peut être annulée par son titulaire uniquement si sa date de début est strictement postérieure à la date du jour. Les jours sont alors restaurés sur la ou les années concernées.
- Un congé déjà commencé ou passé ne peut pas être annulé par l'employé. Aucune annulation par le manager ou l'Admin RH n'est prévue en v1, à l'exception de l'annulation automatique liée à la désactivation d'un compte (RB-21).
- Une annulation est définitive. Une demande annulée ne peut pas être réouverte ; une nouvelle demande doit être soumise.
- Une annulation produit un mouvement de restauration et une entrée d'audit.
- L'annulation d'un congé approuvé futur libère la période dans le calendrier et dans le calcul de chevauchement des demandes suivantes.
- Aucun email d'annulation n'est obligatoire en v1.

### RB-15 — Immutabilité des demandes

- Une demande soumise ne peut pas être modifiée : ni les dates, ni le type, ni les demi-journées, ni le commentaire.
- Aucun statut ne peut être changé en dehors des transitions autorisées : `PENDING → APPROVED`, `PENDING → REJECTED`, `PENDING → CANCELLED`, `APPROVED → CANCELLED` (uniquement dans les cas RB-14 et RB-21).
- Une demande `REJECTED` ou `CANCELLED` est terminale.
- Aucune suppression de demande n'est autorisée, y compris par l'Admin RH. La conservation de l'historique est obligatoire.
- Une correction se fait par annulation puis nouvelle demande, ou par ajustement de solde motivé lorsque seule la valeur du solde est en cause.
- L'Admin RH ne peut pas modifier les dates, le statut ou le décideur d'une demande.

### RB-16 — Notifications email

- Deux notifications sont obligatoires :
  1. **Soumission** : email au manager direct du demandeur, après commit de la demande.
  2. **Décision** : email à l'employé, après commit de l'approbation ou du refus.
- Chaque événement produit au plus un email, même en cas de nouvelles tentatives.
- L'envoi est asynchrone et n'est pas dans la transaction métier. Un échec d'email ne modifie pas le statut de la demande.
- Les tentatives sont retentées selon une politique d'au moins trois essais. Après échec définitif, la notification est marquée en échec et reste consultable par l'Admin RH.
- Le contenu minimal contient : le nom de l'employé concerné, les dates, le nombre de jours, le statut et un lien sécurisé vers l'application. Il ne contient ni type de congé, ni commentaire, ni détail médical, ni pièce jointe.
- L'email de soumission peut signaler l'existence d'une alerte de capacité, sans détail du chevauchement.
- Aucun email n'est obligatoire pour une annulation, une modification administrative, un rappel ou une relance.
- Si le manager n'existe pas, la demande est refusée avant notification (RB-02).

### RB-17 — Visibilité et confidentialité

- L'employé voit ses propres demandes, ses soldes, son profil et le calendrier de son équipe.
- Le manager voit les demandes et les soldes de ses managés directs, le calendrier des équipes dont il manage au moins un membre, et son propre espace.
- L'Admin RH voit toutes les demandes, tous les soldes, tous les calendriers et l'audit. Il ne peut pas décider hors rôle manager.
- Le calendrier d'équipe affiché aux pairs contient uniquement le nom de l'employé, les dates et les demi-journées des absences approuvées. Il ne contient ni type de congé, ni commentaire, ni motif.
- Le manager et l'Admin RH voient le type et les commentaires des demandes de leur périmètre.
- Le signal de chevauchement et son détail sont réservés au manager du demandeur et à l'Admin RH.
- Les commentaires de refus sont visibles de l'employé, du manager décideur et de l'Admin RH. Ils ne sont pas exposés aux autres managers ni aux pairs.
- Une ressource hors périmètre est renvoyée en `404` pour ne pas révéler son existence. Une action interdite à un rôle authentifié est renvoyée en `403`.
- Aucune donnée n'est accessible sans authentification.

### RB-18 — Journal d'audit

- Sont audités au minimum : les décisions d'approbation et de refus, les annulations, les acquittements de chevauchement, les ajustements de solde, les créations/modifications de types, les changements de compte, de rôle, d'équipe et de manager, les désactivations et les réinitialisations de mot de passe.
- Chaque entrée contient : identifiant, acteur, action, type d'entité, identifiant de l'entité, horodatage, résumé avant/après et, pour un ajustement, le motif.
- Les entrées sont en lecture seule et ne peuvent être ni modifiées ni supprimées via l'application.
- L'Admin RH peut consulter l'audit ; les autres rôles n'y ont pas accès.
- L'audit ne duplique pas les données de santé : il référence les entités par identifiant et n'enregistre pas le libellé « maladie » ni les commentaires libres.

### RB-19 — Ajustements de solde

- Seul l'Admin RH peut créer un ajustement de solde.
- Un ajustement porte sur un employé, un type et une année. Il est exprimé en jours, avec une précision de 0,5, et peut être positif ou négatif.
- Le motif est obligatoire, de 1 à 1 000 caractères.
- Un ajustement ne peut pas rendre le disponible négatif compte tenu des jours déjà consommés.
- Un ajustement ne modifie jamais le statut d'une demande.
- Un ajustement ne peut pas être modifié ni supprimé ; une correction se fait par un nouvel ajustement de sens opposé, lui aussi motivé.
- L'attribution initiale d'un solde est un ajustement de nature « octroi initial ».
- Chaque ajustement produit un mouvement de solde et une entrée d'audit consultables par l'Admin RH.
- L'employé voit la valeur résultante de son solde, mais pas le détail des ajustements d'autres employés.

### RB-20 — Calendrier d'équipe

- Le calendrier affiche les absences `APPROVED` des membres actifs de l'équipe sur une période demandée, par défaut le mois courant.
- Les demandes `PENDING`, `REJECTED` et `CANCELLED` n'apparaissent jamais.
- Une absence d'une demi-journée est affichée sur la demi-journée concernée ; une absence complète apparaît sur le matin et l'après-midi.
- Les week-ends peuvent être affichés comme repères mais ne portent aucune absence décomptée.
- L'accès est limité aux membres de l'équipe, au manager d'au moins un membre de l'équipe et à l'Admin RH.
- Le calendrier utilise l'équipe courante de l'employé. Un changement d'équipe reclasse donc toutes ses absences dans le calendrier de la nouvelle équipe, y compris pour les périodes passées.
- Le calendrier est en lecture seule ; aucune action de décision ou de modification n'y est possible.
- Le type de congé et les commentaires ne sont pas affichés aux pairs (RB-17). Le type n'est visible que de l'Admin RH et du manager direct de l'employé concerné ; pour les autres membres, l'absence est générique.

### RB-21 — Cycle de vie des comptes et des rattachements

- L'Admin RH crée, modifie et désactive les comptes. Aucun compte n'est supprimé définitivement via l'application ; la purge ou l'anonymisation après la durée de conservation relève d'une procédure administrative encadrée par le DPO.
- La désactivation coupe l'authentification et invalide les jetons en cours.
- Lors de la désactivation d'un employé :
  - ses demandes `PENDING` sont annulées avec un motif système ;
  - ses demandes `APPROVED` dont la date de début est future sont annulées avec un motif système et les jours sont restaurés ;
  - ses demandes passées restent dans l'historique et dans les calendriers passés.
- Aucun email n'est envoyé lors de l'annulation automatique liée à une désactivation.
- Un changement d'équipe ou de manager est possible ; les demandes en attente suivent les nouvelles relations et le nouveau périmètre de chevauchement.
- Un utilisateur ne peut pas modifier lui-même son équipe, son manager ou ses rôles.
- L'Admin RH ne peut pas désactiver le dernier Admin RH actif ni lui retirer son rôle.
- Toute opération est auditée.

### RB-22 — Concurrence, atomicité et idempotence

- Une décision, une annulation ou un ajustement est atomique : soit toutes les écritures associées sont appliquées, soit aucune.
- Deux décisions concurrentes sur la même demande ne peuvent pas produire deux débits : la première transaction valide gagne, la seconde renvoie `409` avec le statut courant.
- Une approbation et une annulation concurrentes suivent la même règle : la première transition valide gagne, l'autre échoue.
- Le contrôle de solde et la décrémentation sont réalisés dans la même unité de travail, de façon à interdire tout solde négatif.
- Un même événement de notification ne produit qu'un seul email, même si le traitement est rejoué.
- Les opérations sont idempotentes du point de vue métier : réessayer une opération déjà appliquée ne crée pas de double effet.

## 6. Modèle de données fonctionnel (entités, attributs, relations, sans SQL)

### 6.1 Principes

- Les identifiants sont opaques et uniques. Les dates sont des dates civiles sans heure ; les horodatages sont en UTC et affichés en `Europe/Paris`.
- Les durées sont exprimées en jours, avec une précision de 0,5.
- Aucune entité historique n'est supprimée physiquement par l'application.
- Le modèle ci-dessous est fonctionnel : il décrit les informations et leurs relations, pas les tables, index ou types SQL.

### 6.2 Entités

#### Utilisateur

| Attribut | Description |
|---|---|
| `id` | Identifiant unique. |
| `email` | Email professionnel, unique, insensible à la casse, identifiant de connexion. |
| `prenom`, `nom` | Identité affichée. |
| `motDePasseHache` | Empreinte du mot de passe, jamais exposée. |
| `roles` | Ensemble non vide : `EMPLOYE` (obligatoire), `MANAGER`, `ADMIN_RH`. |
| `equipeId` | Équipe de rattachement, obligatoire pour un compte actif. |
| `managerId` | Manager direct, facultatif, différent de l'utilisateur. |
| `actif` | Vrai si le compte peut se connecter. |
| `doitChangerMotDePasse` | Vrai après une création ou une réinitialisation. |
| `dateCreation`, `dateMiseAJour`, `derniereConnexion` | Horodatages de gestion. |

#### Équipe

| Attribut | Description |
|---|---|
| `id` | Identifiant unique. |
| `nom` | Nom unique et lisible. |
| `actif` | Une équipe inactive n'accueille plus de nouveaux membres mais reste consultable. |
| `dateCreation`, `dateMiseAJour` | Horodatages. |

#### TypeDeConge

| Attribut | Description |
|---|---|
| `id` | Identifiant unique. |
| `code` | Code unique et immuable (`CP`, `RTT`, `MALADIE`, `AUTRE` ou code personnalisé). |
| `libelle` | Libellé affiché. |
| `couleur` | Couleur d'affichage du calendrier. |
| `actif` | Autorise ou interdit les nouvelles demandes. |
| `estSysteme` | Vrai pour les types par défaut non supprimables. |
| `dateCreation`, `dateMiseAJour` | Horodatages. |

#### SoldeAnnuel

| Attribut | Description |
|---|---|
| `employeId`, `typeId`, `annee` | Clé fonctionnelle unique. |
| `joursOctroyes` | Total des jours accordés, y compris ajustements. |
| `joursConsommes` | Total des jours des demandes approuvées non annulées. |
| `disponible` | Calculé : `joursOctroyes − joursConsommes` ; jamais négatif. |
| `version` | Utilisé pour détecter les mises à jour concurrentes. |

#### MouvementDeSolde

| Attribut | Description |
|---|---|
| `id` | Identifiant unique. |
| `employeId`, `typeId`, `annee` | Solde concerné. |
| `deltaJours` | Variation signée, multiple de 0,5. |
| `nature` | `OCTROI_INITIAL`, `AJUSTEMENT`, `CONSOMMATION_APPROBATION`, `RESTITUTION_ANNULATION`. |
| `demandeId` | Demande à l'origine d'une consommation ou d'une restitution, facultatif. |
| `motif` | Obligatoire pour un ajustement. |
| `acteurId` | Auteur du mouvement. |
| `dateMouvement` | Horodatage. |

Le solde est l'état courant ; le mouvement est la trace unitaire de chaque variation. Un solde est reconstructible à partir de ses mouvements.

#### DemandeDeConge

| Attribut | Description |
|---|---|
| `id` | Identifiant unique. |
| `employeId` | Demandeur. |
| `typeId` | Type de congé. |
| `dateDebut`, `demiJourDebut` | Date et `AM`/`PM` de début. |
| `dateFin`, `demiJourFin` | Date et `AM`/`PM` de fin. |
| `nombreJours` | Nombre total de jours, multiple de 0,5. |
| `repartitionParAnnee` | Répartition des jours par année de congé, pour les demandes à cheval sur deux années. |
| `statut` | `PENDING`, `APPROVED`, `REJECTED` ou `CANCELLED`. |
| `commentaireEmploye` | Commentaire facultatif, 0 à 1 000 caractères. |
| `dateSoumission` | Horodatage de création. |
| `decideurId`, `dateDecision` | Auteur et date de la décision. |
| `commentaireDecision` | Commentaire du manager, obligatoire pour un refus. |
| `signalChevauchement` | Booléen indiquant si la demande a été signalée lors de la dernière évaluation. |
| `detailChevauchement` | Détail de la dernière évaluation : équipe, `N`, taux maximal, demi-journées en dépassement, horodatage. |
| `acquitteParId`, `dateAcquittement` | Manager et date de l'acquittement du signal, si applicable. |
| `dateAnnulation`, `annuleParId`, `motifAnnulation` | Informations d'annulation, y compris annulation système. |
| `version` | Détection des mises à jour concurrentes. |

#### Notification

| Attribut | Description |
|---|---|
| `id` | Identifiant unique. |
| `type` | `SOUMISSION` ou `DECISION`. |
| `demandeId` | Demande concernée. |
| `destinataire` | Adresse email du destinataire. |
| `statut` | `EN_ATTENTE`, `ENVOYEE` ou `ECHEC`. |
| `tentatives` | Nombre de tentatives. |
| `derniereErreur` | Résumé de la dernière erreur, sans donnée sensible. |
| `dateCreation`, `dateEnvoi` | Horodatages. |
| `cleEvenement` | Clé unique de déduplication. |

#### EntreeAudit

| Attribut | Description |
|---|---|
| `id` | Identifiant unique. |
| `acteurId` | Utilisateur ou composant système à l'origine de l'action. |
| `action` | Action normalisée. |
| `entiteType`, `entiteId` | Entité concernée. |
| `horodatage` | Date et heure. |
| `resumeAvant`, `resumeApres` | Résumé des valeurs utiles, sans données de santé. |
| `motif` | Motif pour les ajustements et annulations système. |

### 6.3 Relations

- Une **Équipe** regroupe zéro ou plusieurs **Utilisateurs**. Un utilisateur actif appartient à exactement une équipe.
- Un **Utilisateur** peut être le manager direct de zéro ou plusieurs **Utilisateurs**. La relation est réflexive et interdit l'auto-référence.
- Un **Utilisateur** est l'auteur de zéro ou plusieurs **DemandesDeConge**.
- Un **TypeDeConge** est utilisé par zéro ou plusieurs **DemandesDeConge** et **SoldesAnnuels**.
- Un **SoldeAnnuel** est unique pour un triplet (`employeId`, `typeId`, `annee`).
- Une **DemandeDeConge** peut produire zéro ou plusieurs **MouvementsDeSolde** (consommation, restitution).
- Une **DemandeDeConge** peut produire zéro ou plusieurs **Notifications** (une de soumission, une de décision).
- Une **EntreeAudit** référence une entité par son type et son identifiant ; elle n'a pas de dépendance de suppression.
- Aucune entité « jour férié », « pièce jointe » ou « délégation » n'existe en v1.

### 6.4 Transitions de statut

| Statut courant | Action | Acteur | Statut cible | Effets |
|---|---|---|---|---|
| `PENDING` | Approuver | Manager direct | `APPROVED` | Décompte du solde, audit, notification. |
| `PENDING` | Refuser | Manager direct | `REJECTED` | Aucun décompte, audit, notification. |
| `PENDING` | Annuler | Titulaire | `CANCELLED` | Aucun décompte, audit. |
| `PENDING` | Désactivation du compte | Admin RH | `CANCELLED` | Aucun décompte, audit, annulation système. |
| `APPROVED` | Annuler un congé futur | Titulaire | `CANCELLED` | Restauration du solde, audit, calendrier mis à jour. |
| `APPROVED` | Désactivation du compte (congé futur) | Admin RH | `CANCELLED` | Restauration du solde, audit, annulation système. |
| `REJECTED` | — | — | Terminal | Aucune transition. |
| `CANCELLED` | — | — | Terminal | Aucune transition. |

### 6.5 Invariants fonctionnels

- Un solde disponible n'est jamais négatif.
- Une demande active ne chevauche jamais une autre demande active du même employé.
- Une demande `APPROVED` possède toujours un décideur et une date de décision.
- Une demande `REJECTED` possède toujours un commentaire de décision non vide.
- Une demande `CANCELLED` possède toujours une date d'annulation et un auteur.
- Une demande n'est jamais supprimée.
- Un mouvement de solde est immuable.
- Le nombre de jours d'une demande est toujours un multiple de 0,5 et strictement positif.
- Une demande ne peut être `APPROVED` que si elle a été soumise, si son statut était `PENDING` et si un décideur autorisé l'a approuvée.

## 7. Exigences non fonctionnelles (performance, sécurité, disponibilité, accessibilité, RGPD)

### 7.1 Performance

- **Charge cible** : 50 utilisateurs actifs, 20 utilisateurs simultanés, 5 requêtes par seconde en pointe, environ 10 000 demandes conservées sur cinq ans.
- **Temps de réponse API** : p95 ≤ 300 ms pour les lectures et ≤ 500 ms pour les écritures, hors envoi d'email, dans les conditions de charge cible.
- **Calcul de chevauchement** : ≤ 100 ms pour une demande de 60 jours ouvrés et une équipe de 20 membres.
- **Calendrier mensuel** : ≤ 800 ms pour une équipe de 20 membres et un mois de données.
- **Interface** : premier affichage utile ≤ 2,5 s sur le réseau de l'entreprise ; interaction perçue ≤ 200 ms pour les actions courantes.
- **Pagination** : toute liste est paginée, avec 50 éléments par défaut et 100 maximum.
- **Volume** : la performance ne doit pas se dégrader de façon perceptible sur cinq ans d'historique.

### 7.2 Sécurité

- Toutes les communications passent par HTTPS avec TLS 1.2 minimum, TLS 1.3 recommandé ; HSTS activé.
- Les mots de passe sont stockés sous forme de hash fort avec sel (Argon2id ou bcrypt avec coût adapté), jamais en clair ni réversible.
- Les mots de passe font au moins 12 caractères ; aucune limite arbitraire de composition n'est imposée au-delà de cette longueur minimale.
- Les jetons JWT sont signés, à durée de vie courte (15 minutes pour l'accès, 7 jours pour le rafraîchissement avec rotation), et contiennent uniquement les informations nécessaires (identité, rôles, expiration, identifiant de jeton).
- Les jetons de rafraîchissement sont révoqués à la désactivation, au changement de rôle, au changement de mot de passe et à la déconnexion.
- Les contrôles d'autorisation sont appliqués côté serveur, selon le principe du moindre privilège et du refus par défaut.
- Les entrées sont validées côté serveur ; les sorties sont encodées pour prévenir XSS ; les requêtes sont protégées contre CSRF lorsque des cookies sont utilisés.
- La limitation de débit protège la connexion (5 tentatives par 15 minutes et par compte/IP) et renvoie `429` au-delà.
- Les messages d'erreur d'authentification sont génériques et ne permettent pas d'énumérer les comptes.
- Les secrets, clés de signature et identifiants SMTP sont stockés hors du code source, dans un coffre de secrets.
- Les dépendances sont analysées pour les vulnérabilités connues ; une politique de mise à jour est définie.
- Les journaux techniques ne contiennent ni mot de passe, ni jeton complet, ni type de congé, ni commentaire libre.
- Les en-têtes de sécurité sont configurés : CSP, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`, `X-Frame-Options` ou `frame-ancestors`.
- Une revue de sécurité et un test d'intrusion léger sont réalisés avant la mise en production.

### 7.3 Disponibilité

- **Disponibilité cible** : 99,5 % pendant les heures ouvrées (8 h–19 h, du lundi au vendredi, `Europe/Paris`), hors maintenance planifiée.
- **Maintenance** : annoncée au moins 72 heures à l'avance et planifiée hors heures ouvrées.
- **RTO** : 4 heures. **RPO** : 24 heures.
- **Sauvegardes** : quotidiennes, conservées 30 jours, avec un test de restauration au moins une fois par an.
- **Dégradation** : une panne du fournisseur d'email ne bloque ni les demandes ni les décisions ; les notifications sont mises en file et réessayées.
- **Supervision** : disponibilité, taux d'erreur, latence, échecs de notification et files en attente sont surveillés avec alerte.
- **Reprise** : en cas d'indisponibilité de la base, l'application renvoie un message d'erreur explicite et ne produit aucune écriture partielle.

### 7.4 Accessibilité

- L'interface respecte **WCAG 2.1 niveau AA**.
- Toutes les fonctions sont utilisables au clavier, avec un ordre de focus logique et un focus visible.
- Les champs de formulaire ont des libellés persistants, des messages d'erreur associés et des indications de format.
- Le contraste texte/fond atteint au moins 4,5:1 (3:1 pour les grands textes).
- L'information n'est jamais portée par la couleur seule : le statut et le signal de chevauchement ont un libellé ou une icône accessible.
- Les tableaux et le calendrier sont structurés avec des en-têtes accessibles et une alternative textuelle.
- L'interface est utilisable à 200 % de zoom, sans perte de contenu ni défilement horizontal obligatoire.
- Le site est en français, avec `lang="fr"` et des libellés explicites.
- Le rendu est responsive de 360 px à 1 920 px.
- Les navigateurs supportés sont les deux dernières versions de Chrome, Edge, Firefox et Safari.

### 7.5 RGPD et protection des données

- **Données traitées** : identité professionnelle, email, équipe, manager, rôles, demandes de congé, dates, demi-journées, type, commentaires, soldes, mouvements, décisions, notifications et audit.
- **Donnée sensible** : le type « Maladie » peut révéler l'état de santé. Il est traité comme une donnée de santé à accès restreint. Il n'apparaît ni dans le calendrier des pairs, ni dans les emails, ni dans les journaux techniques.
- **Base légale** : exécution du contrat de travail et obligations légales de l'employeur, et non consentement. Pour la donnée de santé, une condition spécifique de l'article 9.2 du RGPD (obligations en droit du travail) doit être documentée avec le DPO. La notice d'information est fournie aux employés.
- **Minimisation** : seules les données nécessaires à la gestion des congés sont collectées. Aucun champ de santé détaillé, aucun justificatif et aucune pièce jointe ne sont stockés en v1.
- **Accès** : moindre privilège, revue des accès par l'Admin RH, comptes nominatifs, aucune authentification partagée.
- **Chiffrement** : en transit (HTTPS) et au repos pour la base et les sauvegardes.
- **Hébergement** : Union européenne, avec un sous-traitant encadré par un accord de traitement des données (DPA).
- **Durées de conservation** :
  - demandes, décisions, soldes, mouvements et audit : 5 ans après la fin de l'année de congé concernée, sous réserve de la prescription applicable ;
  - notifications et logs d'envoi : 12 mois ;
  - comptes désactivés : conservés pour l'historique pendant la durée légale, puis anonymisés ou purgés selon la politique validée par le DPO ;
  - journaux techniques : 12 mois maximum.
  Ces durées doivent être validées par le DPO ou le conseil juridique (point ouvert PO-05).
- **Droits des personnes** : accès, rectification, effacement, limitation et opposition, exercés via l'Admin RH ou le DPO. L'effacement est limité par les obligations de conservation légale.
- **Sous-traitants** : liste tenue à jour ; aucun transfert hors UE sans garanties appropriées.
- **Violation de données** : procédure de qualification et de notification à la CNIL sous 72 heures si nécessaire.
- **Pas de décision automatisée** : le signal de chevauchement est une aide à la décision ; la décision finale est humaine.
- **Journalisation** : les accès administratifs et les actions sensibles sont audités, sans journaliser les données de santé.

## 8. Ébauche d'API (ressources et verbes, sans implémentation)

### 8.1 Conventions

- Base : `/api/v1`. Contenu JSON en `camelCase`.
- Dates civiles : `YYYY-MM-DD`. Horodatages : ISO 8601 en UTC.
- Demi-journées : `AM` ou `PM`.
- Statuts : `PENDING`, `APPROVED`, `REJECTED`, `CANCELLED`.
- Authentification : en-tête `Authorization: Bearer <jeton>`, sauf pour la connexion et le rafraîchissement.
- Erreurs : `application/problem+json` avec `type`, `title`, `status`, `detail`, `code` et, pour les validations, `errors`.
- Listes : `page` (à partir de 1) et `pageSize` (50 par défaut, 100 maximum), avec métadonnées de pagination.
- Les ressources hors périmètre renvoient `404` ; les rôles insuffisants renvoient `403`.

### 8.2 Ressources et verbes

| Méthode | Ressource | Rôle | Usage |
|---|---|---|---|
| `POST` | `/auth/login` | Public | Authentifier un utilisateur et délivrer les jetons. |
| `POST` | `/auth/refresh` | Public (jeton de rafraîchissement) | Obtenir un nouveau jeton d'accès. |
| `POST` | `/auth/logout` | Authentifié | Révoquer le jeton de rafraîchissement courant. |
| `GET` | `/me` | Tous | Consulter son profil et ses rôles. |
| `PUT` | `/me/password` | Tous | Changer son mot de passe. |
| `GET` | `/me/balances?year=` | Tous | Consulter ses soldes pour une année. |
| `GET` | `/me/leave-requests?status=&year=&page=&pageSize=` | Tous | Lister ses demandes. |
| `POST` | `/me/leave-requests` | Tous | Soumettre une demande pour soi. |
| `GET` | `/me/leave-requests/{id}` | Tous (titulaire) | Consulter le détail d'une de ses demandes. |
| `POST` | `/me/leave-requests/{id}/cancel` | Tous (titulaire) | Annuler une demande en attente ou un congé futur approuvé. |
| `GET` | `/manager/leave-requests?status=&page=&pageSize=` | Manager | Lister les demandes de ses managés directs. |
| `GET` | `/manager/leave-requests/{id}` | Manager | Consulter une demande de son périmètre, avec le signal recalculé. |
| `GET` | `/manager/reports?page=&pageSize=` | Manager | Lister ses managés directs. |
| `GET` | `/manager/reports/{employeeId}/balances?year=` | Manager | Consulter les soldes d'un managé direct. |
| `POST` | `/manager/leave-requests/{id}/approve` | Manager | Approuver une demande. |
| `POST` | `/manager/leave-requests/{id}/reject` | Manager | Refuser une demande avec commentaire. |
| `GET` | `/teams/{teamId}/calendar?from=&to=` | Membre, manager concerné, Admin RH | Consulter les absences approuvées d'une équipe. |
| `GET` | `/leave-types` | Tous | Lister les types actifs. |
| `GET` | `/admin/leave-types?includeInactive=` | Admin RH | Lister tous les types, actifs ou non. |
| `POST` | `/admin/leave-types` | Admin RH | Créer un type. |
| `PATCH` | `/admin/leave-types/{id}` | Admin RH | Modifier libellé, couleur ou état actif. |
| `GET` | `/admin/users?teamId=&active=&page=&pageSize=` | Admin RH | Lister les comptes. |
| `POST` | `/admin/users` | Admin RH | Créer un compte. |
| `PATCH` | `/admin/users/{id}` | Admin RH | Modifier identité, équipe, manager ou rôles. |
| `POST` | `/admin/users/{id}/deactivate` | Admin RH | Désactiver un compte, annuler ses demandes en attente et ses absences futures. |
| `POST` | `/admin/users/{id}/reset-password` | Admin RH | Réinitialiser le mot de passe. |
| `GET` | `/admin/teams?active=&page=&pageSize=` | Admin RH | Lister les équipes. |
| `POST` | `/admin/teams` | Admin RH | Créer une équipe. |
| `PATCH` | `/admin/teams/{id}` | Admin RH | Renommer ou désactiver une équipe. |
| `GET` | `/admin/users/{userId}/balances?year=` | Admin RH | Consulter les soldes d'un employé. |
| `GET` | `/admin/users/{userId}/balance-adjustments?year=&page=&pageSize=` | Admin RH | Lister les ajustements de solde d'un employé. |
| `POST` | `/admin/users/{userId}/balance-adjustments` | Admin RH | Créer un ajustement motivé. |
| `GET` | `/admin/leave-requests?status=&teamId=&employeeId=&from=&to=&page=&pageSize=` | Admin RH | Consulter toutes les demandes. |
| `GET` | `/admin/leave-requests/{id}` | Admin RH | Consulter le détail d'une demande. |
| `GET` | `/admin/notifications?status=&page=&pageSize=` | Admin RH | Consulter les notifications et leurs échecs. |
| `GET` | `/admin/audit-entries?from=&to=&entityType=&page=&pageSize=` | Admin RH | Consulter le journal d'audit. |

### 8.3 Corps principaux

**Création d'une demande** — `POST /me/leave-requests`

- `leaveTypeId` (obligatoire)
- `startDate`, `startHalfDay` (`AM`/`PM`)
- `endDate`, `endHalfDay` (`AM`/`PM`)
- `comment` (facultatif, 1 000 caractères maximum)

**Décision** — `POST /manager/leave-requests/{id}/approve`

- `comment` (facultatif, 1 000 caractères maximum)
- `overlapAcknowledged` (booléen, obligatoire si le signal est actif)

**Refus** — `POST /manager/leave-requests/{id}/reject`

- `comment` (obligatoire, non vide, 1 000 caractères maximum)

**Ajustement de solde** — `POST /admin/users/{userId}/balance-adjustments`

- `leaveTypeId`
- `year`
- `daysDelta` (multiple de 0,5, positif ou négatif, non nul)
- `reason` (obligatoire, 1 000 caractères maximum)

**Création d'utilisateur** — `POST /admin/users`

- `email`, `firstName`, `lastName`
- `teamId`
- `managerId` (facultatif à la création, mais obligatoire pour pouvoir soumettre)
- `roles` (au moins `EMPLOYE`)

La création ne retourne aucun secret. L'Admin RH utilise la réinitialisation de mot de passe pour générer un mot de passe temporaire, transmis par un canal hors application ; l'utilisateur doit le changer à la première connexion.

**Création de type** — `POST /admin/leave-types`

- `code` (unique, immuable)
- `label`, `color`
- `active` (booléen)

### 8.4 Codes et erreurs

| Code HTTP | Usage |
|---|---|
| `200` | Lecture ou action réussie. |
| `201` | Création réussie, avec `Location`. |
| `204` | Action réussie sans contenu. |
| `400` | Requête mal formée. |
| `401` | Authentification absente, invalide ou expirée. |
| `403` | Rôle authentifié mais action interdite. |
| `404` | Ressource inexistante ou hors périmètre. |
| `409` | Conflit d'état : chevauchement personnel, solde insuffisant à l'approbation, acquittement requis, double décision, transition invalide, absence de manager. |
| `422` | Validation métier ou de forme : dates invalides, type inactif, commentaire obligatoire manquant, solde insuffisant à la soumission, ajustement invalide. |
| `429` | Limitation de débit. |
| `500` | Erreur interne, sans détail sensible. |

Codes métier attendus : `PERSONAL_OVERLAP`, `NO_APPROVER`, `INSUFFICIENT_BALANCE`, `INSUFFICIENT_BALANCE_AT_APPROVAL`, `OVERLAP_ACK_REQUIRED`, `INVALID_STATE`, `INVALID_DATES`, `INACTIVE_LEAVE_TYPE`, `COMMENT_REQUIRED`, `BALANCE_WOULD_BE_NEGATIVE`, `LAST_HR_ADMIN`.

### 8.5 Notes d'interface

- Le détail d'une demande côté manager expose le signal courant, son détail et son horodatage d'évaluation.
- Le calendrier accepte une période maximale d'un an ; une période plus longue est refusée en `422`.
- Les endpoints d'administration ne sont pas accessibles aux rôles Manager ou Employé, même en lecture.
- Aucun endpoint ne permet de modifier une demande, de supprimer un historique ou de changer un statut hors transitions.
- Aucun endpoint public n'expose les notifications ou l'audit.

## 9. Risques et points ouverts (liste explicite des ambiguïtés du brief et des hypothèses retenues)

### 9.1 Ambiguïtés du brief et hypothèses retenues

Les hypothèses ci-dessous sont retenues pour permettre une implémentation sans réunion de cadrage. Elles doivent être validées par le commanditaire. Toute modification d'une hypothèse impacte les règles citées.

| ID | Ambiguïté du brief | Hypothèse retenue pour la v1 | Règles impactées |
|---|---|---|---|
| H-01 | Périmètre de l'entreprise | Une seule société, un seul tenant, une seule langue. Pas de multi-société. | Toutes |
| H-02 | Existant et migration | Aucun outil existant n'est supposé ni migré. Les soldes initiaux sont saisis manuellement. | RB-04, RB-19 |
| H-03 | Année de congé et fuseau | Année civile du 1er janvier au 31 décembre, en `Europe/Paris`. | RB-04, RB-06 |
| H-04 | Jours fériés | Non gérés en v1 ; un jour férié tombant un jour ouvré est compté comme jour ouvré. | RB-06, RB-12 |
| H-05 | Granularité et bornes | Gestion à la demi-journée (`AM`/`PM`), pas à l'heure. Les dates de début et de fin doivent être des jours ouvrés. Une demande peut traverser une ou plusieurs fins d'année et est alors répartie entre les années concernées. | RB-06 |
| H-06 | Types et maladie | Les quatre types par défaut sont créés. Tous, y compris Maladie, consomment un solde. Aucun justificatif ni circuit spécial. | RB-03, RB-04 |
| H-07 | Acquisition et report | Pas d'acquisition automatique, de prorata ni de report automatique. Les reports sont des ajustements manuels. | RB-04, RB-19 |
| H-08 | Solde négatif | Interdit dans tous les cas. | RB-05, RB-10, RB-22 |
| H-09 | Rôles | Employé est la base de tous les comptes. Manager et Admin RH sont additifs. L'Admin RH n'approuve pas au titre de son rôle RH. | RB-01, RB-02, RB-09 |
| H-10 | Équipe et approbation | Un employé a une équipe et un manager direct. L'approbation suit le manager direct ; le calendrier et le chevauchement suivent l'équipe. L'organisation nominale aligne les deux. Une équipe peut être désactivée sans être supprimée : ses membres restent rattachés et consultables. | RB-02, RB-12, RB-17, RB-20 |
| H-11 | Absence ou délégation du manager | Aucune délégation automatique. L'Admin RH réaffecte le manager si nécessaire. | RB-02 |
| H-12 | Demande du manager | Le manager ne s'auto-approuve jamais ; sa demande est traitée par son propre manager. | RB-02, RB-09 |
| H-13 | Absence de manager | La soumission est bloquée tant qu'aucun manager direct actif n'est désigné. | RB-02, RB-07 |
| H-14 | Brouillon et modification | Pas de brouillon, pas de modification après soumission ; annulation puis nouvelle demande. | RB-07, RB-15 |
| H-15 | Dates passées | Une demande doit commencer aujourd'hui ou plus tard. Aucune saisie rétroactive, même pour la maladie. Aucune durée maximale. | RB-07 |
| H-16 | Annulation | Une demande en attente peut être annulée par son titulaire ; un congé approuvé futur aussi. Un congé commencé ou passé ne peut plus être annulé par l'employé. | RB-14 |
| H-17 | Désactivation | La désactivation annule automatiquement les demandes en attente et les congés futurs approuvés, avec restauration des jours. | RB-21 |
| H-18 | Règle des 50 % | Calcul par demi-journée, demande incluse, sur les absences approuvées des autres membres actifs, tous types confondus : `(A + 1) / N > 0,50`. Seuil strict. Les demandes en attente ne comptent pas. Une équipe d'un seul membre ne produit pas de signal. | RB-12 |
| H-19 | Effet du signal | Le signal est consultatif et recalculé avant décision. Il exige un acquittement explicite du manager avant approbation, mais ne bloque pas définitivement. | RB-13 |
| H-20 | Réservation de solde | Les demandes en attente ne réservent pas de solde. Le contrôle définitif a lieu à l'approbation. | RB-05, RB-10 |
| H-21 | Contenu et fiabilité des emails | Deux emails obligatoires seulement, contenu minimal sans type ni commentaire, envoi asynchrone avec tentatives. | RB-16 |
| H-22 | Visibilité | L'employé voit ses données ; le manager voit les demandes, soldes, types et commentaires de ses managés ; l'Admin RH voit tout. Le calendrier des pairs masque le type et les commentaires. | RB-17, RB-20 |
| H-23 | Calendrier | Vue par mois ou période d'un an maximum, équipe courante, absences approuvées uniquement. | RB-20 |
| H-24 | Changement d'équipe ou de manager | Les demandes en attente suivent les relations courantes ; aucun instantané de l'ancien manager n'est utilisé. | RB-02, RB-21 |
| H-25 | Conservation | 5 ans pour les congés et l'audit, 12 mois pour les emails et journaux techniques, sous réserve de validation DPO. | NFR 7.5 |
| H-26 | Authentification | Pas d'auto-inscription ni de mot de passe oublié par email. L'Admin RH crée les comptes et réinitialise les mots de passe. Mot de passe de 12 caractères minimum. Jetons courts avec rafraîchissement. | RB-01, NFR 7.2 |
| H-27 | Performance et disponibilité | Cibles définies pour 50 utilisateurs et 20 simultanés, avec 99,5 % en heures ouvrées. | NFR 7.1, 7.3 |
| H-28 | Langue et locale | Interface en français, fuseau `Europe/Paris`, formats locaux français. | Toutes |
| H-29 | Temps partiel | Non modélisé : tous les employés sont supposés à temps plein. | RB-06, RB-12 |
| H-30 | Décision après le début | Une demande en attente peut être décidée même après sa date de début ; pas d'expiration automatique. | RB-09 |
| H-31 | Type inactif | Un type inactif n'accepte plus de nouvelles demandes ; l'historique et les soldes sont conservés. | RB-03 |
| H-32 | Commentaires | Commentaire employé facultatif ; commentaire d'approbation facultatif ; commentaire de refus obligatoire ; maximum 1 000 caractères. | RB-07, RB-09, RB-11 |
| H-33 | API | Versionnement `/api/v1`, JSON `camelCase`, pagination, erreurs `problem+json`, dates ISO, `404` pour une ressource hors périmètre et `403` pour un rôle insuffisant. | Section 8 |
| H-34 | Export et reporting | Aucun export ni rapport avancé en v1 ; seules les listes consultables sont fournies. | Périmètre |
| H-35 | Actions groupées | Aucune action groupée en v1. | RB-07, RB-09 |
| H-36 | Admin RH et audit | L'Admin RH peut consulter tout l'audit, mais ne peut pas le modifier. | RB-17, RB-18 |
| H-37 | Suppression | Aucune suppression physique d'historique via l'application ; désactivation uniquement. La purge ou l'anonymisation à l'échéance de conservation suit une procédure administrative encadrée. | RB-15, RB-21 |
| H-38 | Annulation après début | Aucune annulation après le début, même par l'Admin RH ; seul un ajustement de solde motivé est possible. | RB-14, RB-19 |

**Justification de l'interprétation H-18.** Le brief dit « une demande qui chevauche plus de 50 % d'absences déjà approuvées sur l'équipe ». Une lecture littérale consisterait à comparer le nombre d'absents déjà approuvés à l'effectif, sans compter la demande. Cette lecture ne signale pas une équipe de 6 dont 3 membres sont déjà absents et qui passerait à 4 absents avec la demande, alors que le chevauchement devient critique. La lecture retenue mesure donc le taux d'absence **projeté après approbation**, sur chaque demi-journée, et conserve le détail des absences déjà approuvées. Une autre lecture possible — « jours de la demande qui chevauchent des jours déjà approuvés / jours demandés » — a été écartée car elle ne reflète pas la capacité de l'équipe. Ce point doit être validé en priorité (PO-01).

### 9.2 Risques

| ID | Risque | Impact | Mesure de réduction |
|---|---|---|---|
| R-01 | La formule du seuil de 50 % ne correspond pas à l'intention réelle du métier. | Alertes trop fréquentes ou silencieuses, perte de confiance. | Valider H-18 avec des exemples réels avant l'étape 2 ; afficher le détail du calcul au manager. |
| R-02 | Les demandes en attente ne réservent pas de solde. | Deux demandes valides à la soumission peuvent dépasser le solde ; la seconde échoue à l'approbation. | Contrôle à l'approbation, affichage du solde au manager, ajustement RH. Une réservation pourra être étudiée en v2. |
| R-03 | Les jours fériés sont comptés comme ouvrés. | Surconsommation de jours et calendrier inexact. | Hypothèse explicite H-04 ; prévoir un paramétrage des jours fériés en v2 et des ajustements RH en attendant. |
| R-04 | Le type Maladie révèle une donnée de santé. | Risque RGPD et atteinte à la vie privée. | Accès restreint, absence de type dans les emails et le calendrier des pairs, pas de justificatif, durée de conservation validée. |
| R-05 | Les emails peuvent ne pas être délivrés. | Manager ou employé non informé. | File d'attente, tentatives, trace d'échec, information disponible dans l'application ; la décision ne dépend pas de l'email. |
| R-06 | Absence de délégation de management. | Demandes bloquées pendant les congés du manager. | Réaffectation rapide par l'Admin RH ; délégation à étudier en v2. |
| R-07 | Changement d'équipe ou de manager pendant un traitement. | Décideur ou périmètre de chevauchement différent de celui attendu. | Règles H-10 et H-24, audit des changements, communication RH. |
| R-08 | Saisie manuelle des soldes. | Erreurs d'attribution, écarts avec la paie. | Ajustements motivés, audit, contrôle par l'Admin RH, formation. |
| R-09 | Adoption insuffisante. | Retour aux fichiers et emails, perte de la source unique. | Interface simple en français, notification automatique, calendrier utile, accompagnement au démarrage. |
| R-10 | Durées de conservation non validées juridiquement. | Non-conformité RGPD. | Validation DPO avant mise en production (PO-05). |
| R-11 | Désactivation automatique des congés futurs. | Surprise pour l'employé qui quitte l'entreprise. | Règle explicite H-17, audit et information RH ; aucun email automatique. |
| R-12 | Approbations concurrentes. | Double décompte ou solde négatif. | Transactions atomiques, contrôle dans la même unité de travail, `409` sur conflit (RB-22). |
| R-13 | Demandes à cheval sur deux années. | Contrôle de solde incomplet ou répartition erronée. | Répartition annuelle explicite, contrôle des deux soldes, mouvements séparés. |
| R-14 | Périmètre qui s'élargit en cours de route. | Retard et complexité. | Liste d'exclusions explicite, revue de périmètre à chaque étape. |
| R-15 | Informations de santé dans les journaux ou emails. | Fuite de données sensibles. | Contenu minimal, journaux sans type ni commentaire, accès restreint. |

### 9.3 Points ouverts à trancher avant les étapes 2 et 3

| ID | Point ouvert | Décision attendue |
|---|---|---|
| PO-01 | Définition exacte du seuil de 50 % | Valider H-18 ou choisir une autre formule (absences existantes seules, jours chevauchés, seuil inclusif). |
| PO-02 | Jours fériés et fermetures collectives | Confirmer l'exclusion en v1 ou ajouter un calendrier paramétrable. |
| PO-03 | Maladie | Confirmer qu'elle consomme un solde, qu'aucun justificatif n'est demandé et qu'aucune saisie rétroactive n'est possible. |
| PO-04 | Délégation et suppléance | Confirmer l'absence de délégation en v1 et le processus de réaffectation par l'Admin RH. |
| PO-05 | Durées de conservation | Validation DPO/juridique des 5 ans et 12 mois proposés. |
| PO-06 | Acquisition, prorata et report | Confirmer l'absence d'automatisation et le recours aux ajustements manuels. |
| PO-07 | Temps partiel | Confirmer que tous les employés sont à temps plein en v1. |
| PO-08 | Annulation d'un congé commencé | Confirmer l'interdiction et le traitement par ajustement de solde. |
| PO-09 | Manager d'équipe ou manager direct | Confirmer que l'approbation suit le manager direct et que le calendrier suit l'équipe. |
| PO-10 | Demande en attente et chevauchement | Confirmer que les demandes en attente ne sont pas comptées dans le seuil de 50 % ni réservées. |
| PO-11 | Export et reporting | Confirmer l'exclusion de la v1. |
| PO-12 | SSO / annuaire | Confirmer l'authentification locale et la création manuelle des comptes en v1. |
| PO-13 | Contenu des emails | Confirmer l'absence de type, de commentaire et de détail médical dans les emails. |
| PO-14 | Rôle Admin RH et approbation | Confirmer que l'Admin RH n'approuve pas hors rôle manager. |
| PO-15 | Suppression et anonymisation | Confirmer la conservation de l'historique et le processus d'anonymisation après départ. |
| PO-16 | Base légale de la donnée de santé | Documenter avec le DPO la condition de l'article 9.2 du RGPD applicable au type Maladie. |

---

**Fin de la spécification.** Les éléments d'architecture, de conception système, de modèle physique et d'implémentation sont traités dans les étapes 2 à 4.
