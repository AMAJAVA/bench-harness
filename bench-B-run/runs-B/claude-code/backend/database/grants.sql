-- =====================================================================
-- TeamLeave — droits de l'identité applicative (step2_arch §4.3)
-- Exécuté par le pipeline avec l'identité de déploiement, jamais par l'application.
-- =====================================================================

-- L'application se connecte avec l'identité managée de l'App Service (ENF-S8)
CREATE USER [app-teamleave-prd] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [app-teamleave-prd];
ALTER ROLE db_datawriter ADD MEMBER [app-teamleave-prd];

-- Journaux en ajout seul (RB-25, RB-26)
DENY UPDATE, DELETE ON dbo.BalanceMovements    TO [app-teamleave-prd];
DENY UPDATE, DELETE ON dbo.LeaveRequestHistory TO [app-teamleave-prd];
DENY UPDATE, DELETE ON dbo.AuditLog            TO [app-teamleave-prd];

-- Aucune suppression physique (RB-27)
DENY DELETE ON dbo.Users         TO [app-teamleave-prd];
DENY DELETE ON dbo.LeaveRequests TO [app-teamleave-prd];

-- Écart assumé avec §4.3 (voir step3_backend.md, décision D-07) : pas de DENY DELETE sur dbo.Balances.
-- La suppression d'un type de congé jamais utilisé (CA-15.3) supprime aussi les soldes à 0 sans mouvement
-- créés d'office pour ce type (GAP-15). Un solde ayant au moins un mouvement n'est jamais supprimé
-- (contrôle applicatif + clé étrangère FK_BalanceMovements_Balances).
