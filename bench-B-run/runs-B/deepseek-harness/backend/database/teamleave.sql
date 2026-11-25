-- ============================================================================
-- TeamLeave — schéma PostgreSQL 17 (référence physique)
-- Appliquer avec : psql -U teamleave -d teamleave -f database/teamleave.sql
-- Le script est idempotent pour les données de référence, pas pour les tables.
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS btree_gist;

CREATE TABLE IF NOT EXISTS teams (
    id          uuid        PRIMARY KEY,
    name        citext      NOT NULL,
    is_active   boolean     NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_teams_name UNIQUE (name),
    CONSTRAINT ck_teams_name CHECK (btrim(name::text) <> '')
);

CREATE TABLE IF NOT EXISTS users (
    id                   uuid        PRIMARY KEY,
    email                citext      NOT NULL,
    first_name           text        NOT NULL,
    last_name            text        NOT NULL,
    password_hash        text        NOT NULL,
    roles                text[]      NOT NULL DEFAULT ARRAY['EMPLOYE']::text[],
    team_id              uuid        NOT NULL REFERENCES teams(id) ON DELETE RESTRICT,
    manager_id           uuid        NULL REFERENCES users(id) ON DELETE RESTRICT,
    is_active            boolean     NOT NULL DEFAULT true,
    must_change_password boolean     NOT NULL DEFAULT true,
    security_stamp       uuid        NOT NULL,
    failed_login_count   integer     NOT NULL DEFAULT 0,
    lockout_until        timestamptz NULL,
    last_login_at        timestamptz NULL,
    created_at           timestamptz NOT NULL DEFAULT now(),
    updated_at           timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_users_email UNIQUE (email),
    CONSTRAINT ck_users_roles CHECK (
        cardinality(roles) >= 1
        AND roles <@ ARRAY['EMPLOYE','MANAGER','ADMIN_RH']::text[]
        AND 'EMPLOYE' = ANY (roles)
    ),
    CONSTRAINT ck_users_manager_not_self CHECK (manager_id IS NULL OR manager_id <> id),
    CONSTRAINT ck_users_names CHECK (btrim(first_name) <> '' AND btrim(last_name) <> ''),
    CONSTRAINT ck_users_failed_logins CHECK (failed_login_count >= 0)
);

CREATE TABLE IF NOT EXISTS leave_types (
    id          uuid        PRIMARY KEY,
    code        citext      NOT NULL,
    label       text        NOT NULL,
    color       varchar(7)  NOT NULL,
    is_active   boolean     NOT NULL DEFAULT true,
    is_system   boolean     NOT NULL DEFAULT false,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_leave_types_code UNIQUE (code),
    CONSTRAINT ck_leave_types_code CHECK (
        code::text = upper(code::text)
        AND code::text ~ '^[A-Z][A-Z0-9_]{1,31}$'
    ),
    CONSTRAINT ck_leave_types_label CHECK (btrim(label) <> '' AND char_length(label) <= 100),
    CONSTRAINT ck_leave_types_color CHECK (color ~ '^#[0-9A-Fa-f]{6}$')
);

CREATE TABLE IF NOT EXISTS annual_balances (
    employee_id        uuid        NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    leave_type_id      uuid        NOT NULL REFERENCES leave_types(id) ON DELETE RESTRICT,
    year               integer     NOT NULL,
    granted_half_days  integer     NOT NULL DEFAULT 0,
    consumed_half_days integer     NOT NULL DEFAULT 0,
    version            bigint      NOT NULL DEFAULT 0,
    created_at         timestamptz NOT NULL DEFAULT now(),
    updated_at         timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_annual_balances PRIMARY KEY (employee_id, leave_type_id, year),
    CONSTRAINT ck_balances_granted CHECK (granted_half_days >= 0),
    CONSTRAINT ck_balances_consumed CHECK (consumed_half_days >= 0),
    CONSTRAINT ck_balances_available CHECK (consumed_half_days <= granted_half_days),
    CONSTRAINT ck_balances_year CHECK (year BETWEEN 2000 AND 2100)
);

CREATE TABLE IF NOT EXISTS leave_requests (
    id                   uuid        PRIMARY KEY,
    employee_id          uuid        NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    leave_type_id        uuid        NOT NULL REFERENCES leave_types(id) ON DELETE RESTRICT,
    start_date           date        NOT NULL,
    start_half_day       varchar(2)  NOT NULL,
    end_date             date        NOT NULL,
    end_half_day         varchar(2)  NOT NULL,
    total_half_days      integer     NOT NULL,
    status               varchar(16) NOT NULL DEFAULT 'PENDING',
    comment              text        NULL,
    submitted_at         timestamptz NOT NULL DEFAULT now(),
    decided_by_id        uuid        NULL REFERENCES users(id) ON DELETE RESTRICT,
    decided_at           timestamptz NULL,
    decision_comment     text        NULL,
    overlap_flagged      boolean     NOT NULL DEFAULT false,
    overlap_evaluated_at timestamptz NULL,
    acknowledged_by_id   uuid        NULL REFERENCES users(id) ON DELETE RESTRICT,
    acknowledged_at      timestamptz NULL,
    cancelled_by_id      uuid        NULL REFERENCES users(id) ON DELETE RESTRICT,
    cancelled_at         timestamptz NULL,
    cancellation_reason  varchar(64) NULL,
    version              bigint      NOT NULL DEFAULT 0,
    created_at           timestamptz NOT NULL DEFAULT now(),
    updated_at           timestamptz NOT NULL DEFAULT now(),
    period int4range GENERATED ALWAYS AS (
        int4range(
            2 * (start_date - DATE '2000-01-01')
                + CASE start_half_day WHEN 'AM' THEN 0 ELSE 1 END,
            2 * (end_date - DATE '2000-01-01')
                + CASE end_half_day WHEN 'AM' THEN 1 ELSE 2 END
        )
    ) STORED,
    CONSTRAINT ck_requests_half_days CHECK (
        start_half_day IN ('AM','PM') AND end_half_day IN ('AM','PM')
    ),
    CONSTRAINT ck_requests_weekdays CHECK (
        EXTRACT(ISODOW FROM start_date) <= 5
        AND EXTRACT(ISODOW FROM end_date) <= 5
    ),
    CONSTRAINT ck_requests_period CHECK (NOT isempty(period)),
    CONSTRAINT ck_requests_total CHECK (total_half_days > 0),
    CONSTRAINT ck_requests_comment CHECK (comment IS NULL OR char_length(comment) <= 1000),
    CONSTRAINT ck_requests_decision_comment CHECK (
        decision_comment IS NULL OR char_length(decision_comment) <= 1000
    ),
    CONSTRAINT ck_requests_status CHECK (
        status IN ('PENDING','APPROVED','REJECTED','CANCELLED')
    ),
    CONSTRAINT ck_requests_decided CHECK (
        status NOT IN ('APPROVED','REJECTED')
        OR (decided_by_id IS NOT NULL AND decided_at IS NOT NULL)
    ),
    CONSTRAINT ck_requests_rejected_comment CHECK (
        status <> 'REJECTED'
        OR (decision_comment IS NOT NULL AND btrim(decision_comment) <> '')
    ),
    CONSTRAINT ck_requests_cancelled CHECK (
        status <> 'CANCELLED'
        OR (cancelled_at IS NOT NULL AND cancelled_by_id IS NOT NULL)
    ),
    CONSTRAINT ck_requests_ack_pair CHECK (
        (acknowledged_by_id IS NULL) = (acknowledged_at IS NULL)
    ),
    CONSTRAINT ck_requests_overlap_ack CHECK (
        status <> 'APPROVED' OR NOT overlap_flagged OR acknowledged_by_id IS NOT NULL
    )
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ex_leave_requests_personal_overlap'
    ) THEN
        ALTER TABLE leave_requests
            ADD CONSTRAINT ex_leave_requests_personal_overlap
            EXCLUDE USING gist (employee_id WITH =, period WITH &&)
            WHERE (status IN ('PENDING','APPROVED'));
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS leave_request_allocations (
    leave_request_id uuid     NOT NULL REFERENCES leave_requests(id) ON DELETE RESTRICT,
    year             integer  NOT NULL,
    half_days        integer  NOT NULL,
    CONSTRAINT pk_leave_request_allocations PRIMARY KEY (leave_request_id, year),
    CONSTRAINT ck_allocations_half_days CHECK (half_days > 0),
    CONSTRAINT ck_allocations_year CHECK (year BETWEEN 2000 AND 2100)
);

CREATE TABLE IF NOT EXISTS balance_movements (
    id               uuid        PRIMARY KEY,
    employee_id      uuid        NOT NULL,
    leave_type_id    uuid        NOT NULL,
    year             integer     NOT NULL,
    delta_half_days  integer     NOT NULL,
    nature           varchar(32) NOT NULL,
    leave_request_id uuid        NULL REFERENCES leave_requests(id) ON DELETE RESTRICT,
    reason           text        NULL,
    actor_id         uuid        NULL REFERENCES users(id) ON DELETE RESTRICT,
    occurred_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_movements_balance FOREIGN KEY (employee_id, leave_type_id, year)
        REFERENCES annual_balances (employee_id, leave_type_id, year) ON DELETE RESTRICT,
    CONSTRAINT ck_movements_delta CHECK (delta_half_days <> 0),
    CONSTRAINT ck_movements_nature CHECK (
        nature IN ('OCTROI_INITIAL','AJUSTEMENT','CONSOMMATION_APPROBATION','RESTITUTION_ANNULATION')
    ),
    CONSTRAINT ck_movements_sign CHECK (
        (nature = 'OCTROI_INITIAL' AND delta_half_days > 0)
        OR (nature = 'CONSOMMATION_APPROBATION' AND delta_half_days < 0)
        OR (nature = 'RESTITUTION_ANNULATION' AND delta_half_days > 0)
        OR (nature = 'AJUSTEMENT')
    ),
    CONSTRAINT ck_movements_reason CHECK (
        nature <> 'AJUSTEMENT' OR (reason IS NOT NULL AND btrim(reason) <> '')
    ),
    CONSTRAINT ck_movements_request CHECK (
        (nature IN ('CONSOMMATION_APPROBATION','RESTITUTION_ANNULATION') AND leave_request_id IS NOT NULL)
        OR (nature IN ('OCTROI_INITIAL','AJUSTEMENT') AND leave_request_id IS NULL)
    )
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_movements_request_nature_year
    ON balance_movements (leave_request_id, nature, year)
    WHERE leave_request_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS overlap_evaluations (
    id                  bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    leave_request_id    uuid        NOT NULL REFERENCES leave_requests(id) ON DELETE RESTRICT,
    team_id             uuid        NOT NULL REFERENCES teams(id) ON DELETE RESTRICT,
    team_size           integer     NOT NULL,
    max_rate            numeric(5,4) NOT NULL,
    breaching_half_days jsonb       NOT NULL DEFAULT '[]'::jsonb,
    trigger_kind        varchar(16) NOT NULL,
    evaluated_by_id     uuid        NULL REFERENCES users(id) ON DELETE RESTRICT,
    evaluated_at        timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_overlap_team_size CHECK (team_size >= 1),
    CONSTRAINT ck_overlap_rate CHECK (max_rate >= 0 AND max_rate <= 1),
    CONSTRAINT ck_overlap_trigger CHECK (
        trigger_kind IN ('SUBMISSION','CONSULTATION','DECISION')
    )
);

CREATE TABLE IF NOT EXISTS notifications (
    id                uuid        PRIMARY KEY,
    event_key         text        NOT NULL,
    type              varchar(16) NOT NULL,
    leave_request_id  uuid        NOT NULL REFERENCES leave_requests(id) ON DELETE RESTRICT,
    recipient_user_id uuid        NULL REFERENCES users(id) ON DELETE RESTRICT,
    recipient_email   citext      NOT NULL,
    status            varchar(16) NOT NULL DEFAULT 'PENDING',
    attempts          integer     NOT NULL DEFAULT 0,
    last_error        text        NULL,
    next_attempt_at   timestamptz NOT NULL DEFAULT now(),
    created_at        timestamptz NOT NULL DEFAULT now(),
    sent_at           timestamptz NULL,
    CONSTRAINT uq_notifications_event UNIQUE (event_key),
    CONSTRAINT ck_notifications_type CHECK (type IN ('SOUMISSION','DECISION')),
    CONSTRAINT ck_notifications_status CHECK (status IN ('PENDING','SENT','FAILED')),
    CONSTRAINT ck_notifications_attempts CHECK (attempts >= 0),
    CONSTRAINT ck_notifications_error CHECK (last_error IS NULL OR char_length(last_error) <= 1000),
    CONSTRAINT ck_notifications_sent CHECK (status <> 'SENT' OR sent_at IS NOT NULL)
);

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id             uuid        PRIMARY KEY,
    user_id        uuid        NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    token_hash     bytea       NOT NULL,
    family_id      uuid        NOT NULL,
    expires_at     timestamptz NOT NULL,
    created_at     timestamptz NOT NULL DEFAULT now(),
    created_by_ip  varchar(64) NULL,
    user_agent     varchar(512) NULL,
    revoked_at     timestamptz NULL,
    revoked_reason varchar(32) NULL,
    replaced_by_id uuid        NULL REFERENCES refresh_tokens(id) ON DELETE RESTRICT,
    CONSTRAINT uq_refresh_tokens_hash UNIQUE (token_hash),
    CONSTRAINT ck_refresh_revoked CHECK ((revoked_at IS NULL) = (revoked_reason IS NULL)),
    CONSTRAINT ck_refresh_reason CHECK (
        revoked_reason IS NULL OR revoked_reason IN (
            'LOGOUT','ROTATION','REUSE_DETECTED','PASSWORD_CHANGED','ROLE_CHANGED','DEACTIVATED'
        )
    )
);

CREATE TABLE IF NOT EXISTS audit_entries (
    id             bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    actor_id       uuid        NULL REFERENCES users(id) ON DELETE RESTRICT,
    actor_type     varchar(8)  NOT NULL DEFAULT 'USER',
    action         varchar(64) NOT NULL,
    entity_type    varchar(32) NOT NULL,
    entity_id      uuid        NULL,
    occurred_at    timestamptz NOT NULL DEFAULT now(),
    before_summary jsonb       NULL,
    after_summary  jsonb       NULL,
    reason         text        NULL,
    correlation_id uuid        NULL,
    CONSTRAINT ck_audit_actor_type CHECK (actor_type IN ('USER','SYSTEM')),
    CONSTRAINT ck_audit_actor CHECK ((actor_type = 'USER') = (actor_id IS NOT NULL)),
    CONSTRAINT ck_audit_reason CHECK (reason IS NULL OR char_length(reason) <= 1000)
);

CREATE INDEX IF NOT EXISTS ix_users_team ON users (team_id);
CREATE INDEX IF NOT EXISTS ix_users_manager ON users (manager_id) WHERE manager_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_users_active ON users (is_active) WHERE is_active;
CREATE INDEX IF NOT EXISTS ix_leave_types_active ON leave_types (is_active) WHERE is_active;
CREATE INDEX IF NOT EXISTS ix_balances_employee_year ON annual_balances (employee_id, year);
CREATE INDEX IF NOT EXISTS ix_requests_employee_submitted ON leave_requests (employee_id, submitted_at DESC);
CREATE INDEX IF NOT EXISTS ix_requests_employee_status ON leave_requests (employee_id, status, submitted_at DESC);
CREATE INDEX IF NOT EXISTS ix_requests_pending ON leave_requests (submitted_at) WHERE status = 'PENDING';
CREATE INDEX IF NOT EXISTS ix_requests_approved_period ON leave_requests USING gist (period) WHERE status = 'APPROVED';
CREATE INDEX IF NOT EXISTS ix_requests_type ON leave_requests (leave_type_id);
CREATE INDEX IF NOT EXISTS ix_requests_decided_by ON leave_requests (decided_by_id) WHERE decided_by_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_allocations_year ON leave_request_allocations (year);
CREATE INDEX IF NOT EXISTS ix_movements_balance ON balance_movements (employee_id, leave_type_id, year, occurred_at);
CREATE INDEX IF NOT EXISTS ix_movements_request ON balance_movements (leave_request_id) WHERE leave_request_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_overlap_request ON overlap_evaluations (leave_request_id, evaluated_at DESC);
CREATE INDEX IF NOT EXISTS ix_notifications_due ON notifications (next_attempt_at) WHERE status = 'PENDING';
CREATE INDEX IF NOT EXISTS ix_notifications_status ON notifications (status, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_refresh_user ON refresh_tokens (user_id) WHERE revoked_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_refresh_expires ON refresh_tokens (expires_at);
CREATE INDEX IF NOT EXISTS ix_audit_occurred ON audit_entries (occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_audit_entity ON audit_entries (entity_type, entity_id, occurred_at DESC);

-- ----------------------------------------------------------------------------
-- Triggers
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_teams_updated ON teams;
CREATE TRIGGER trg_teams_updated BEFORE UPDATE ON teams
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
DROP TRIGGER IF EXISTS trg_users_updated ON users;
CREATE TRIGGER trg_users_updated BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
DROP TRIGGER IF EXISTS trg_leave_types_updated ON leave_types;
CREATE TRIGGER trg_leave_types_updated BEFORE UPDATE ON leave_types
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
DROP TRIGGER IF EXISTS trg_balances_updated ON annual_balances;
CREATE TRIGGER trg_balances_updated BEFORE UPDATE ON annual_balances
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
DROP TRIGGER IF EXISTS trg_leave_requests_updated ON leave_requests;
CREATE TRIGGER trg_leave_requests_updated BEFORE UPDATE ON leave_requests
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE OR REPLACE FUNCTION forbid_leave_type_code_change()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.code IS DISTINCT FROM OLD.code THEN
        RAISE EXCEPTION 'leave type code is immutable' USING ERRCODE = '23514';
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_leave_types_code_immutable ON leave_types;
CREATE TRIGGER trg_leave_types_code_immutable BEFORE UPDATE ON leave_types
    FOR EACH ROW EXECUTE FUNCTION forbid_leave_type_code_change();

CREATE OR REPLACE FUNCTION guard_leave_request_update()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF OLD.status IN ('REJECTED','CANCELLED') THEN
        RAISE EXCEPTION 'leave request % is terminal', OLD.id USING ERRCODE = '23514';
    END IF;

    IF OLD.status = 'APPROVED' AND NEW.status = 'APPROVED' THEN
        RAISE EXCEPTION 'approved leave request % is immutable', OLD.id USING ERRCODE = '23514';
    END IF;

    IF NEW.status IS DISTINCT FROM OLD.status
       AND NOT (
            (OLD.status = 'PENDING' AND NEW.status IN ('APPROVED','REJECTED','CANCELLED'))
         OR (OLD.status = 'APPROVED' AND NEW.status = 'CANCELLED')
       ) THEN
        RAISE EXCEPTION 'invalid leave request transition % -> %', OLD.status, NEW.status
            USING ERRCODE = '23514';
    END IF;

    IF NEW.employee_id IS DISTINCT FROM OLD.employee_id
    OR NEW.leave_type_id IS DISTINCT FROM OLD.leave_type_id
    OR NEW.start_date IS DISTINCT FROM OLD.start_date
    OR NEW.start_half_day IS DISTINCT FROM OLD.start_half_day
    OR NEW.end_date IS DISTINCT FROM OLD.end_date
    OR NEW.end_half_day IS DISTINCT FROM OLD.end_half_day
    OR NEW.total_half_days IS DISTINCT FROM OLD.total_half_days
    OR NEW.comment IS DISTINCT FROM OLD.comment
    OR NEW.submitted_at IS DISTINCT FROM OLD.submitted_at THEN
        RAISE EXCEPTION 'immutable field of leave request %', OLD.id USING ERRCODE = '23514';
    END IF;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_leave_requests_guard ON leave_requests;
CREATE TRIGGER trg_leave_requests_guard BEFORE UPDATE ON leave_requests
    FOR EACH ROW EXECUTE FUNCTION guard_leave_request_update();

CREATE OR REPLACE FUNCTION reject_mutation()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION '% is append-only', TG_TABLE_NAME USING ERRCODE = '23514';
END;
$$;

DROP TRIGGER IF EXISTS trg_audit_no_update_delete ON audit_entries;
CREATE TRIGGER trg_audit_no_update_delete BEFORE UPDATE OR DELETE ON audit_entries
    FOR EACH ROW EXECUTE FUNCTION reject_mutation();
DROP TRIGGER IF EXISTS trg_audit_no_truncate ON audit_entries;
CREATE TRIGGER trg_audit_no_truncate BEFORE TRUNCATE ON audit_entries
    FOR EACH STATEMENT EXECUTE FUNCTION reject_mutation();
DROP TRIGGER IF EXISTS trg_movements_no_update_delete ON balance_movements;
CREATE TRIGGER trg_movements_no_update_delete BEFORE UPDATE OR DELETE ON balance_movements
    FOR EACH ROW EXECUTE FUNCTION reject_mutation();
DROP TRIGGER IF EXISTS trg_movements_no_truncate ON balance_movements;
CREATE TRIGGER trg_movements_no_truncate BEFORE TRUNCATE ON balance_movements
    FOR EACH STATEMENT EXECUTE FUNCTION reject_mutation();

CREATE OR REPLACE FUNCTION assert_allocations_total(p_request_id uuid)
RETURNS void LANGUAGE plpgsql AS $$
DECLARE
    v_expected integer;
    v_sum      integer;
BEGIN
    SELECT total_half_days INTO v_expected FROM leave_requests WHERE id = p_request_id;
    IF v_expected IS NULL THEN
        RETURN;
    END IF;
    SELECT COALESCE(SUM(half_days), 0) INTO v_sum
      FROM leave_request_allocations WHERE leave_request_id = p_request_id;
    IF v_sum <> v_expected THEN
        RAISE EXCEPTION 'allocations (%) do not match request total (%) for %', v_sum, v_expected, p_request_id
            USING ERRCODE = '23514';
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION check_allocations_after_change()
RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    v_request_id uuid;
BEGIN
    v_request_id := CASE WHEN TG_OP = 'DELETE' THEN OLD.leave_request_id ELSE NEW.leave_request_id END;
    PERFORM assert_allocations_total(v_request_id);
    RETURN NULL;
END;
$$;

DROP TRIGGER IF EXISTS trg_allocations_consistency ON leave_request_allocations;
CREATE CONSTRAINT TRIGGER trg_allocations_consistency
    AFTER INSERT OR UPDATE OR DELETE ON leave_request_allocations
    DEFERRABLE INITIALLY DEFERRED
    FOR EACH ROW EXECUTE FUNCTION check_allocations_after_change();

CREATE OR REPLACE FUNCTION check_request_allocations_present()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM assert_allocations_total(NEW.id);
    RETURN NULL;
END;
$$;

DROP TRIGGER IF EXISTS trg_request_allocations_required ON leave_requests;
CREATE CONSTRAINT TRIGGER trg_request_allocations_required
    AFTER INSERT ON leave_requests
    DEFERRABLE INITIALLY DEFERRED
    FOR EACH ROW EXECUTE FUNCTION check_request_allocations_present();

-- ----------------------------------------------------------------------------
-- Données de référence
-- ----------------------------------------------------------------------------
INSERT INTO teams (id, name)
VALUES ('00000000-0000-0000-0000-000000000001', 'Direction')
ON CONFLICT (id) DO NOTHING;

INSERT INTO leave_types (id, code, label, color, is_active, is_system) VALUES
    ('00000000-0000-0000-0000-000000000101', 'CP',      'Congés payés', '#2563EB', true, true),
    ('00000000-0000-0000-0000-000000000102', 'RTT',     'RTT',           '#16A34A', true, true),
    ('00000000-0000-0000-0000-000000000103', 'MALADIE', 'Maladie',       '#DC2626', true, true),
    ('00000000-0000-0000-0000-000000000104', 'AUTRE',   'Autre',         '#6B7280', true, true)
ON CONFLICT (id) DO NOTHING;
