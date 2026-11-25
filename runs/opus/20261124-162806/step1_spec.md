# TeamLeave — Spécification technico-fonctionnelle

**Version** : 1.0 (étape 1/4)
**Destinataires** : équipe de développement backend (ASP.NET Core) et frontend (React + TypeScript), QA, référent RH.
**Conventions de référencement** :

| Préfixe | Objet |
|---|---|
| `US-xx` | User story |
| `CA-xx.y` | Critère d'acceptation *y* de la story *xx* |
| `RB-xx` | Règle métier |
| `ENF-xx` | Exigence non fonctionnelle |
| `A-xx` | Ambiguïté du brief et hypothèse retenue |
| `R-xx` | Risque |
| `PO-xx` | Point ouvert à trancher |

Les termes **DOIT**, **NE DOIT PAS** et **PEUT** sont normatifs.

---

## 1. Contexte et objectifs

### 1.1 Contexte

L'entreprise est une PME d'environ 50 salariés. Elle ne dispose pas d'outil dédié à la gestion des absences. Selon l'hypothèse A-01, la gestion actuelle repose sur des échanges par email et des tableurs. Cette situation pose quatre problèmes :

- les soldes ne sont pas fiables ;
- la validation des demandes n'est pas tracée ;
- personne n'a de vision consolidée des absences d'une équipe ;
- des sous-effectifs critiques apparaissent sans être anticipés.

TeamLeave est une application web interne qui couvre le cycle complet d'une demande d'absence : saisie, contrôle, validation, notification, décompte du solde et visualisation en calendrier d'équipe.

Le présent document est l'entrée des étapes suivantes : architecture (étape 2), backend (étape 3) et frontend (étape 4). Il est conçu pour qu'une équipe puisse démarrer sans réunion de cadrage. Toute zone d'incertitude est explicitée en section 9.

### 1.2 Objectifs

| ID | Objectif | Indicateur de réussite |
|---|---|---|
| O-1 | Dématérialiser 100 % des demandes d'absence | Aucune demande traitée hors outil après la mise en service |
| O-2 | Fiabiliser les soldes | Chaque variation de solde est tracée par un mouvement horodaté et attribué (RB-31) |
| O-3 | Accélérer la décision | Le valideur est notifié par email au plus tard 5 minutes après la soumission (RB-23, RB-26) |
| O-4 | Prévenir les sous-effectifs | Toute demande dépassant le seuil de chevauchement est signalée avant décision (RB-20, RB-21) |
| O-5 | Assurer la traçabilité et la conformité | Toute action sensible est journalisée (RB-34) et la politique RGPD est respectée (ENF-RGPD) |

### 1.3 Contraintes techniques imposées (rappel)

- **Backend** : ASP.NET Core (dernière LTS), EF Core, base SQL Server ou PostgreSQL. Le choix est justifié à l'étape 2 (PO-01).
- **Frontend** : React + TypeScript, outillé avec Vite.
- **Tests backend** : xUnit.
- **Authentification** : JWT porteur des rôles.
- **Livrable** : le code compile et les tests passent.

### 1.4 Glossaire

| Terme | Définition |
|---|---|
| **Demande** | Demande d'absence d'un employé sur une période continue [date de début ; date de fin], bornes incluses. |
| **Jour ouvré** | Du lundi au vendredi, hors jour férié enregistré (RB-03, RB-27). |
| **Type de congé** | Catégorie d'absence paramétrable : CP, RTT, Maladie, Autre, etc. (RB-30). |
| **Type décompté** | Type dont les jours sont déduits d'un solde. Attribut « décompte solde » = vrai. |
| **Solde** | Nombre de jours disponibles pour un couple (employé, type décompté). |
| **Solde disponible** | Solde diminué des jours des demandes en attente du même type (RB-09). |
| **Équipe** | Groupe d'employés. Chaque employé actif appartient à exactement une équipe (RB-15). |
| **Manager d'équipe** | Utilisateur désigné responsable d'une équipe. Il valide les demandes des membres de cette équipe. |
| **Valideur** | Utilisateur habilité à décider d'une demande donnée (RB-16). |
| **Taux de chevauchement** | Pour un jour donné, part des autres membres de l'équipe déjà en absence approuvée (RB-20). |
| **Demande signalée** | Demande dont au moins un jour dépasse le seuil de chevauchement (RB-20). |
| **Aujourd'hui** | Date courante dans le fuseau Europe/Paris (RB-01). |

---

## 2. Périmètre (inclus / exclu)

### 2.1 Inclus (V1)

| # | Fonctionnalité | Références |
|---|---|---|
| I-01 | Authentification par email et mot de passe, JWT avec rôles, rafraîchissement de session, changement de mot de passe | US-01, US-02 |
| I-02 | Consultation par l'employé de ses soldes et de l'historique des mouvements | US-03 |
| I-03 | Prévisualisation d'une demande (nombre de jours, contrôle de solde, signalement) puis soumission | US-04 |
| I-04 | Consultation et annulation de ses demandes | US-05, US-06 |
| I-05 | Calendrier d'équipe des absences approuvées, avec vue liste accessible | US-07 |
| I-06 | File de demandes à valider avec indicateur de chevauchement | US-09, US-10 |
| I-07 | Approbation (avec acquittement si signalement) et refus (commentaire obligatoire) | US-11, US-12 |
| I-08 | Notifications email : soumission (valideur), décision (employé), annulation (valideur) | US-08, US-13, RB-25 |
| I-09 | Administration RH : types de congés, soldes, utilisateurs, équipes, rôles, jours fériés | US-14 à US-17 |
| I-10 | Journal d'audit consultable par l'admin RH | US-18 |
| I-11 | Annulation d'une demande par l'admin RH | US-06 |

### 2.2 Exclu (V1)

| # | Hors périmètre | Justification / renvoi |
|---|---|---|
| E-01 | Calcul automatique des acquisitions (CP mensuels, RTT), report et expiration des soldes, gestion par période de référence | A-06 |
| E-02 | Demi-journées et absences horaires | A-04 |
| E-03 | Temps partiel et calendriers de travail individualisés | A-05 |
| E-04 | Dépôt de justificatifs (arrêts maladie) et traitement des données médicales | A-08, ENF-RGPD |
| E-05 | Export et interface vers la paie | Non demandé |
| E-06 | SSO (Entra ID / Google) et réinitialisation du mot de passe en libre-service par email | A-12 |
| E-07 | Workflow de validation multi-niveaux et délégation de validation (intérim) | A-10 |
| E-08 | Modification d'une demande existante (le parcours est : annuler puis resoumettre) | A-13 |
| E-09 | Saisie d'une demande pour le compte d'un autre salarié | A-14 |
| E-10 | Notifications in-app/push, rappels et relances automatiques | Non demandé |
| E-11 | Synchronisation avec les agendas (Outlook, Google, iCal) | Non demandé |
| E-12 | Multilinguisme (interface en français uniquement), multi-société, multi-pays | A-15 |
| E-13 | Application mobile native (le web responsive est inclus, voir ENF-ACC) | — |
| E-14 | Paramétrage du seuil de chevauchement depuis l'interface (il reste paramétrable par configuration) | RB-20 |

---

## 3. Acteurs et rôles (matrice des permissions)

### 3.1 Acteurs

| Acteur | Description |
|---|---|
| **Employé** | Tout utilisateur authentifié. Il gère ses propres absences. Ce rôle est implicite pour tout compte actif. |
| **Manager** | Employé disposant du rôle `Manager`. Il valide les demandes des équipes qu'il manage (RB-16) et peut manager plusieurs équipes. |
| **Admin RH** | Employé disposant du rôle `AdminRH`. Il paramètre types, soldes, comptes, équipes et jours fériés. Il est valideur de repli (RB-16). |
| **Système** | Traitements automatiques : calculs, envoi des emails, journalisation. |
| **Service SMTP** | Acteur externe d'envoi des emails (PO-03). |

Les rôles sont **cumulables** : un utilisateur peut être à la fois Manager et Admin RH. Un Manager et un Admin RH restent des employés pour leurs propres absences.

### 3.2 Matrice des permissions

Légende :
- ✅ autorisé ;
- 🔸 autorisé sous condition de périmètre (voir colonne « Condition ») ;
- ❌ interdit.

| # | Action | Employé | Manager | Admin RH | Condition / règle |
|---|---|---|---|---|---|
| P-01 | Se connecter, rafraîchir la session, se déconnecter | ✅ | ✅ | ✅ | Compte actif (RB-32) |
| P-02 | Changer son mot de passe | ✅ | ✅ | ✅ | ENF-SEC-04 |
| P-03 | Consulter ses soldes et mouvements | ✅ | ✅ | ✅ | — |
| P-04 | Prévisualiser et soumettre une demande pour soi | ✅ | ✅ | ✅ | RB-01 à RB-09 |
| P-05 | Soumettre une demande pour autrui | ❌ | ❌ | ❌ | A-14 |
| P-06 | Consulter ses demandes | ✅ | ✅ | ✅ | — |
| P-07 | Annuler sa propre demande | 🔸 | 🔸 | 🔸 | RB-13 |
| P-08 | Annuler la demande d'autrui | ❌ | ❌ | ✅ | RB-13, motif obligatoire |
| P-09 | Calendrier de sa propre équipe | ✅ | ✅ | ✅ | RB-29 |
| P-10 | Calendrier d'une autre équipe | ❌ | 🔸 | ✅ | Manager : équipes qu'il manage uniquement |
| P-11 | Voir le **type** des absences d'autrui | ❌ | 🔸 | ✅ | Manager : membres des équipes qu'il manage (RB-29) |
| P-12 | Consulter les demandes d'un autre utilisateur | ❌ | 🔸 | ✅ | RB-28 |
| P-13 | Lister les demandes à valider | ❌ | 🔸 | 🔸 | Uniquement celles dont il est valideur (RB-16) |
| P-14 | Consulter l'analyse de chevauchement détaillée (noms des absents) | ❌ | 🔸 | 🔸 | Valideur de la demande ou Admin RH (RB-21) |
| P-15 | Approuver / refuser une demande | ❌ | 🔸 | 🔸 | Valideur au sens de RB-16, jamais pour soi (RB-17) |
| P-16 | Gérer les types de congés | ❌ | ❌ | ✅ | RB-30 |
| P-17 | Ajuster les soldes | ❌ | ❌ | 🔸 | Pas sur ses propres soldes (RB-31) |
| P-18 | Gérer comptes, rôles et équipes | ❌ | ❌ | ✅ | RB-15, RB-32, RB-33 |
| P-19 | Gérer les jours fériés | ❌ | ❌ | ✅ | RB-27 |
| P-20 | Consulter le journal d'audit | ❌ | ❌ | ✅ | RB-34 |

**Principe de contrôle d'accès.** Toute permission est vérifiée **côté serveur**, par rôle **et** par périmètre (ressource), à chaque requête. Le frontend ne fait que masquer les actions non autorisées. Il ne constitue jamais un contrôle.

---

## 4. User stories numérotées, chacune avec critères d'acceptation testables (format Given/When/Then)

### 4.0 Jeu de données de référence des critères

Sauf mention contraire, les critères s'appuient sur le contexte suivant. Les tests DOIVENT injecter une horloge contrôlable.

**Horloge et calendrier**

- Aujourd'hui = **lundi 2026-03-02** (Europe/Paris).
- Jours fériés enregistrés : 2026-04-06, 2026-05-01, 2026-05-08, 2026-05-14.
- Seuil de chevauchement = 0,50 (RB-20).

**Types de congés**

| Code | Décompte solde | Rétroactif autorisé |
|---|---|---|
| CP | oui | non |
| RTT | oui | non |
| MALADIE | non | oui |
| AUTRE | non | non |

**Équipe « Produit »**

- Manager : **Marie**. Elle n'est pas membre de « Produit » ; elle appartient à l'équipe « Direction ».
- Membres : **Eric**, Alice, Bruno, Chloé, David.
- Admins RH actifs : **Hélène** et Hugo.
- Soldes d'Eric : CP = 10, RTT = 3, aucune demande en attente.

### Authentification

#### US-01 — Se connecter
*En tant qu'utilisateur, je veux m'authentifier avec mon email et mon mot de passe afin d'accéder aux fonctions de mes rôles.*
Règles : RB-32, ENF-SEC-01 à ENF-SEC-05.

- **CA-01.1**
  - *Given* un compte actif `eric@acme.fr` avec un mot de passe valide,
  - *When* il soumet ses identifiants,
  - *Then* il reçoit un jeton d'accès JWT valable 30 min, contenant son identifiant, son email et ses rôles, et un jeton de rafraîchissement est déposé en cookie `HttpOnly; Secure; SameSite=Strict`.
- **CA-01.2**
  - *Given* un mot de passe erroné,
  - *When* il soumet ses identifiants,
  - *Then* la réponse est 401 avec un message générique qui ne révèle pas si l'email existe.
- **CA-01.3**
  - *Given* 5 échecs consécutifs sur le même compte en moins de 15 min,
  - *When* une 6ᵉ tentative a lieu, même avec le bon mot de passe,
  - *Then* la réponse est 401 et le compte est verrouillé 15 min. L'événement est journalisé.
- **CA-01.4**
  - *Given* un compte désactivé,
  - *When* il soumet des identifiants corrects,
  - *Then* la réponse est 401.
- **CA-01.5**
  - *Given* un jeton d'accès expiré et un jeton de rafraîchissement valide,
  - *When* le client appelle le rafraîchissement,
  - *Then* un nouveau couple de jetons est émis et l'ancien jeton de rafraîchissement est révoqué (rotation).
- **CA-01.6**
  - *Given* un jeton de rafraîchissement déjà utilisé,
  - *When* il est présenté de nouveau,
  - *Then* la réponse est 401 et tous les jetons de rafraîchissement de l'utilisateur sont révoqués (détection de rejeu).
- **CA-01.7**
  - *Given* un utilisateur connecté,
  - *When* il se déconnecte,
  - *Then* son jeton de rafraîchissement est révoqué et toute tentative de rafraîchissement ultérieure renvoie 401.

#### US-02 — Changer mon mot de passe
*En tant qu'utilisateur, je veux changer mon mot de passe afin de sécuriser mon compte.*
Règles : ENF-SEC-04, RB-34.

- **CA-02.1**
  - *Given* un utilisateur connecté,
  - *When* il fournit son mot de passe actuel correct et un nouveau mot de passe conforme à ENF-SEC-04,
  - *Then* le mot de passe est changé, tous ses jetons de rafraîchissement sont révoqués et l'action est journalisée.
- **CA-02.2**
  - *Given* un mot de passe actuel incorrect,
  - *When* il tente le changement,
  - *Then* la réponse est 422 `INVALID_CURRENT_PASSWORD` et le mot de passe est inchangé.
- **CA-02.3**
  - *Given* un nouveau mot de passe de 8 caractères,
  - *When* il tente le changement,
  - *Then* la réponse est 400 `VALIDATION_ERROR` sur le champ concerné.

### Employé

#### US-03 — Consulter mes soldes
*En tant qu'employé, je veux voir mes soldes par type et leur historique afin de planifier mes absences.*
Règles : RB-09, RB-30, RB-31.

- **CA-03.1**
  - *Given* Eric (CP = 10) avec une demande CP en attente de 2 jours,
  - *When* il consulte ses soldes,
  - *Then* il voit pour CP : solde = 10, en attente = 2, disponible = 8.
- **CA-03.2**
  - *Given* les types MALADIE et AUTRE, non décomptés,
  - *When* Eric consulte ses soldes,
  - *Then* ces types n'apparaissent pas dans la liste des soldes.
- **CA-03.3**
  - *Given* un type CP désactivé par la RH,
  - *When* Eric consulte ses soldes,
  - *Then* le solde CP reste affiché avec la mention « type inactif ».
- **CA-03.4**
  - *Given* un historique de mouvements (initialisation, débits, crédits, ajustements),
  - *When* Eric consulte l'historique d'un type,
  - *Then* les mouvements sont listés du plus récent au plus ancien, avec date, nature, montant signé, solde après mouvement, motif et demande liée le cas échéant.

#### US-04 — Prévisualiser et soumettre une demande
*En tant qu'employé, je veux saisir une période et un type, voir le décompte calculé puis soumettre ma demande afin d'obtenir une validation.*
Règles : RB-01 à RB-09, RB-12, RB-16, RB-20, RB-21, RB-22, RB-23.

- **CA-04.1**
  - *Given* Eric avec CP = 10,
  - *When* il soumet CP du 2026-03-09 au 2026-03-13,
  - *Then*
    - la demande est créée avec le statut `EnAttente` et `nbJours = 5` ;
    - le solde CP reste 10 et le disponible passe à 5 ;
    - une notification est mise en file pour Marie (RB-23).
- **CA-04.2**
  - *Given* le 2026-05-01 est férié,
  - *When* Eric soumet RTT du 2026-04-27 au 2026-05-01 avec RTT = 3,
  - *Then* `nbJours = 4` et la demande est refusée en 422 `INSUFFICIENT_BALANCE` (4 > 3).
- **CA-04.3**
  - *Given* Eric avec CP = 10,
  - *When* il prévisualise CP du 2026-04-27 au 2026-05-01,
  - *Then*
    - la réponse indique `nbJours = 4` et `soldeSuffisant = true` ;
    - elle indique `signalee` (vrai/faux) **sans** nom de collègue ;
    - **aucune** demande n'est créée.
- **CA-04.4**
  - *Given* une période du samedi 2026-03-07 au dimanche 2026-03-08,
  - *When* Eric soumet,
  - *Then* la réponse est 422 `NO_WORKING_DAYS`.
- **CA-04.5**
  - *Given* date de début = 2026-03-13 et date de fin = 2026-03-09,
  - *When* Eric soumet,
  - *Then* la réponse est 400 `VALIDATION_ERROR`.
- **CA-04.6**
  - *Given* le type CP (rétroactif non autorisé),
  - *When* Eric soumet du 2026-02-27 au 2026-02-27,
  - *Then* la réponse est 422 `PAST_DATE_NOT_ALLOWED`.
- **CA-04.7**
  - *Given* le type MALADIE (rétroactif autorisé),
  - *When* Eric soumet du 2026-02-23 au 2026-02-24,
  - *Then* la demande est créée en `EnAttente` avec `nbJours = 2` et aucun solde n'est contrôlé.
- **CA-04.8**
  - *Given* le type MALADIE,
  - *When* Eric soumet avec une date de début au 2026-01-26 (antérieure à aujourd'hui − 30 jours),
  - *Then* la réponse est 422 `PAST_DATE_NOT_ALLOWED`.
- **CA-04.9**
  - *Given* aujourd'hui + 365 jours = 2027-03-02,
  - *When* Eric soumet avec une date de fin au 2027-03-03,
  - *Then* la réponse est 422 `DATE_BEYOND_HORIZON`.
- **CA-04.10**
  - *Given* Eric a une demande `EnAttente` ou `Approuvée` du 2026-03-09 au 2026-03-13,
  - *When* il soumet du 2026-03-13 au 2026-03-16,
  - *Then* la réponse est 422 `OVERLAPPING_OWN_REQUEST`.
- **CA-04.11**
  - *Given* Eric a une demande `Refusée` ou `Annulée` du 2026-03-09 au 2026-03-13,
  - *When* il soumet la même période,
  - *Then* la demande est acceptée.
- **CA-04.12**
  - *Given* un type désactivé,
  - *When* Eric soumet avec ce type,
  - *Then* la réponse est 422 `LEAVE_TYPE_INACTIVE`.
- **CA-04.13**
  - *Given* CP = 10 et une demande CP en attente de 8 jours,
  - *When* Eric soumet une demande CP de 3 jours,
  - *Then* la réponse est 422 `INSUFFICIENT_BALANCE` (disponible = 2).
- **CA-04.14**
  - *Given* Alice, Bruno et Chloé ont des absences approuvées couvrant le 2026-03-10,
  - *When* Eric soumet du 2026-03-09 au 2026-03-13,
  - *Then*
    - la demande est créée ;
    - l'instantané de signalement à la soumission vaut vrai, avec un taux maximal de 0,75 ;
    - l'email à Marie mentionne le signalement (RB-21, RB-23).
- **CA-04.15**
  - *Given* l'équipe de Marie (Direction) n'a pas de manager,
  - *When* Marie soumet une demande,
  - *Then* la demande est créée et la notification est envoyée à Hélène et Hugo (RB-16).

#### US-05 — Consulter mes demandes
*En tant qu'employé, je veux voir mes demandes et leur statut afin de suivre leur traitement.*
Règles : RB-12, RB-28.

- **CA-05.1**
  - *Given* Eric a des demandes dans les 4 statuts,
  - *When* il consulte sa liste sans filtre,
  - *Then*
    - toutes sont listées par date de début décroissante, paginées par 20 ;
    - chacune affiche type, période, nombre de jours, statut, date de soumission et, si décidée, décideur, date et commentaire.
- **CA-05.2**
  - *Given* le filtre `statut = EnAttente`,
  - *When* Eric consulte sa liste,
  - *Then* seules ses demandes en attente sont renvoyées.
- **CA-05.3**
  - *Given* une demande d'Alice,
  - *When* Eric en demande le détail par identifiant,
  - *Then* la réponse est 404.

#### US-06 — Annuler une demande
*En tant qu'employé, je veux annuler une demande afin de corriger une erreur ou un changement de plan. En tant qu'admin RH, je veux pouvoir annuler toute demande active afin de régulariser une situation.*
Règles : RB-12, RB-13, RB-14, RB-25, RB-34.

- **CA-06.1**
  - *Given* une demande CP `EnAttente` d'Eric,
  - *When* il l'annule,
  - *Then*
    - le statut passe à `Annulée` et le solde CP est inchangé ;
    - le disponible est réaugmenté ;
    - une notification d'annulation est mise en file pour le valideur.
- **CA-06.2**
  - *Given* une demande CP `Approuvée` de 5 jours débutant le 2026-03-09 et CP = 5 après débit,
  - *When* Eric l'annule,
  - *Then*
    - le statut passe à `Annulée` ;
    - CP = 10 ;
    - un mouvement « Crédit annulation » de +5 est créé et lié à la demande.
- **CA-06.3**
  - *Given* une demande `Approuvée` débutant aujourd'hui (2026-03-02),
  - *When* Eric l'annule,
  - *Then* la réponse est 422 `CANCELLATION_NOT_ALLOWED` et rien ne change.
- **CA-06.4**
  - *Given* une demande `Refusée`,
  - *When* Eric l'annule,
  - *Then* la réponse est 409 `INVALID_STATUS_TRANSITION`.
- **CA-06.5**
  - *Given* une demande `Approuvée` d'Eric déjà commencée,
  - *When* Hélène l'annule avec le motif « Retour anticipé »,
  - *Then*
    - le statut passe à `Annulée` ;
    - le solde est recrédité intégralement ;
    - l'action est journalisée avec le motif.
- **CA-06.6**
  - *Given* Hélène annule une demande sans motif,
  - *When* la requête est traitée,
  - *Then* la réponse est 400 `VALIDATION_ERROR`.
- **CA-06.7**
  - *Given* une demande d'Alice,
  - *When* Marie (manager) tente de l'annuler,
  - *Then* la réponse est 403.

#### US-07 — Consulter le calendrier d'équipe
*En tant que membre d'une équipe, je veux voir les absences approuvées de mon équipe afin d'éviter les chevauchements.*
Règles : RB-27, RB-29.

- **CA-07.1**
  - *Given* Alice a une absence MALADIE approuvée et Bruno une absence CP approuvée en mars,
  - *When* Eric consulte le calendrier de « Produit » pour mars 2026,
  - *Then*
    - il voit les deux absences avec nom et période ;
    - le type est remplacé par « Absent » ;
    - ses propres absences affichent leur type.
- **CA-07.2**
  - *Given* la même situation,
  - *When* Marie consulte le calendrier de « Produit »,
  - *Then* elle voit le type de chaque absence.
- **CA-07.3**
  - *Given* Chloé a une demande `EnAttente`,
  - *When* quiconque consulte le calendrier,
  - *Then* cette demande n'apparaît pas.
- **CA-07.4**
  - *Given* une période,
  - *When* le calendrier est affiché,
  - *Then* week-ends et jours fériés sont visuellement distingués, et une vue liste équivalente est disponible (ENF-ACC-03).
- **CA-07.5**
  - *Given* Eric (membre de « Produit »),
  - *When* il demande le calendrier de l'équipe « Direction »,
  - *Then* la réponse est 403.
- **CA-07.6**
  - *Given* une plage demandée de 120 jours,
  - *When* le calendrier est appelé,
  - *Then* la réponse est 400 `RANGE_TOO_LARGE` (maximum 93 jours).

### Manager / valideur

#### US-08 — Être notifié d'une nouvelle demande
*En tant que valideur, je veux recevoir un email à chaque soumission me concernant afin de traiter la demande rapidement.*
Règles : RB-16, RB-21, RB-23, RB-26.

- **CA-08.1**
  - *Given* Eric soumet une demande,
  - *When* la transaction est validée,
  - *Then*
    - un email est mis en file pour Marie ;
    - il contient nom de l'employé, type, période, nombre de jours, commentaire éventuel, indicateur de signalement et lien vers la demande.
- **CA-08.2**
  - *Given* le serveur SMTP est indisponible,
  - *When* Eric soumet,
  - *Then*
    - la demande est créée (201) ;
    - l'email reste en file et est retenté selon RB-26.
- **CA-08.3**
  - *Given* le valideur est déterminé par repli RH,
  - *When* une demande est soumise,
  - *Then* chaque admin RH actif, à l'exception du demandeur, reçoit l'email.

#### US-09 — Lister les demandes à valider
*En tant que valideur, je veux une file de demandes en attente afin de les traiter.*
Règles : RB-16, RB-17, RB-20, RB-21, RB-22.

- **CA-09.1**
  - *Given* 3 demandes `EnAttente` dans « Produit » et 1 dans une autre équipe,
  - *When* Marie consulte sa file,
  - *Then*
    - elle voit exactement les 3 demandes de « Produit », triées par date de début croissante ;
    - chacune porte l'indicateur `signalee` calculé au moment de la consultation.
- **CA-09.2**
  - *Given* Marie a elle-même une demande en attente,
  - *When* elle consulte sa file,
  - *Then* sa propre demande n'y figure pas.
- **CA-09.3**
  - *Given* Eric (sans rôle Manager),
  - *When* il appelle la file,
  - *Then* la réponse est une liste vide.
- **CA-09.4**
  - *Given* une demande de Marie relevant du repli RH,
  - *When* Hélène consulte sa file,
  - *Then* la demande y figure.

#### US-10 — Consulter le détail et l'analyse de chevauchement
*En tant que valideur, je veux voir le détail d'une demande et, s'il y a lieu, les jours critiques afin de décider en connaissance de cause.*
Règles : RB-20, RB-21, RB-22, RB-28.

- **CA-10.1**
  - *Given* la demande d'Eric du 2026-03-09 au 2026-03-13, avec Alice et Bruno absents le 09 et Alice, Bruno et Chloé absents le 10,
  - *When* Marie consulte l'analyse,
  - *Then*
    - `signalee = true` avec un effectif de référence de 4 ;
    - les jours critiques listés se limitent au 10, avec `absents = 3`, `taux = 0,75` et les noms Alice, Bruno, Chloé ;
    - le 09 (taux 0,50) n'est pas critique.
- **CA-10.2**
  - *Given* aucune absence approuvée dans l'équipe sur la période,
  - *When* Marie consulte l'analyse,
  - *Then* `signalee = false` et la liste des jours critiques est vide.
- **CA-10.3**
  - *Given* l'absence de Chloé a été annulée après la soumission d'Eric,
  - *When* Marie consulte l'analyse,
  - *Then*
    - le résultat est recalculé et vaut `signalee = false` ;
    - l'instantané à la soumission reste affiché à titre informatif (RB-22).
- **CA-10.4**
  - *Given* Eric,
  - *When* il appelle l'analyse détaillée de sa propre demande,
  - *Then* la réponse est 403.

#### US-11 — Approuver une demande
*En tant que valideur, je veux approuver une demande afin d'acter l'absence et de décompter le solde.*
Règles : RB-10, RB-11, RB-12, RB-16, RB-17, RB-18, RB-19, RB-21, RB-22, RB-24, RB-34.

- **CA-11.1**
  - *Given* une demande CP `EnAttente` de 5 jours d'Eric (CP = 10), non signalée,
  - *When* Marie l'approuve sans commentaire,
  - *Then*
    - le statut passe à `Approuvée` et CP = 5 ;
    - un mouvement « Débit approbation » de −5 est créé ;
    - décideur, date de décision et instantané de chevauchement sont enregistrés ;
    - un email est mis en file pour Eric.
- **CA-11.2**
  - *Given* une demande signalée au moment de la décision,
  - *When* Marie l'approuve sans `acquittementChevauchement = true`,
  - *Then* la réponse est 422 `OVERLAP_ACK_REQUIRED` et la demande reste `EnAttente`.
- **CA-11.3**
  - *Given* une demande signalée,
  - *When* Marie l'approuve avec `acquittementChevauchement = true`,
  - *Then* la demande est approuvée et l'acquittement est enregistré et journalisé.
- **CA-11.4**
  - *Given* la RH a ajusté CP d'Eric à 3 après la soumission d'une demande de 5 jours,
  - *When* Marie l'approuve,
  - *Then* la réponse est 422 `INSUFFICIENT_BALANCE`, la demande reste `EnAttente` et CP = 3.
- **CA-11.5**
  - *Given* une demande MALADIE `EnAttente`,
  - *When* Marie l'approuve,
  - *Then* aucun mouvement de solde n'est créé.
- **CA-11.6**
  - *Given* une demande déjà `Approuvée` ou `Refusée`,
  - *When* Marie tente de l'approuver,
  - *Then* la réponse est 409 `INVALID_STATUS_TRANSITION`.
- **CA-11.7**
  - *Given* deux approbations concurrentes de la même demande,
  - *When* elles sont traitées,
  - *Then* une seule réussit et l'autre reçoit 409. Le solde n'est débité qu'une fois.
- **CA-11.8**
  - *Given* une demande de Marie relevant du repli RH,
  - *When* Marie tente de l'approuver,
  - *Then* la réponse est 403 `SELF_APPROVAL_FORBIDDEN`.
- **CA-11.9**
  - *Given* une demande d'Eric dont le valideur est Marie,
  - *When* Hélène (Admin RH) tente de l'approuver,
  - *Then* la réponse est 403 `NOT_APPROVER`.

#### US-12 — Refuser une demande
*En tant que valideur, je veux refuser une demande en motivant ma décision afin d'informer l'employé.*
Règles : RB-12, RB-16, RB-17, RB-18, RB-19, RB-24, RB-34.

- **CA-12.1**
  - *Given* une demande `EnAttente`,
  - *When* Marie la refuse avec le commentaire « Période de clôture »,
  - *Then*
    - le statut passe à `Refusée`, le commentaire est enregistré et aucun solde n'est modifié ;
    - un email contenant le commentaire est mis en file pour Eric.
- **CA-12.2**
  - *Given* un commentaire absent, vide ou composé uniquement d'espaces,
  - *When* Marie refuse,
  - *Then* la réponse est 400 `VALIDATION_ERROR` (champ `commentaire`) et la demande reste `EnAttente`.
- **CA-12.3**
  - *Given* un commentaire de 1 001 caractères,
  - *When* Marie refuse,
  - *Then* la réponse est 400 `VALIDATION_ERROR`.
- **CA-12.4**
  - *Given* une demande signalée,
  - *When* Marie la refuse sans acquittement,
  - *Then* le refus est accepté (l'acquittement n'est requis que pour approuver).

#### US-13 — Être notifié de la décision
*En tant qu'employé, je veux être informé par email de la décision afin d'organiser mon absence.*
Règles : RB-24, RB-26.

- **CA-13.1**
  - *Given* une demande approuvée,
  - *When* la transaction est validée,
  - *Then* un email « Demande approuvée » est mis en file pour l'employé, avec type, période, nombre de jours, décideur, commentaire éventuel et solde restant pour un type décompté.
- **CA-13.2**
  - *Given* une demande refusée,
  - *When* la transaction est validée,
  - *Then* un email « Demande refusée » contenant le commentaire obligatoire est mis en file.

### Admin RH

#### US-14 — Gérer les types de congés
*En tant qu'admin RH, je veux créer, modifier et désactiver des types afin d'adapter l'outil à la politique de l'entreprise.*
Règles : RB-30, RB-31, RB-34.

- **CA-14.1**
  - *Given* aucun type de code `FORM`,
  - *When* Hélène crée le type (`FORM`, « Formation », décompte = oui, rétroactif = non),
  - *Then* le type est actif et un solde à 0 est créé pour chaque utilisateur actif (RB-31).
- **CA-14.2**
  - *Given* un type `CP` existant,
  - *When* Hélène crée un type de code `cp`,
  - *Then* la réponse est 409 `DUPLICATE_CODE` (unicité insensible à la casse).
- **CA-14.3**
  - *Given* le type `RTT` utilisé par des demandes,
  - *When* Hélène le désactive,
  - *Then*
    - il n'est plus proposé à la saisie ;
    - les demandes existantes restent inchangées et peuvent toujours être décidées.
- **CA-14.4**
  - *Given* un type existant,
  - *When* Hélène tente d'en modifier le code,
  - *Then* la réponse est 400 (code immuable).
- **CA-14.5**
  - *Given* des demandes `EnAttente` sur un type décompté,
  - *When* Hélène tente de passer l'attribut « décompte solde » de vrai à faux, ou inversement,
  - *Then* la réponse est 409 `LEAVE_TYPE_IN_USE`.

#### US-15 — Gérer les soldes
*En tant qu'admin RH, je veux consulter et ajuster les soldes afin de refléter les droits acquis.*
Règles : RB-11, RB-31, RB-34.

- **CA-15.1**
  - *Given* Eric avec CP = 10,
  - *When* Hélène ajuste de +2,08 avec le motif « Acquisition mars »,
  - *Then* CP = 12,08 et un mouvement « Ajustement RH » est créé avec auteur, motif et solde après mouvement.
- **CA-15.2**
  - *Given* CP = 10,
  - *When* Hélène ajuste de −12,
  - *Then* la réponse est 422 `NEGATIVE_BALANCE` et rien ne change.
- **CA-15.3**
  - *Given* un ajustement sans motif,
  - *When* il est soumis,
  - *Then* la réponse est 400 `VALIDATION_ERROR`.
- **CA-15.4**
  - *Given* Hélène,
  - *When* elle tente d'ajuster son propre solde,
  - *Then* la réponse est 403 `SELF_BALANCE_ADJUSTMENT_FORBIDDEN`.
- **CA-15.5**
  - *Given* un montant à 3 décimales (0,125),
  - *When* il est soumis,
  - *Then* la réponse est 400 (2 décimales maximum).

#### US-16 — Gérer utilisateurs, rôles et équipes
*En tant qu'admin RH, je veux gérer les comptes, leurs rôles et l'organisation en équipes afin que les validations soient routées correctement.*
Règles : RB-15, RB-16, RB-31, RB-32, RB-33, RB-34.

- **CA-16.1**
  - *Given* une équipe existante,
  - *When* Hélène crée le compte de Fanny (email unique, équipe « Produit », mot de passe initial conforme),
  - *Then* le compte est actif avec le rôle Employé implicite, et des soldes à 0 sont créés pour chaque type décompté actif.
- **CA-16.2**
  - *Given* un email déjà utilisé, quelle que soit la casse,
  - *When* Hélène crée un compte,
  - *Then* la réponse est 409 `DUPLICATE_EMAIL`.
- **CA-16.3**
  - *Given* Eric (sans rôle Manager),
  - *When* Hélène le désigne manager d'une équipe,
  - *Then* la réponse est 422 `MANAGER_ROLE_REQUIRED`.
- **CA-16.4**
  - *Given* Alice a 2 demandes `EnAttente` et 1 `Approuvée` future,
  - *When* Hélène désactive le compte d'Alice,
  - *Then*
    - les 2 demandes passent à `Annulée` avec le motif « Compte désactivé » ;
    - la demande approuvée est conservée ;
    - les jetons d'Alice sont révoqués.
- **CA-16.5**
  - *Given* Marie est manager de « Produit »,
  - *When* Hélène désactive Marie,
  - *Then* « Produit » n'a plus de manager et les demandes en attente de l'équipe relèvent du repli RH (RB-16).
- **CA-16.6**
  - *Given* Hélène et Hugo sont les seuls Admins RH actifs et Hugo est désactivé,
  - *When* Hélène tente de retirer son propre rôle AdminRH,
  - *Then* la réponse est 422 `LAST_HR_ADMIN`.
- **CA-16.7**
  - *Given* Eric a une demande `EnAttente`,
  - *When* Hélène le déplace dans l'équipe « Direction »,
  - *Then* le valideur de la demande devient celui de « Direction » ou le repli RH (RB-33), et l'analyse de chevauchement porte désormais sur « Direction ».
- **CA-16.8**
  - *Given* une équipe contenant des membres actifs,
  - *When* Hélène tente de la supprimer,
  - *Then* la réponse est 409 `TEAM_NOT_EMPTY`.

#### US-17 — Gérer les jours fériés
*En tant qu'admin RH, je veux maintenir la liste des jours fériés afin que le décompte soit exact.*
Règles : RB-03, RB-27, RB-34.

- **CA-17.1**
  - *Given* aucun férié au 2026-07-14,
  - *When* Hélène ajoute (2026-07-14, « Fête nationale »),
  - *Then* toute nouvelle demande couvrant ce jour l'exclut du décompte.
- **CA-17.2**
  - *Given* une demande soumise avant l'ajout du férié,
  - *When* le férié est ajouté,
  - *Then* le `nbJours` de cette demande est inchangé (RB-27).
- **CA-17.3**
  - *Given* le 2026-05-01 déjà enregistré,
  - *When* Hélène l'ajoute de nouveau,
  - *Then* la réponse est 409 `DUPLICATE_HOLIDAY`.

#### US-18 — Consulter le journal d'audit
*En tant qu'admin RH, je veux consulter l'historique des actions sensibles afin de garantir la traçabilité.*
Règles : RB-34.

- **CA-18.1**
  - *Given* des actions journalisées,
  - *When* Hélène filtre par utilisateur et par période,
  - *Then* les entrées correspondantes sont renvoyées, paginées, triées par date décroissante, avec date, auteur, action, entité et détails.
- **CA-18.2**
  - *Given* Marie (Manager sans rôle RH),
  - *When* elle appelle le journal,
  - *Then* la réponse est 403.

---

## 5. Règles métier numérotées (incluant la règle de chevauchement à 50 %)

### Saisie et calcul

**RB-01 — Dates calendaires et fuseau.**
- Les dates de demande sont des dates calendaires, sans heure.
- « Aujourd'hui » désigne la date courante dans le fuseau **Europe/Paris**.
- La date de début DOIT être inférieure ou égale à la date de fin. Sinon : 400 `VALIDATION_ERROR`.

**RB-02 — Granularité.**
- Une demande porte sur des **journées entières**. Les deux bornes sont incluses.
- Les demi-journées sont hors périmètre (A-04).

**RB-03 — Décompte en jours ouvrés.**
- Le nombre de jours d'une demande (`nbJours`) est le nombre de jours de la période qui sont à la fois :
  - du lundi au vendredi ;
  - absents de la table des jours fériés (RB-27).
- La même règle s'applique à tous les types (A-07).
- `nbJours` est calculé et **figé à la soumission**.

**RB-04 — Au moins un jour ouvré.** Une demande dont `nbJours = 0` est rejetée : 422 `NO_WORKING_DAYS`.

**RB-05 — Dates passées.**
- Si le type n'autorise pas le rétroactif, la date de début DOIT être supérieure ou égale à aujourd'hui.
- Si le type l'autorise, la date de début DOIT être supérieure ou égale à aujourd'hui − 30 jours calendaires.
- Sinon : 422 `PAST_DATE_NOT_ALLOWED`.

**RB-06 — Horizon de saisie.** La date de fin DOIT être inférieure ou égale à aujourd'hui + 365 jours calendaires. Sinon : 422 `DATE_BEYOND_HORIZON`.

**RB-07 — Type actif.** Seul un type actif peut être choisi à la soumission. Sinon : 422 `LEAVE_TYPE_INACTIVE`.

**RB-08 — Non-chevauchement personnel.**
- Une nouvelle demande NE DOIT partager **aucun jour calendaire** avec une autre demande du même employé au statut `EnAttente` ou `Approuvée`.
- La règle vaut quel que soit le type.
- Sinon : 422 `OVERLAPPING_OWN_REQUEST`.

### Soldes

**RB-09 — Solde disponible à la soumission.**
- Pour un type décompté : `disponible = solde − Σ nbJours(demandes EnAttente du même employé et du même type)`.
- La soumission est refusée si `nbJours > disponible` : 422 `INSUFFICIENT_BALANCE`.
- Pour un type non décompté : aucun contrôle et aucun solde.

**RB-10 — Débit à l'approbation.**
- À l'approbation d'une demande de type décompté, le système vérifie `solde ≥ nbJours`.
- Si la vérification réussit, il débite `nbJours` et crée un mouvement « Débit approbation ». Changement de statut, débit et mouvement forment **une seule transaction atomique**.
- Si le solde est insuffisant : 422 `INSUFFICIENT_BALANCE` et la demande reste `EnAttente`.

**RB-11 — Invariant.**
- Un solde NE DOIT JAMAIS être négatif.
- Un solde est un décimal positif ou nul, avec 2 décimales maximum.

### Cycle de vie

**RB-12 — Statuts et transitions.** Toute autre transition renvoie 409 `INVALID_STATUS_TRANSITION`.

| De | Vers | Déclencheur | Conditions |
|---|---|---|---|
| (création) | `EnAttente` | Soumission | RB-01 à RB-09, RB-16 |
| `EnAttente` | `Approuvée` | Valideur | RB-10, RB-16, RB-17, RB-21 |
| `EnAttente` | `Refusée` | Valideur | RB-16, RB-17, RB-18 |
| `EnAttente` | `Annulée` | Employé, Admin RH ou système (RB-32) | RB-13 |
| `Approuvée` | `Annulée` | Employé ou Admin RH | RB-13, RB-14 |

Les statuts `Refusée` et `Annulée` sont terminaux.

**RB-13 — Droit d'annulation.**
- **L'employé** peut annuler :
  - sa demande `EnAttente`, à tout moment ;
  - sa demande `Approuvée`, uniquement si la date de début est postérieure à aujourd'hui (strictement). Sinon : 422 `CANCELLATION_NOT_ALLOWED`. Son motif est facultatif.
- **L'Admin RH** peut annuler toute demande `EnAttente` ou `Approuvée`, y compris d'autres utilisateurs et y compris déjà commencée. Un motif est obligatoire (1 à 500 caractères).
- **Le manager** n'a aucun droit d'annulation sur les demandes d'autrui.

**RB-14 — Recrédit.**
- L'annulation d'une demande `Approuvée` de type décompté recrédite **l'intégralité** de `nbJours`, via un mouvement « Crédit annulation ».
- Le recrédit partiel (absence commencée) n'est pas géré : la RH l'ajuste manuellement (RB-31, A-11).

### Organisation et validation

**RB-15 — Équipes.**
- Tout utilisateur actif appartient à **exactement une** équipe.
- Une équipe a 0 ou 1 manager.
- Le manager DOIT être un utilisateur actif disposant du rôle `Manager`. Sinon : 422 `MANAGER_ROLE_REQUIRED`.
- Un manager n'est pas nécessairement membre de l'équipe qu'il manage et peut manager plusieurs équipes.
- Une équipe ne peut être supprimée que si elle n'a aucun membre actif. Sinon : 409 `TEAM_NOT_EMPTY`.

**RB-16 — Détermination du valideur.** Pour une demande d'un employé E, membre de l'équipe T, le valideur est déterminé **au moment de l'évaluation** (consultation, décision, notification) :
1. si T a un manager M actif et que M ≠ E, alors le valideur est M ;
2. sinon, tout Admin RH actif ≠ E est valideur (repli RH) ;
3. si aucun valideur n'existe, la soumission est refusée : 422 `NO_APPROVER_AVAILABLE`.

**RB-17 — Interdiction de l'auto-validation.** Un utilisateur NE DOIT JAMAIS décider de sa propre demande (403 `SELF_APPROVAL_FORBIDDEN`). Un utilisateur qui n'est pas valideur au sens de RB-16 reçoit 403 `NOT_APPROVER`.

**RB-18 — Commentaires de décision.**
- **Refus** : commentaire obligatoire, non vide après suppression des espaces de début et de fin, 1 000 caractères maximum.
- **Approbation** : commentaire facultatif, 1 000 caractères maximum.
- **Commentaire employé à la soumission** : facultatif, 500 caractères maximum.

**RB-19 — Décision unique et concurrence.**
- Une demande ne peut être décidée qu'une fois.
- Les modifications concurrentes d'une même demande ou d'un même solde sont détectées par contrôle de concurrence optimiste. La seconde opération reçoit 409 `CONCURRENCY_CONFLICT` ou `INVALID_STATUS_TRANSITION`.

### Règle de chevauchement à 50 %

**RB-20 — Calcul du taux de chevauchement.**

Soit R une demande de l'employé E, membre de l'équipe T, sur la période [début ; fin].

1. **Population de référence** P = membres **actifs** de T **hors E**. Seuls les utilisateurs dont l'équipe est T sont comptés, ce qui exclut le manager de T s'il n'en est pas membre.
2. Si |P| = 0, aucun jour n'est critique et R n'est pas signalée.
3. **Jours évalués** : les jours ouvrés de R au sens de RB-03.
4. Pour chaque jour évalué D :
   - `absents(D)` = nombre de membres **distincts** de P ayant au moins une demande au statut **`Approuvée`** (tout type) dont la période couvre D ;
   - `taux(D) = absents(D) / |P|`.
5. D est **critique** si `taux(D) > seuil`. L'inégalité est **stricte**.
6. R est **signalée** si au moins un jour évalué est critique.
7. Le **seuil** vaut **0,50** par défaut. Il est paramétrable par configuration applicative (valeur dans ]0 ; 1[), sans interface en V1.
8. Ne sont **pas** pris en compte :
   - les demandes `EnAttente`, `Refusée` et `Annulée` des autres membres ;
   - les demandes de E lui-même.

Exemples. Le cas « Produit » repose sur E = Eric et |P| = 4.

| Cas | Situation | Taux | Résultat |
|---|---|---|---|
| Produit, jour J1 | Alice et Bruno absents | 2/4 = 0,50 | Non critique (inégalité stricte) |
| Produit, jour J2 | Alice, Bruno et Chloé absents | 3/4 = 0,75 | Critique, donc R signalée |
| Équipe de 2 (E + A) | A absent | 1/1 = 1,00 | Critique |
| Équipe de 1 (E seul) | — | — | Jamais signalée |

**RB-21 — Signalement et acquittement.** Lorsqu'une demande est signalée :
- **(a) file de validation** : un badge « Chevauchement critique » est affiché ;
- **(b) détail** : les jours critiques sont listés avec, pour chacun, le nombre d'absents, le taux et les noms des absents. Ces données sont visibles du valideur et de l'Admin RH uniquement ;
- **(c) email de soumission** : il mentionne le signalement si la demande est signalée à la soumission ;
- **(d) approbation** : elle exige le paramètre explicite `acquittementChevauchement = true`. Sinon : 422 `OVERLAP_ACK_REQUIRED`, sans changement d'état ;
- **(e) refus** : il n'exige pas d'acquittement ;
- **(f) prévisualisation et soumission** : l'employé est informé que sa demande sera signalée, sans aucun nom.

**RB-22 — Fraîcheur et instantanés.**
- Le signalement est **recalculé** à chaque consultation de la file ou du détail, et à la décision.
- Deux instantanés sont persistés sur la demande :
  - à la soumission : signalée oui/non et taux maximal ;
  - à la décision : signalée oui/non, taux maximal et acquittement.
- C'est le calcul effectué **à la décision** qui fait foi pour RB-21 (d).

### Notifications

**RB-23 — Notification de soumission.**
- À la création d'une demande, un email est mis en file pour le ou les valideurs (RB-16).
- Contenu :
  - nom de l'employé, type, période, `nbJours` et commentaire ;
  - indicateur de signalement ;
  - lien profond vers la demande.

**RB-24 — Notification de décision.**
- À l'approbation ou au refus, un email est mis en file pour l'employé.
- Contenu :
  - décision, type, période et `nbJours` ;
  - décideur et commentaire (obligatoire pour un refus) ;
  - solde restant si le type est décompté et la demande approuvée.

**RB-25 — Notification d'annulation.** Cette règle est une extension du brief (A-09).
- À l'annulation d'une demande `EnAttente` ou `Approuvée` par l'employé, un email est mis en file pour le ou les valideurs courants.
- À l'annulation par un Admin RH, un email est mis en file pour l'employé.

**RB-26 — Fiabilité des notifications.**
- Les emails sont enregistrés dans une file persistante, **dans la même transaction** que l'opération métier (pattern outbox), puis envoyés de façon asynchrone.
- Un échec d'envoi n'annule jamais l'opération métier.
- Politique de nouvelle tentative : 5 tentatives au maximum, avec un délai croissant (1, 5, 15, 60, 240 min). Ensuite, le statut passe à « Échec » et un journal d'erreur est produit.
- Le délai cible entre l'opération et l'envoi est inférieur à 5 min, hors panne SMTP.

### Référentiels et visibilité

**RB-27 — Jours fériés.**
- La table des jours fériés est maintenue par l'Admin RH. Les dates sont uniques.
- L'ajout ou la suppression d'un férié n'affecte pas le `nbJours` des demandes existantes (RB-03).
- Les fériés 2026 et 2027 de France métropolitaine sont pré-chargés (A-07).

**RB-28 — Visibilité des demandes.**
- **Employé** : ses propres demandes uniquement.
- **Manager** : en plus, toutes les demandes (tous statuts) des membres des équipes qu'il manage.
- **Admin RH** : toutes les demandes.
- Une demande hors périmètre renvoie **404**, pour ne pas révéler son existence.

**RB-29 — Calendrier et confidentialité.**
- Le calendrier n'affiche que les demandes **`Approuvée`**.
- Le **type** d'une absence est visible uniquement par :
  - l'intéressé ;
  - le manager de son équipe ;
  - les Admins RH.
- Pour les autres consultants, l'absence apparaît sous le libellé neutre « Absent ». Cette règle protège notamment les données de santé (MALADIE).
- Périmètre consultable :
  - tout utilisateur : sa propre équipe ;
  - Manager : en plus, les équipes qu'il manage ;
  - Admin RH : toutes les équipes.
- La plage interrogée est limitée à 93 jours. Au-delà : 400 `RANGE_TOO_LARGE`.

**RB-30 — Types de congés.**
- **Attributs** :
  - code (unique, insensible à la casse, 2 à 20 caractères parmi `A-Z`, `0-9` et `_`, immuable) ;
  - libellé (1 à 50 caractères) ;
  - décompte solde (booléen) ;
  - rétroactif autorisé (booléen) ;
  - couleur (hexadécimal `#RRGGBB`) ;
  - ordre d'affichage ;
  - actif.
- **Suppression** : un type n'est jamais supprimé, seulement désactivé.
- **Attribut « décompte solde »** : il n'est modifiable que si aucune demande `EnAttente` n'existe pour ce type. Sinon : 409 `LEAVE_TYPE_IN_USE`.
- **Types initiaux** :

| Code | Libellé | Décompte | Rétroactif |
|---|---|---|---|
| CP | Congés payés | oui | non |
| RTT | RTT | oui | non |
| MALADIE | Maladie | non | oui |
| AUTRE | Autre absence | non | non |

**RB-31 — Soldes : initialisation et ajustement.**
- **Initialisation** :
  - un solde à 0 est créé automatiquement pour chaque couple (utilisateur actif, type décompté) ;
  - cela se produit à la création d'un compte ou d'un type décompté, et à la réactivation ou au passage en type décompté ;
  - chaque création génère un mouvement « Initialisation ».
- **Ajustement RH** :
  - montant signé non nul, 2 décimales maximum ;
  - motif obligatoire (1 à 500 caractères) ;
  - solde résultant ≥ 0. Sinon : 422 `NEGATIVE_BALANCE` ;
  - un Admin RH NE PEUT PAS ajuster ses propres soldes : 403 `SELF_BALANCE_ADJUSTMENT_FORBIDDEN`.
- **Traçabilité** : chaque variation produit un mouvement immuable (nature, montant signé, solde après, auteur, date, motif, demande liée).

**RB-32 — Comptes.**
- L'email est unique, insensible à la casse.
- Les rôles possibles sont `Manager` et `AdminRH`. Le rôle Employé est implicite.
- **Désactivation d'un compte** :
  - connexion refusée et jetons de rafraîchissement révoqués ;
  - demandes `EnAttente` annulées par le système avec le motif « Compte désactivé » ;
  - demandes `Approuvée` conservées ;
  - si l'utilisateur managait des équipes, ces équipes n'ont plus de manager.
- **Comptes et données** : les comptes ne sont pas supprimés physiquement ; leur sort relève de l'anonymisation (ENF-RGPD).
- **Dernier Admin RH** : il DOIT toujours exister au moins un Admin RH actif. Sinon : 422 `LAST_HR_ADMIN`.

**RB-33 — Changement d'équipe.**
- Un changement d'équipe prend effet immédiatement.
- Le valideur des demandes `EnAttente` suit RB-16 dynamiquement.
- Les calculs de chevauchement (RB-20) et les calendriers utilisent l'appartenance **courante**, y compris pour les absences passées (A-16).

**RB-34 — Journal d'audit.**
- Sont journalisés : connexions réussies et échouées, verrouillages, changements de mot de passe, soumissions, décisions (avec acquittement), annulations, ajustements de soldes, opérations sur comptes, rôles, équipes, types et jours fériés.
- Contenu d'une entrée : date UTC, auteur, action, entité, identifiant, détails (avant/après).
- Le journal est en ajout seul : aucune modification ni suppression par l'application.

---

## 6. Modèle de données fonctionnel (entités, attributs, relations, sans SQL)

Types fonctionnels utilisés :

| Type | Signification |
|---|---|
| `Id` | Identifiant opaque (GUID) |
| `Date` | Date calendaire |
| `Horodatage` | Instant UTC |
| `Décimal(2)` | Décimal à 2 décimales |
| `Texte(n)` | Chaîne de *n* caractères au maximum |

Toute entité modifiable porte un **jeton de version** pour la concurrence optimiste (RB-19) ainsi que les horodatages de création et de modification.

### 6.1 Entités

**Utilisateur**

| Attribut | Type | Obl. | Contraintes |
|---|---|---|---|
| id | Id | ✔ | — |
| email | Texte(254) | ✔ | Unique, insensible à la casse, format email |
| prénom, nom | Texte(100) | ✔ | — |
| empreinteMotDePasse | Texte | ✔ | Jamais exposé (ENF-SEC-04) |
| rôles | Ensemble {Manager, AdminRH} | ✔ | Peut être vide (Employé implicite) |
| équipeId | Id → Équipe | ✔ | RB-15 |
| actif | Booléen | ✔ | — |
| échecsConnexion, verrouilléJusquà | Entier, Horodatage? | ✔ / — | ENF-SEC-03 |
| dernièreConnexion | Horodatage? | — | — |

**Équipe**

| Attribut | Type | Obl. | Contraintes |
|---|---|---|---|
| id | Id | ✔ | — |
| nom | Texte(100) | ✔ | Unique |
| managerId | Id? → Utilisateur | — | Utilisateur actif avec le rôle Manager (RB-15) |

**TypeCongé**

| Attribut | Type | Obl. | Contraintes |
|---|---|---|---|
| id | Id | ✔ | — |
| code | Texte(20) | ✔ | Unique, immuable (RB-30) |
| libellé | Texte(50) | ✔ | — |
| décompteSolde | Booléen | ✔ | — |
| rétroactifAutorisé | Booléen | ✔ | — |
| couleur | Texte(7) | ✔ | `#RRGGBB` |
| ordreAffichage | Entier | ✔ | — |
| actif | Booléen | ✔ | — |

**Solde**

| Attribut | Type | Obl. | Contraintes |
|---|---|---|---|
| id | Id | ✔ | — |
| utilisateurId | Id → Utilisateur | ✔ | — |
| typeCongéId | Id → TypeCongé | ✔ | Unicité du couple (utilisateur, type) |
| valeur | Décimal(2) | ✔ | ≥ 0 (RB-11) |

Les valeurs « en attente » et « disponible » sont **calculées** et non stockées (RB-09).

**MouvementSolde** (immuable)

| Attribut | Type | Obl. | Contraintes |
|---|---|---|---|
| id | Id | ✔ | — |
| soldeId | Id → Solde | ✔ | — |
| nature | {Initialisation, DébitApprobation, CréditAnnulation, AjustementRH} | ✔ | — |
| montant | Décimal(2) signé | ✔ | ≠ 0, sauf pour Initialisation |
| soldeAprès | Décimal(2) | ✔ | ≥ 0 |
| demandeId | Id? → DemandeCongé | — | Obligatoire pour les natures Débit et Crédit |
| motif | Texte(500)? | — | Obligatoire pour AjustementRH |
| auteurId | Id? → Utilisateur | — | Nul si l'auteur est le système |
| horodatage | Horodatage | ✔ | — |

**DemandeCongé**

| Attribut | Type | Obl. | Contraintes |
|---|---|---|---|
| id | Id | ✔ | — |
| employéId | Id → Utilisateur | ✔ | — |
| typeCongéId | Id → TypeCongé | ✔ | — |
| dateDébut, dateFin | Date | ✔ | Début ≤ fin (RB-01) |
| nbJours | Entier | ✔ | ≥ 1, figé (RB-03) |
| commentaireEmployé | Texte(500)? | — | — |
| statut | {EnAttente, Approuvée, Refusée, Annulée} | ✔ | RB-12 |
| dateSoumission | Horodatage | ✔ | — |
| signaléeÀSoumission, tauxMaxÀSoumission | Booléen, Décimal | ✔ | RB-22 |
| décideurId | Id? → Utilisateur | — | Renseigné si décidée |
| dateDécision | Horodatage? | — | — |
| commentaireDécision | Texte(1000)? | — | Obligatoire si refusée (RB-18) |
| signaléeÀDécision, tauxMaxÀDécision, chevauchementAcquitté | Booléen?, Décimal?, Booléen? | — | RB-22 |
| annuléeParId, dateAnnulation, motifAnnulation | Id?, Horodatage?, Texte(500)? | — | RB-13 |

**JourFérié**

| Attribut | Type | Obl. | Contraintes |
|---|---|---|---|
| id | Id | ✔ | — |
| date | Date | ✔ | Unique |
| libellé | Texte(100) | ✔ | — |

**NotificationEmail** (outbox)

| Attribut | Type | Obl. | Contraintes |
|---|---|---|---|
| id | Id | ✔ | — |
| destinataire | Texte(254) | ✔ | — |
| modèle | {Soumission, Approbation, Refus, AnnulationEmployé, AnnulationRH} | ✔ | — |
| donnéesModèle | Document structuré | ✔ | — |
| demandeId | Id? | — | — |
| statut | {EnFile, Envoyé, Échec} | ✔ | — |
| tentatives | Entier | ✔ | — |
| prochaineTentative | Horodatage? | — | — |
| dernièreErreur | Texte? | — | — |
| crééLe, envoyéLe | Horodatage, Horodatage? | ✔ / — | — |

**JetonRafraîchissement**

| Attribut | Type | Obl. | Contraintes |
|---|---|---|---|
| id | Id | ✔ | — |
| utilisateurId | Id → Utilisateur | ✔ | — |
| empreinteJeton | Texte | ✔ | Le jeton en clair n'est jamais stocké |
| expireLe | Horodatage | ✔ | — |
| révoquéLe | Horodatage? | — | — |
| remplacéParId | Id? | — | Rotation (CA-01.5, CA-01.6) |

**EntréeAudit** (ajout seul)

| Attribut | Type | Obl. | Contraintes |
|---|---|---|---|
| id | Id | ✔ | — |
| horodatage | Horodatage | ✔ | — |
| auteurId | Id? | — | Nul si système ou anonyme (connexion échouée) |
| action | Texte(100) | ✔ | Ex. `LeaveRequest.Approved` |
| typeEntité | Texte(100) | ✔ | — |
| entitéId | Texte(100)? | — | — |
| détails | Document structuré | — | Sans donnée de santé ni secret |
| adresseIP | Texte(45)? | — | — |

### 6.2 Relations

| Relation | Cardinalité | Remarque |
|---|---|---|
| Équipe — Utilisateur (membres) | 1 — 0..n | Via `Utilisateur.équipeId` |
| Équipe — Utilisateur (manager) | 0..n — 0..1 | Un utilisateur peut manager plusieurs équipes |
| Utilisateur — Solde | 1 — 0..n | Un solde par type décompté |
| TypeCongé — Solde | 1 — 0..n | — |
| Solde — MouvementSolde | 1 — 0..n | Historique complet |
| Utilisateur — DemandeCongé (employé) | 1 — 0..n | — |
| TypeCongé — DemandeCongé | 1 — 0..n | — |
| DemandeCongé — MouvementSolde | 1 — 0..2 | Débit à l'approbation, crédit à l'annulation |
| DemandeCongé — NotificationEmail | 1 — 0..n | — |
| Utilisateur — JetonRafraîchissement | 1 — 0..n | — |

### 6.3 Invariants vérifiables

- `Solde.valeur = Σ MouvementSolde.montant` du solde. Cette égalité sert de test d'intégrité.
- Pour une demande `Approuvée` de type décompté, il existe exactement 1 mouvement DébitApprobation lié.
- Pour une demande `Annulée` après approbation sur un type décompté, il existe exactement 1 DébitApprobation et 1 CréditAnnulation liés.

### 6.4 Index fonctionnels nécessaires

- DemandeCongé par (employéId, statut, dateDébut, dateFin) : contrôles RB-08 et RB-09.
- DemandeCongé par (statut, dateDébut, dateFin) : calendrier et RB-20.
- Utilisateur par équipeId.
- NotificationEmail par (statut, prochaineTentative).

---

## 7. Exigences non fonctionnelles (performance, sécurité, disponibilité, accessibilité, RGPD)

### 7.1 Performance

Hypothèse de charge : 50 utilisateurs, environ 20 sessions simultanées au pic, environ 1 000 demandes par an.

| ID | Exigence |
|---|---|
| ENF-PERF-01 | Temps de réponse API p95 < 300 ms en lecture et < 500 ms en écriture, mesuré côté serveur, avec 20 utilisateurs simultanés et 5 ans de données. |
| ENF-PERF-02 | Calendrier d'une équipe sur 93 jours : p95 < 500 ms. |
| ENF-PERF-03 | Calcul RB-20 pour une demande de 30 jours ouvrés dans une équipe de 20 personnes : < 100 ms. Le calcul repose sur une seule requête agrégée, sans N+1. |
| ENF-PERF-04 | Frontend : chargement initial < 2 s en réseau d'entreprise (LCP), bundle JS initial compressé < 300 Ko. |
| ENF-PERF-05 | Listes paginées côté serveur : 20 éléments par défaut, 100 au maximum. |

### 7.2 Sécurité

| ID | Exigence |
|---|---|
| ENF-SEC-01 | HTTPS obligatoire (TLS 1.2 minimum), HSTS. Aucun échange en clair. |
| ENF-SEC-02 | **Jeton d'accès** : JWT signé, durée de vie 30 min. Claims : `sub`, `email`, `name`, `role` (multi-valué), `iss`, `aud`, `exp`, `jti`. Algorithme et gestion des clés : étape 2 (PO-04). Les secrets ne sont jamais versionnés.<br>**Jeton de rafraîchissement** : opaque, 7 jours, rotatif, stocké sous forme d'empreinte. Il est transmis par cookie `HttpOnly; Secure; SameSite=Strict` limité au chemin d'authentification.<br>**Côté frontend** : le jeton d'accès est conservé **en mémoire**, jamais dans `localStorage`. |
| ENF-SEC-03 | Verrouillage de 15 min après 5 échecs consécutifs (CA-01.3). Limitation de débit sur l'authentification : 10 requêtes/min par adresse IP. |
| ENF-SEC-04 | Mots de passe : 12 caractères minimum, 128 maximum, sans autre contrainte de composition, refus des 1 000 mots de passe les plus courants. Hachage par algorithme adaptatif (PBKDF2 ≥ 600 000 itérations ou Argon2id). |
| ENF-SEC-05 | Autorisation vérifiée côté serveur sur chaque point d'accès, par rôle et par périmètre (section 3.2, RB-28, RB-29). Politique de refus par défaut : tout point d'accès non annoté exige l'authentification. |
| ENF-SEC-06 | Validation systématique des entrées côté serveur. Requêtes paramétrées exclusivement (ORM). Encodage des sorties côté frontend, sans injection HTML brute. |
| ENF-SEC-07 | CORS restreint à l'origine du frontend. En-têtes de sécurité : CSP, `X-Content-Type-Options`, `Referrer-Policy`, `Frame-Options DENY`. |
| ENF-SEC-08 | Les journaux techniques ne contiennent ni mot de passe, ni jeton, ni type d'absence MALADIE associé à un nom. |
| ENF-SEC-09 | Couverture OWASP ASVS niveau 1. Analyse de dépendances vulnérables en intégration continue. |
| ENF-SEC-10 | Erreurs au format RFC 7807 (*Problem Details*), sans trace de pile en production. |

### 7.3 Disponibilité, exploitabilité, maintenabilité

| ID | Exigence |
|---|---|
| ENF-DISP-01 | Disponibilité de 99,5 % sur la plage de service (lundi à vendredi, 7 h–20 h, Europe/Paris). Maintenance planifiée hors plage. |
| ENF-DISP-02 | Sauvegarde quotidienne de la base, rétention 30 jours. RPO ≤ 24 h, RTO ≤ 4 h. Restauration testée au moins une fois par an. |
| ENF-DISP-03 | Points de santé `liveness` et `readiness`, ce dernier incluant la base. |
| ENF-OBS-01 | Journaux structurés (JSON) avec identifiant de corrélation propagé du frontend à l'API puis aux emails. Métriques : nombre de requêtes, latences, erreurs et taille de la file d'emails. |
| ENF-MAINT-01 | Migrations de schéma versionnées et rejouables. Données initiales (types, fériés, premier Admin RH) créées par un mécanisme idempotent. |
| ENF-MAINT-02 | Tests xUnit couvrant **chaque RB** et chaque CA côté backend (au moins un test nommé par référence). Couverture de la couche domaine ≥ 80 %. Horloge injectable. |
| ENF-MAINT-03 | Configuration externalisée (seuil RB-20, durées des jetons, SMTP, origines CORS), sans recompilation. |

### 7.4 Accessibilité et ergonomie

| ID | Exigence |
|---|---|
| ENF-ACC-01 | Conformité **RGAA 4.1 / WCAG 2.1 niveau AA**. |
| ENF-ACC-02 | Toutes les fonctions sont utilisables au clavier, avec focus visible et ordre logique. Les champs de date sont saisissables au clavier. |
| ENF-ACC-03 | Le calendrier dispose d'une **vue liste équivalente** (tableau sémantique). La couleur n'est jamais le seul vecteur d'information (libellé ou motif en complément). Contraste ≥ 4,5:1. |
| ENF-ACC-04 | Messages d'erreur associés aux champs (`aria-describedby`) et annoncés aux lecteurs d'écran. Alerte de chevauchement exposée via `role="alert"`. |
| ENF-ACC-05 | Interface responsive à partir de 360 px de largeur. Navigateurs supportés : les deux dernières versions de Chrome, Edge, Firefox et Safari. |
| ENF-ACC-06 | Interface en français. Dates au format `JJ/MM/AAAA` et semaine commençant le lundi. |

### 7.5 RGPD

| ID | Exigence |
|---|---|
| ENF-RGPD-01 | **Finalité** : gestion administrative des absences. **Base légale** : exécution du contrat de travail et obligations légales de l'employeur. Le traitement est inscrit au registre (à la charge de l'entreprise). |
| ENF-RGPD-02 | **Minimisation** : aucun motif médical, diagnostic ni justificatif n'est collecté. Le type MALADIE constitue une donnée relative à la santé (art. 9) : sa visibilité est restreinte (RB-29), il est absent des journaux techniques (ENF-SEC-08) et il n'est communiqué par email qu'au valideur. |
| ENF-RGPD-03 | **Conservation** (A-17) : demandes et mouvements conservés pendant la durée du contrat puis 5 ans. Au-delà, anonymisation (nom et email remplacés par un pseudonyme). Audit conservé 1 an pour les connexions et 5 ans pour les actions métier. Emails envoyés purgés de l'outbox après 90 jours. |
| ENF-RGPD-04 | **Droits des personnes** : l'Admin RH peut produire un export (JSON ou CSV) de toutes les données d'un utilisateur (droit d'accès et de portabilité). Rectification par l'Admin RH. |
| ENF-RGPD-05 | **Hébergement** des données et du service SMTP dans l'UE (PO-02, PO-03). |
| ENF-RGPD-06 | **Contrôles d'accès** journalisés (RB-34), principe du moindre privilège (section 3.2). |

---

## 8. Ébauche d'API (ressources et verbes, sans implémentation)

### 8.1 Conventions

- **Format** : base `/api/v1`, JSON, `camelCase`. Dates `YYYY-MM-DD` ; horodatages ISO 8601 UTC.
- **Authentification** : `Authorization: Bearer <jwt>` sur toutes les routes, sauf `POST /auth/login` et `POST /auth/refresh`.
- **Pagination** : paramètres `page` (à partir de 1) et `pageSize` (20 par défaut, 100 au maximum). Réponse `{ items, page, pageSize, totalCount }`.
- **Concurrence** : chaque ressource modifiable expose `version`, renvoyée lors des modifications administratives (PUT/PATCH). Un conflit renvoie 409 `CONCURRENCY_CONFLICT`.
- **Codes HTTP** :

| Code | Usage |
|---|---|
| 200 / 201 / 204 | Succès |
| 400 | Format ou validation syntaxique (`VALIDATION_ERROR`, avec `errors` par champ) |
| 401 | Non authentifié |
| 403 | Authentifié mais non autorisé |
| 404 | Ressource inexistante ou hors périmètre (RB-28) |
| 409 | Conflit d'état, d'unicité ou de concurrence |
| 422 | Violation d'une règle métier |

- **Corps d'erreur** (RFC 7807) : `{ type, title, status, detail, code, traceId, errors? }`. Le champ `code` contient l'un des codes métier listés en 8.3.

### 8.2 Ressources

**Authentification et profil**

| Verbe | Ressource | Accès | Description | Réf. |
|---|---|---|---|---|
| POST | `/auth/login` | Public | `{email, password}` → `{accessToken, expiresIn, user}` et dépôt du cookie de rafraîchissement | US-01 |
| POST | `/auth/refresh` | Cookie | Rotation des jetons | CA-01.5, CA-01.6 |
| POST | `/auth/logout` | Authentifié | Révocation du jeton de rafraîchissement | CA-01.7 |
| GET | `/me` | Authentifié | Profil : id, nom, email, rôles, équipe, équipes managées | — |
| POST | `/me/password` | Authentifié | `{currentPassword, newPassword}` | US-02 |
| GET | `/me/balances` | Authentifié | Soldes : `[{leaveType, balance, pending, available, leaveTypeActive}]` | US-03 |
| GET | `/me/balances/{leaveTypeId}/movements` | Authentifié | Historique paginé des mouvements | CA-03.4 |
| GET | `/me/leave-requests` | Authentifié | Filtres `status`, `from`, `to` ; paginé | US-05 |

**Demandes**

| Verbe | Ressource | Accès | Description | Réf. |
|---|---|---|---|---|
| POST | `/leave-requests/preview` | Authentifié | Simulation sans création (voir 8.4) | CA-04.3, RB-21 (f) |
| POST | `/leave-requests` | Authentifié | Soumission `{leaveTypeId, startDate, endDate, comment?}` → 201 et ressource créée | US-04 |
| GET | `/leave-requests/{id}` | Périmètre RB-28 | Détail de la demande | US-05, US-10 |
| GET | `/leave-requests/{id}/overlap` | Valideur ou Admin RH | Analyse détaillée (voir 8.4) | US-10 |
| POST | `/leave-requests/{id}/approve` | Valideur RB-16 | `{comment?, overlapAcknowledged?}` | US-11 |
| POST | `/leave-requests/{id}/reject` | Valideur RB-16 | `{comment}` (obligatoire) | US-12 |
| POST | `/leave-requests/{id}/cancel` | Employé (sa demande) ou Admin RH | `{reason?}`, avec `reason` obligatoire pour un Admin RH | US-06 |
| GET | `/approvals/pending` | Authentifié | Demandes dont l'appelant est valideur, avec `overlapFlagged` ; paginé | US-09 |
| GET | `/users/{userId}/leave-requests` | Manager (périmètre) ou Admin RH | Demandes d'un utilisateur ; paginé | RB-28 |

**Calendrier**

| Verbe | Ressource | Accès | Description | Réf. |
|---|---|---|---|---|
| GET | `/teams/{teamId}/calendar?from=&to=` | RB-29 | `{team, members[], absences[{userId, displayName, startDate, endDate, leaveType \| null}], holidays[]}`, où `leaveType` est nul si masqué | US-07 |
| GET | `/teams/visible` | Authentifié | Équipes dont l'appelant peut voir le calendrier | RB-29 |

**Administration (Admin RH)**

| Verbe | Ressource | Description | Réf. |
|---|---|---|---|
| GET, POST | `/admin/leave-types` | Lister (y compris inactifs), créer | US-14 |
| PUT | `/admin/leave-types/{id}` | Modifier libellé, couleur, ordre, flags et actif (code exclu) | US-14 |
| GET, POST | `/admin/users` | Lister (filtres `teamId`, `active`, `role`, `q`), créer | US-16 |
| GET, PUT | `/admin/users/{id}` | Détail, modification (nom, équipe, rôles) | US-16 |
| POST | `/admin/users/{id}/deactivate` | Désactiver | RB-32 |
| POST | `/admin/users/{id}/reactivate` | Réactiver | RB-31, RB-32 |
| POST | `/admin/users/{id}/password` | Définir un nouveau mot de passe (réinitialisation par la RH) | A-12 |
| GET | `/admin/users/{id}/export` | Export RGPD | ENF-RGPD-04 |
| GET | `/admin/users/{id}/balances` | Soldes d'un utilisateur | US-15 |
| POST | `/admin/users/{id}/balances/{leaveTypeId}/adjustments` | `{amount, reason}` | US-15 |
| GET | `/admin/users/{id}/balances/{leaveTypeId}/movements` | Historique | US-15 |
| GET, POST | `/admin/teams` | Lister, créer `{name, managerId?}` | US-16 |
| PUT, DELETE | `/admin/teams/{id}` | Modifier, supprimer (si vide) | RB-15 |
| GET, POST | `/admin/public-holidays?year=` | Lister, ajouter | US-17 |
| DELETE | `/admin/public-holidays/{id}` | Supprimer | US-17 |
| GET | `/admin/audit-logs` | Filtres `actorId`, `entityType`, `entityId`, `from`, `to` ; paginé | US-18 |

**Technique**

| Verbe | Ressource | Description |
|---|---|---|
| GET | `/health/live`, `/health/ready` | Points de santé (ENF-DISP-03), sans authentification |

### 8.3 Codes d'erreur métier

| Code | HTTP | Règle |
|---|---|---|
| `VALIDATION_ERROR` | 400 | Format, bornes, champs obligatoires |
| `RANGE_TOO_LARGE` | 400 | RB-29 |
| `NO_WORKING_DAYS` | 422 | RB-04 |
| `PAST_DATE_NOT_ALLOWED` | 422 | RB-05 |
| `DATE_BEYOND_HORIZON` | 422 | RB-06 |
| `LEAVE_TYPE_INACTIVE` | 422 | RB-07 |
| `OVERLAPPING_OWN_REQUEST` | 422 | RB-08 |
| `INSUFFICIENT_BALANCE` | 422 | RB-09, RB-10 |
| `NO_APPROVER_AVAILABLE` | 422 | RB-16 |
| `OVERLAP_ACK_REQUIRED` | 422 | RB-21 |
| `CANCELLATION_NOT_ALLOWED` | 422 | RB-13 |
| `NEGATIVE_BALANCE` | 422 | RB-31 |
| `MANAGER_ROLE_REQUIRED` | 422 | RB-15 |
| `LAST_HR_ADMIN` | 422 | RB-32 |
| `INVALID_CURRENT_PASSWORD` | 422 | US-02 |
| `SELF_APPROVAL_FORBIDDEN` | 403 | RB-17 |
| `NOT_APPROVER` | 403 | RB-17 |
| `SELF_BALANCE_ADJUSTMENT_FORBIDDEN` | 403 | RB-31 |
| `INVALID_STATUS_TRANSITION` | 409 | RB-12, RB-19 |
| `CONCURRENCY_CONFLICT` | 409 | RB-19 |
| `DUPLICATE_CODE`, `DUPLICATE_EMAIL`, `DUPLICATE_HOLIDAY` | 409 | RB-30, RB-32, RB-27 |
| `LEAVE_TYPE_IN_USE`, `TEAM_NOT_EMPTY` | 409 | RB-30, RB-15 |

### 8.4 Formes indicatives des réponses clés

**`POST /leave-requests/preview`**

```json
{
  "workingDays": 5,
  "balanceChecked": true,
  "available": 10.00,
  "sufficientBalance": true,
  "willBeFlagged": true,
  "validationErrors": []
}
```

**`GET /leave-requests/{id}/overlap`**

```json
{
  "flagged": true,
  "threshold": 0.50,
  "referenceHeadcount": 4,
  "maxRate": 0.75,
  "criticalDays": [
    { "date": "2026-03-10", "absentCount": 3, "rate": 0.75,
      "absentees": [ { "userId": "…", "displayName": "Alice Martin" } ] }
  ],
  "computedAt": "2026-03-02T09:15:00Z",
  "atSubmission": { "flagged": true, "maxRate": 0.75 }
}
```

---

## 9. Risques et points ouverts (liste explicite des ambiguïtés du brief et des hypothèses retenues)

### 9.1 Ambiguïtés du brief et hypothèses retenues

Chaque hypothèse est appliquée telle quelle par les étapes 2 à 4, sauf invalidation par le métier (« À valider par »).

| ID | Ambiguïté | Hypothèse retenue | Impact si invalidée | À valider par |
|---|---|---|---|---|
| A-01 | Outil actuel non précisé | Gestion actuelle par email et tableur. Aucune reprise de données automatisée : soldes initiaux saisis par la RH via ajustements. | Faible : ajout d'un import CSV | RH |
| A-02 | **« Chevauche plus de 50 % d'absences déjà approuvées sur l'équipe »** admet plusieurs lectures : (i) part de l'équipe déjà absente un jour donné ; (ii) part des jours de la demande qui chevauchent au moins une absence ; (iii) part de l'équipe absente *en incluant* le demandeur. | Lecture (i), car elle correspond à l'objectif « chevauchements critiques », donc au risque de sous-effectif : taux journalier = autres membres déjà absents (approuvés) / autres membres actifs. Seuil **strictement** supérieur à 0,50 ; un seul jour critique suffit. Seuil configurable (RB-20). | Moyen, mais isolé dans un service de calcul unique et paramétrable | Direction / managers |
| A-03 | « Signalée au manager avant décision » : simple information ou contrôle bloquant ? | Signalement visuel et email, **plus** acquittement explicite obligatoire pour approuver. Le refus reste libre (RB-21). Pas de blocage de la soumission. | Faible : retrait d'un paramètre | Managers |
| A-04 | Granularité non précisée | Journées entières uniquement en V1 (RB-02). Soldes en décimal pour préparer les demi-journées. | Moyen : impacte le calcul, RB-20 et l'interface | RH |
| A-05 | Temps partiel non évoqué | Tous les salariés travaillent du lundi au vendredi. | Moyen : un calendrier de travail par salarié serait nécessaire | RH |
| A-06 | « Solde de jours par type » : gestion des acquisitions ? | Le solde est un compteur alimenté **manuellement** par la RH. Pas d'acquisition automatique, de période de référence ni de report. | Élevé si un calcul automatique est attendu (nouveau module) | RH |
| A-07 | Unité de décompte | **Jours ouvrés** (lundi–vendredi hors fériés) pour tous les types. Le Code du travail raisonne en jours *ouvrables* (lundi–samedi) pour les CP ; le décompte en jours ouvrés est admis s'il n'est pas défavorable au salarié. Fériés de France métropolitaine (hors Alsace-Moselle) pré-chargés pour 2026–2027, puis maintenus par la RH. | Moyen : isolé dans RB-03 | RH / juridique |
| A-08 | Maladie soumise à « solde » et à « validation » selon le brief | MALADIE non décomptée, rétroactif autorisé (30 jours), même workflow de validation (le brief l'impose). Pas de justificatif. En pratique, le manager « prend acte » ; le refus reste techniquement possible. | Moyen : un arrêt maladie ne se refuse pas juridiquement. Option : approbation automatique par type | RH / juridique |
| A-09 | Annulation non évoquée | Annulation possible (RB-13, RB-14) avec notification (RB-25). Ces deux notifications sortent du strict périmètre du brief. | Faible | RH |
| A-10 | Qui valide les demandes d'un manager, ou d'une équipe sans manager ? | Le manager de l'équipe dont il est membre, sinon repli Admin RH (RB-16). Validation à un seul niveau. | Faible | Direction |
| A-11 | Annulation d'une absence commencée | Admin RH uniquement, recrédit intégral, régularisation par ajustement (RB-14). | Faible | RH |
| A-12 | Cycle de vie des mots de passe | L'Admin RH crée les comptes et (re)définit les mots de passe. Pas de réinitialisation en libre-service ni de changement forcé à la première connexion en V1. | Moyen (sécurité) : SSO recommandé à terme (PO-05) | DSI |
| A-13 | Modification d'une demande | Non supportée : annuler puis resoumettre. | Faible | RH |
| A-14 | Saisie pour autrui (salarié absent sans accès) | Non supportée. | Faible : l'Admin RH peut ajuster les soldes | RH |
| A-15 | Organisation | Une seule entité juridique, un seul pays, interface en français. | — | — |
| A-16 | Historique d'appartenance aux équipes | Non historisé : appartenance courante utilisée partout (RB-33). | Faible : calendrier rétrospectif approximatif après mutation | RH |
| A-17 | Durées de conservation | Contrat + 5 ans pour les demandes et soldes. Audit : 1 an pour les connexions, 5 ans pour les actions métier (ENF-RGPD-03). | Faible : paramétrable | DPO |
| A-18 | Visibilité du calendrier | Les membres d'une équipe voient les noms et périodes des absences approuvées de leurs collègues, sans le type (RB-29). | Faible | RH / DPO |
| A-19 | Bornes de saisie | Horizon de 365 jours, rétroactif de 30 jours (RB-05, RB-06). | Faible : paramètres | RH |
| A-20 | Contrôle du solde à la soumission | Bloquant à la soumission (solde disponible) **et** à l'approbation (RB-09, RB-10). | Faible | RH |
| A-21 | Un employé peut-il appartenir à plusieurs équipes ? | Non, exactement une (RB-15). | Moyen : impacte RB-16 et RB-20 | Direction |
| A-22 | Notifications aux Admins RH en repli | Email à **tous** les Admins RH actifs sauf le demandeur ; le premier qui décide clôt la demande. | Faible | RH |

### 9.2 Risques

| ID | Risque | Prob. | Impact | Mitigation |
|---|---|---|---|---|
| R-01 | Interprétation de la règle des 50 % non conforme à l'attente (A-02) | Moyenne | Moyen | Calcul isolé et testé par des cas chiffrés (RB-20), seuil configurable, revue avec 2 managers avant mise en production |
| R-02 | Non-délivrabilité des emails (SPF/DKIM, filtrage anti-spam) | Moyenne | Moyen | Outbox avec nouvelles tentatives (RB-26), relais SMTP authentifié, métrique sur la taille de la file, file de validation consultable dans l'application en secours |
| R-03 | Soldes initiaux erronés lors du démarrage | Élevée | Élevé | Période de double saisie d'un mois, export de contrôle, ajustements tracés (RB-31) |
| R-04 | Exposition de données de santé (type MALADIE) | Faible | Élevé | RB-29, ENF-SEC-08, tests d'autorisation dédiés à la non-divulgation |
| R-05 | Approbations concurrentes conduisant à un solde incohérent | Faible | Élevé | Transaction atomique, concurrence optimiste (RB-19), invariant testé (section 6.3) |
| R-06 | Mauvais routage des validations après réorganisation | Moyenne | Moyen | Valideur calculé dynamiquement (RB-16), repli RH garanti, contrôle « dernier Admin RH » (RB-32) |
| R-07 | Dépendance à une seule personne RH | Moyenne | Moyen | Recommandation d'au moins 2 Admins RH. RB-16 empêche l'auto-validation. |
| R-08 | Gestion d'identité locale moins robuste qu'un SSO | Moyenne | Moyen | ENF-SEC-02 à ENF-SEC-04 ; SSO en évolution (PO-05) |
| R-09 | Fuseau horaire ou décalage de date (UTC vs Europe/Paris) produisant des erreurs d'un jour | Moyenne | Moyen | Dates sans heure de bout en bout (RB-01), horloge injectée, tests aux bornes de minuit |

### 9.3 Points ouverts à trancher

| ID | Point | Échéance | Recommandation provisoire |
|---|---|---|---|
| PO-01 | Choix SQL Server ou PostgreSQL | Étape 2 | Justification attendue sur : coût de licence, hébergement cible, support des types `date` et `numeric`, concurrence optimiste, maturité du fournisseur EF Core |
| PO-02 | Hébergement (on-premise, cloud UE, conteneurs) | Étape 2 | Conteneurs sur un hébergeur UE |
| PO-03 | Fournisseur SMTP | Étape 2 | Relais SMTP de l'entreprise ou service transactionnel UE ; serveur SMTP de test local en développement |
| PO-04 | Algorithme de signature JWT et gestion des clés | Étape 2 | Signature asymétrique si d'autres services doivent vérifier les jetons, sinon symétrique avec clé ≥ 256 bits en coffre à secrets |
| PO-05 | Passage au SSO (Entra ID / Google Workspace) | Après la V1 | Prévoir dans l'architecture une abstraction du fournisseur d'identité |
| PO-06 | Approbation automatique de certains types (MALADIE) | Arbitrage RH avant l'étape 3 | Ajout possible d'un attribut de type sans impact structurel |
| PO-07 | Demi-journées | Arbitrage RH (V2) | Modèle de soldes déjà décimal |