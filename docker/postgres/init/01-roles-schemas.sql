-- =============================================================================
--  AGENTICS WMS — BAZANI BIRINCHI MARTA TAYYORLASH (Wash/HRM naqshi)
--
--  `docker-entrypoint-initdb.d` orqali FAQAT bo'sh data-volume'da bir marta yuradi.
--  Idempotent — qo'lda qayta yurgizish xavfsiz. JADVAL YARATMAYDI: jadvallar va RLS
--  siyosatlari EF Core migratsiyalaridan keladi (`WMS.API migrate`, app_migrator bilan).
--
--  Kontekst: superuser (POSTGRES_USER). Muhit: APP_USER_PASSWORD, APP_MIGRATOR_PASSWORD.
-- =============================================================================

\set ON_ERROR_STOP on

\set app_user_password 'CHANGE_ME_app_user'
\set app_migrator_password 'CHANGE_ME_app_migrator'
\getenv app_user_password APP_USER_PASSWORD
\getenv app_migrator_password APP_MIGRATOR_PASSWORD

-- ---------------------------------------------------------------------------
--  1. KENGAYTMALAR
-- ---------------------------------------------------------------------------
CREATE EXTENSION IF NOT EXISTS pgcrypto;   -- gen_random_uuid
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;    -- mahsulot/kontragent nomi bo'yicha qisman qidiruv

-- ---------------------------------------------------------------------------
--  2. ROLLAR — ikki rol modeli (RLS asosi)
--     app_migrator — DDL egasi, faqat migratsiya paytida.
--     app_user     — ilova roli, faqat DML, NOBYPASSRLS (eng muhim qator).
-- ---------------------------------------------------------------------------
SELECT format(
         'CREATE ROLE app_migrator LOGIN PASSWORD %L '
         'NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION INHERIT',
         :'app_migrator_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_migrator')
\gexec

SELECT format('ALTER ROLE app_migrator PASSWORD %L', :'app_migrator_password')
\gexec

SELECT format(
         'CREATE ROLE app_user LOGIN PASSWORD %L '
         'NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT',
         :'app_user_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_user')
\gexec

SELECT format('ALTER ROLE app_user PASSWORD %L', :'app_user_password')
\gexec

ALTER ROLE app_user NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
ALTER ROLE app_migrator NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;

COMMENT ON ROLE app_user IS
  'Ilova roli. Faqat DML. BYPASSRLS YO''Q — tenant izolyatsiyasi RLS bilan majburlanadi.';
COMMENT ON ROLE app_migrator IS
  'Migratsiya/DDL roli. Sxema va jadvallar egasi. Ilova bu rol bilan ulanmaydi.';

-- ---------------------------------------------------------------------------
--  3. SXEMA (bitta baza: agentics_wms, bitta sxema: wms)
-- ---------------------------------------------------------------------------
CREATE SCHEMA IF NOT EXISTS wms AUTHORIZATION app_migrator;
COMMENT ON SCHEMA wms IS 'Agentics WMS domeni. Tenant jadvallarida tenant_id + RLS.';

-- ---------------------------------------------------------------------------
--  4. HUQUQLAR
-- ---------------------------------------------------------------------------
SELECT format('GRANT CONNECT ON DATABASE %I TO app_user, app_migrator', current_database())
\gexec

REVOKE CREATE ON SCHEMA public FROM PUBLIC;

GRANT USAGE         ON SCHEMA wms TO app_user;
GRANT USAGE, CREATE ON SCHEMA wms TO app_migrator;

GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES    IN SCHEMA wms TO app_user;
GRANT USAGE, SELECT                  ON ALL SEQUENCES IN SCHEMA wms TO app_user;

-- app_migrator yaratadigan kelajak obyektlari — app_user avtomatik DML oladi.
ALTER DEFAULT PRIVILEGES FOR ROLE app_migrator IN SCHEMA wms
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO app_user;
ALTER DEFAULT PRIVILEGES FOR ROLE app_migrator IN SCHEMA wms
  GRANT USAGE, SELECT ON SEQUENCES TO app_user;

-- wms.audit_log — APPEND-ONLY. Asosiy joy — WmsDatabaseMigrator (har migratsiyadan keyin).
DO
$$
BEGIN
    IF to_regclass('wms.audit_log') IS NOT NULL THEN
        REVOKE UPDATE, DELETE, TRUNCATE ON wms.audit_log FROM app_user;
    END IF;
END
$$;

-- ---------------------------------------------------------------------------
--  5. BAZA SOZLAMALARI
-- ---------------------------------------------------------------------------
SELECT format('ALTER DATABASE %I SET timezone TO ''UTC''', current_database())
\gexec
SELECT format('ALTER DATABASE %I SET log_min_duration_statement TO 1000', current_database())
\gexec

DO $$
BEGIN
  RAISE NOTICE '---------------------------------------------------------------';
  RAISE NOTICE ' Agentics WMS: baza tayyorlandi';
  RAISE NOTICE '   sxema  : wms';
  RAISE NOTICE '   rollar : app_migrator (DDL egasi), app_user (DML, NOBYPASSRLS)';
  RAISE NOTICE '   keyingi: wms-migrator (EF migratsiya + RLS siyosatlari)';
  RAISE NOTICE '---------------------------------------------------------------';
END
$$;
