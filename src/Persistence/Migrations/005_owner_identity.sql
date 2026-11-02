-- Owner identities/history for the first Vineyard and Weather command slices.
-- Existing rows become known at upgrade time; no invented historical timestamp.
ALTER TABLE vineyard.blocks
    ADD COLUMN public_id text,
    ADD COLUMN recorded_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    ADD COLUMN weather_location_id text,
    ALTER COLUMN area_m2 TYPE numeric;
UPDATE vineyard.blocks SET public_id=id::text;
ALTER TABLE vineyard.blocks
    ALTER COLUMN public_id SET NOT NULL,
    ALTER COLUMN public_id SET DEFAULT gen_random_uuid()::text,
    ADD UNIQUE(workspace_id,public_id),
    ADD CHECK (btrim(public_id)<>''),
    ADD CHECK (version>0);

CREATE TABLE vineyard.block_revisions (
    workspace_id uuid NOT NULL,
    block_id uuid NOT NULL,
    version bigint NOT NULL CHECK(version>0),
    recorded_at timestamptz NOT NULL,
    name text NOT NULL,
    area_m2 numeric,
    weather_location_id text,
    PRIMARY KEY(workspace_id,block_id,version),
    FOREIGN KEY(workspace_id,block_id) REFERENCES vineyard.blocks(workspace_id,id)
);
CREATE INDEX block_revisions_known_idx
    ON vineyard.block_revisions(workspace_id,block_id,recorded_at DESC,version DESC);
INSERT INTO vineyard.block_revisions(workspace_id,block_id,version,recorded_at,name,area_m2,weather_location_id)
    SELECT workspace_id,id,version,recorded_at,name,area_m2,weather_location_id FROM vineyard.blocks;
CREATE FUNCTION vineyard.protect_block_revision() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'Block revision history is immutable' USING ERRCODE='23514';
END $$;
CREATE TRIGGER block_revision_immutable BEFORE UPDATE OR DELETE ON vineyard.block_revisions
    FOR EACH ROW EXECUTE FUNCTION vineyard.protect_block_revision();

ALTER TABLE weather.locations
    ADD COLUMN public_id text,
    ADD COLUMN version bigint NOT NULL DEFAULT 1 CHECK(version>0),
    ADD COLUMN recorded_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    ALTER COLUMN latitude TYPE numeric,
    ALTER COLUMN longitude TYPE numeric,
    ALTER COLUMN elevation_m TYPE numeric;
UPDATE weather.locations SET public_id=id::text;
ALTER TABLE weather.locations
    ALTER COLUMN public_id SET NOT NULL,
    ALTER COLUMN public_id SET DEFAULT gen_random_uuid()::text,
    ADD UNIQUE(workspace_id,public_id),
    ADD CHECK(btrim(public_id)<>'');

CREATE TABLE vineyard.command_receipts (
    workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
    idempotency_key text NOT NULL CHECK(btrim(idempotency_key)<>''),
    request_sha256 char(64) NOT NULL CHECK(request_sha256 ~ '^[0-9a-f]{64}$'),
    result jsonb NOT NULL CHECK(jsonb_typeof(result)='object'),
    recorded_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    PRIMARY KEY(workspace_id,idempotency_key)
);
CREATE TABLE weather.command_receipts (
    workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
    idempotency_key text NOT NULL CHECK(btrim(idempotency_key)<>''),
    request_sha256 char(64) NOT NULL CHECK(request_sha256 ~ '^[0-9a-f]{64}$'),
    result jsonb NOT NULL CHECK(jsonb_typeof(result)='object'),
    recorded_at timestamptz NOT NULL DEFAULT transaction_timestamp(),
    PRIMARY KEY(workspace_id,idempotency_key)
);
