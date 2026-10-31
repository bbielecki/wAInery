-- wAInery LLD v1 — reference PostgreSQL 16+ schema. Reviewed design blueprint,
-- NOT yet applied or integration-tested against a live PostgreSQL instance.
-- Run once using a dedicated migration owner; use EF migrations for later changes.
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS vector;
CREATE SCHEMA IF NOT EXISTS registry;
CREATE SCHEMA IF NOT EXISTS connectivity;
CREATE SCHEMA IF NOT EXISTS vineyard;
CREATE SCHEMA IF NOT EXISTS winery;
CREATE SCHEMA IF NOT EXISTS observations;
CREATE SCHEMA IF NOT EXISTS weather;
CREATE SCHEMA IF NOT EXISTS knowledge;
CREATE SCHEMA IF NOT EXISTS decisions;
CREATE SCHEMA IF NOT EXISTS ai_execution;
CREATE SCHEMA IF NOT EXISTS processing;

CREATE TABLE registry.workspaces (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  display_name text NOT NULL, created_at timestamptz NOT NULL DEFAULT now(),
  CHECK (char_length(display_name) BETWEEN 1 AND 200)
);
CREATE TABLE registry.sources (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  source_kind text NOT NULL, display_name text NOT NULL, adapter_kind text NOT NULL,
  status text NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','DEGRADED','DISABLED')),
  rights_class text NOT NULL CHECK (rights_class IN ('PRIVATE_COPYRIGHT','PRIVATE_DERIVED','PRIVATE_OPERATIONAL','PUBLIC_LICENSED','PUBLIC_REFERENCE')),
  external_ai_policy text NOT NULL DEFAULT 'DENY' CHECK (external_ai_policy IN ('DENY','REVIEW','ALLOW')),
  config jsonb NOT NULL DEFAULT '{}'::jsonb, version bigint NOT NULL DEFAULT 1,
  created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(workspace_id,id)
);
CREATE TABLE registry.measurement_types (
  code text PRIMARY KEY, canonical_unit text NOT NULL, value_kind text NOT NULL
     CHECK (value_kind IN ('NUMERIC','TEXT','BOOLEAN','CATEGORICAL')),
  description text NOT NULL, permitted_subject_kinds text[] NOT NULL DEFAULT '{}',
  validation jsonb NOT NULL DEFAULT '{}'::jsonb, definition_version integer NOT NULL DEFAULT 1,
  active boolean NOT NULL DEFAULT true
);
CREATE TABLE connectivity.source_receipts (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  source_id uuid NOT NULL, external_key text NOT NULL, content_sha256 char(64), raw_object_key text,
  received_at timestamptz NOT NULL DEFAULT now(), status text NOT NULL
    CHECK (status IN ('RECEIVED','PARSED','PROPOSED','ACCEPTED','REJECTED','FAILED')),
  mapping_version text, error_code text, UNIQUE(workspace_id,source_id,external_key),
  FOREIGN KEY (workspace_id,source_id) REFERENCES registry.sources(workspace_id,id)
);
CREATE TABLE connectivity.operation_proposals (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  receipt_id uuid REFERENCES connectivity.source_receipts(id), target_module text NOT NULL,
  proposed_subject_kind text, proposed_subject_id uuid, proposed_type text,
  extracted_data jsonb NOT NULL, evidence_refs jsonb NOT NULL DEFAULT '[]'::jsonb,
  status text NOT NULL DEFAULT 'PENDING' CHECK(status IN ('PENDING','CONFIRMED','REJECTED')),
  confirmed_at timestamptz, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE vineyard.blocks (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  name text NOT NULL, location jsonb NOT NULL DEFAULT '{}'::jsonb,
  soil_notes text, area_m2 numeric(12,2), version bigint NOT NULL DEFAULT 1,
  UNIQUE(workspace_id,id), UNIQUE(workspace_id,name)
);
CREATE TABLE vineyard.block_varieties (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL,
  block_id uuid NOT NULL, variety_name text NOT NULL, vine_count integer,
  planted_year integer, attributes jsonb NOT NULL DEFAULT '{}'::jsonb,
  FOREIGN KEY (workspace_id,block_id) REFERENCES vineyard.blocks(workspace_id,id),
  CHECK(vine_count IS NULL OR vine_count >= 0)
);
CREATE TABLE vineyard.phenology (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL,
  block_id uuid NOT NULL, phase_code text NOT NULL, observed_at timestamptz NOT NULL,
  received_at timestamptz NOT NULL DEFAULT now(), confidence text NOT NULL CHECK(confidence IN ('OBSERVED','ESTIMATED','UNKNOWN')),
  source_id uuid, version bigint NOT NULL DEFAULT 1,
  FOREIGN KEY (workspace_id,block_id) REFERENCES vineyard.blocks(workspace_id,id)
);
CREATE TABLE vineyard.operations (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL,
  block_id uuid NOT NULL, operation_type text NOT NULL, notes text,
  performed_at timestamptz NOT NULL, recorded_at timestamptz NOT NULL DEFAULT now(),
  source_id uuid, source_external_key text, confirmed_by text NOT NULL,
  corrects_operation_id uuid, attributes jsonb NOT NULL DEFAULT '{}'::jsonb,
  FOREIGN KEY (workspace_id,block_id) REFERENCES vineyard.blocks(workspace_id,id),
  FOREIGN KEY (corrects_operation_id) REFERENCES vineyard.operations(id),
  UNIQUE(workspace_id,source_id,source_external_key)
);
CREATE TABLE winery.vessels (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  name text NOT NULL, capacity_l numeric(12,3), type text NOT NULL, version bigint NOT NULL DEFAULT 1,
  UNIQUE(workspace_id,id), UNIQUE(workspace_id,name), CHECK(capacity_l IS NULL OR capacity_l > 0)
);
CREATE TABLE winery.batches (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  code text NOT NULL, wine_style text, vintage integer, state text NOT NULL,
  attributes jsonb NOT NULL DEFAULT '{}'::jsonb, version bigint NOT NULL DEFAULT 1,
  UNIQUE(workspace_id,id), UNIQUE(workspace_id,code)
);
CREATE TABLE winery.batch_vessel_occupancy (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL,
  batch_id uuid NOT NULL, vessel_id uuid NOT NULL, volume_l numeric(12,3),
  valid_from timestamptz NOT NULL, valid_to timestamptz,
  FOREIGN KEY (workspace_id,batch_id) REFERENCES winery.batches(workspace_id,id),
  FOREIGN KEY (workspace_id,vessel_id) REFERENCES winery.vessels(workspace_id,id),
  CHECK(valid_to IS NULL OR valid_to > valid_from), CHECK(volume_l IS NULL OR volume_l > 0)
);
CREATE TABLE winery.operations (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL,
  batch_id uuid NOT NULL, operation_type text NOT NULL, notes text,
  performed_at timestamptz NOT NULL, recorded_at timestamptz NOT NULL DEFAULT now(),
  source_id uuid, source_external_key text, confirmed_by text NOT NULL,
  attributes jsonb NOT NULL DEFAULT '{}'::jsonb, corrects_operation_id uuid,
  FOREIGN KEY (workspace_id,batch_id) REFERENCES winery.batches(workspace_id,id),
  FOREIGN KEY (corrects_operation_id) REFERENCES winery.operations(id),
  UNIQUE(workspace_id,source_id,source_external_key)
);
CREATE TABLE observations.measurements (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  subject_module text NOT NULL, subject_kind text NOT NULL, subject_id uuid NOT NULL,
  measurement_type text NOT NULL REFERENCES registry.measurement_types(code),
  numeric_value numeric(20,8), text_value text, bool_value boolean,
  unit text NOT NULL, observed_at timestamptz NOT NULL, received_at timestamptz NOT NULL DEFAULT now(),
  source_id uuid NOT NULL, source_external_key text NOT NULL, quality text NOT NULL
      CHECK(quality IN ('VALID','SUSPECT','INVALID','UNCALIBRATED','MISSING')),
  method text, calibration_ref text, attributes jsonb NOT NULL DEFAULT '{}'::jsonb,
  corrected_measurement_id uuid REFERENCES observations.measurements(id),
  UNIQUE(workspace_id,source_id,source_external_key),
  CHECK ((numeric_value IS NOT NULL)::integer+(text_value IS NOT NULL)::integer+(bool_value IS NOT NULL)::integer = 1)
);
CREATE INDEX observations_subject_time_idx ON observations.measurements
  (workspace_id,subject_module,subject_kind,subject_id,measurement_type,observed_at DESC);
CREATE TABLE weather.locations (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  name text NOT NULL, latitude numeric(9,6) NOT NULL, longitude numeric(9,6) NOT NULL,
  elevation_m numeric(9,2), timezone text NOT NULL DEFAULT 'Europe/Warsaw',
  UNIQUE(workspace_id,id), CHECK(latitude BETWEEN -90 AND 90), CHECK(longitude BETWEEN -180 AND 180)
);
CREATE TABLE weather.forecast_runs (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL,
  location_id uuid NOT NULL, source_id uuid NOT NULL, issued_at timestamptz NOT NULL,
  retrieved_at timestamptz NOT NULL DEFAULT now(), provider_run_id text,
  payload_sha256 char(64), quality text NOT NULL DEFAULT 'VALID',
  FOREIGN KEY (workspace_id,location_id) REFERENCES weather.locations(workspace_id,id),
  UNIQUE(workspace_id,id)
);
CREATE TABLE weather.forecast_points (
  forecast_run_id uuid NOT NULL REFERENCES weather.forecast_runs(id) ON DELETE CASCADE,
  valid_at timestamptz NOT NULL, variable_code text NOT NULL,
  value numeric(20,8), unit text NOT NULL, attributes jsonb NOT NULL DEFAULT '{}'::jsonb,
  PRIMARY KEY(forecast_run_id,valid_at,variable_code)
);
CREATE INDEX weather_runs_lookup_idx ON weather.forecast_runs(workspace_id,location_id,issued_at DESC);
CREATE TABLE knowledge.documents (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  source_id uuid NOT NULL, title text NOT NULL, document_kind text NOT NULL,
  rights_class text NOT NULL, external_ai_policy text NOT NULL DEFAULT 'DENY',
  created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(workspace_id,id)
);
CREATE TABLE knowledge.document_versions (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL,
  document_id uuid NOT NULL, version integer NOT NULL, raw_object_key text NOT NULL,
  content_sha256 char(64) NOT NULL, parser_version text NOT NULL, ingestion_status text NOT NULL,
  published_at timestamptz, metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
  FOREIGN KEY (workspace_id,document_id) REFERENCES knowledge.documents(workspace_id,id),
  UNIQUE(document_id,version), UNIQUE(workspace_id,id)
);
CREATE TABLE knowledge.chunks (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL,
  document_version_id uuid NOT NULL, ordinal integer NOT NULL, section_path text,
  page_start integer, page_end integer, chunk_text text NOT NULL,
  topics text[] NOT NULL DEFAULT '{}', variety_tags text[] NOT NULL DEFAULT '{}',
  language text, lexical tsvector GENERATED ALWAYS AS (to_tsvector('simple',chunk_text)) STORED,
  FOREIGN KEY (workspace_id,document_version_id) REFERENCES knowledge.document_versions(workspace_id,id),
  UNIQUE(document_version_id,ordinal), UNIQUE(workspace_id,id),
  CHECK(page_end IS NULL OR page_start IS NULL OR page_end>=page_start)
);
CREATE INDEX knowledge_chunk_lexical_idx ON knowledge.chunks USING gin(lexical);
CREATE INDEX knowledge_chunk_tags_idx ON knowledge.chunks USING gin(topics);
CREATE TABLE knowledge.embedding_spaces (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), model_id text NOT NULL, revision text NOT NULL,
  dimensions integer NOT NULL CHECK(dimensions BETWEEN 1 AND 4096), normalizer text NOT NULL,
  UNIQUE(model_id,revision,dimensions,normalizer)
);
CREATE TABLE knowledge.chunk_embeddings (
  chunk_id uuid NOT NULL REFERENCES knowledge.chunks(id) ON DELETE CASCADE,
  embedding_space_id uuid NOT NULL REFERENCES knowledge.embedding_spaces(id),
  embedding vector NOT NULL, created_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY(chunk_id,embedding_space_id)
);
-- Exact vector search in MVP; create model/dimension-specific ANN expression index only after benchmarks.
CREATE TABLE knowledge.index_publications (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  index_version text NOT NULL, embedding_space_id uuid REFERENCES knowledge.embedding_spaces(id),
  published_at timestamptz NOT NULL DEFAULT now(), status text NOT NULL CHECK(status IN ('ACTIVE','RETIRED')),
  UNIQUE(workspace_id,index_version)
);
CREATE TABLE decisions.prediction_definitions (
  code text PRIMARY KEY, version text NOT NULL, model_kind text NOT NULL,
  input_dependencies jsonb NOT NULL, materiality_policy jsonb NOT NULL,
  output_contract text NOT NULL, active boolean NOT NULL DEFAULT true
);
CREATE TABLE decisions.prediction_states (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  prediction_code text NOT NULL REFERENCES decisions.prediction_definitions(code),
  subject_module text NOT NULL, subject_kind text NOT NULL, subject_id uuid NOT NULL,
  horizon_start timestamptz NOT NULL, horizon_end timestamptz NOT NULL,
  status text NOT NULL CHECK(status IN ('CURRENT','STALE','RECOMPUTING','FAILED','INSUFFICIENT_DATA')),
  input_revision bigint NOT NULL DEFAULT 0, published_revision bigint NOT NULL DEFAULT 0,
  last_run_at timestamptz, valid_until timestamptz, last_result jsonb,
  UNIQUE(workspace_id,prediction_code,subject_module,subject_kind,subject_id,horizon_start,horizon_end),
  CHECK(horizon_end>horizon_start)
);
CREATE TABLE decisions.prediction_runs (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), prediction_state_id uuid NOT NULL REFERENCES decisions.prediction_states(id),
  requested_revision bigint NOT NULL, started_at timestamptz NOT NULL,
  finished_at timestamptz, status text NOT NULL, input_manifest jsonb NOT NULL,
  model_version text, probability_metadata jsonb, outcome jsonb, error_code text
);
CREATE TABLE decisions.decision_plans (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  subject_module text NOT NULL, subject_kind text NOT NULL, subject_id uuid NOT NULL,
  current_revision integer NOT NULL DEFAULT 0,
  UNIQUE(workspace_id,subject_module,subject_kind,subject_id)
);
CREATE TABLE decisions.plan_revisions (
  plan_id uuid NOT NULL REFERENCES decisions.decision_plans(id), revision integer NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(), as_of timestamptz NOT NULL,
  status text NOT NULL, input_manifest jsonb NOT NULL, plan jsonb NOT NULL,
  PRIMARY KEY(plan_id,revision)
);
CREATE TABLE decisions.decision_feedback (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plan_id uuid NOT NULL,
  revision integer NOT NULL, action_id text NOT NULL, feedback_type text NOT NULL,
  noted_at timestamptz NOT NULL DEFAULT now(), notes text,
  FOREIGN KEY(plan_id,revision) REFERENCES decisions.plan_revisions(plan_id,revision)
);
CREATE TABLE ai_execution.model_usage (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  request_id uuid NOT NULL, provider text NOT NULL, model text NOT NULL,
  input_tokens integer NOT NULL DEFAULT 0, output_tokens integer NOT NULL DEFAULT 0,
  cost_usd numeric(14,8) NOT NULL DEFAULT 0, happened_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE(workspace_id,request_id), CHECK(input_tokens>=0 AND output_tokens>=0 AND cost_usd>=0)
);
CREATE TABLE processing.outbox (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  producer text NOT NULL, aggregate_kind text NOT NULL, aggregate_id uuid NOT NULL,
  aggregate_version bigint NOT NULL, event_type text NOT NULL, schema_version integer NOT NULL,
  subject jsonb NOT NULL, changed_fields text[] NOT NULL DEFAULT '{}',
  data_ref text NOT NULL, payload jsonb NOT NULL DEFAULT '{}'::jsonb,
  recorded_at timestamptz NOT NULL DEFAULT now(), available_at timestamptz NOT NULL DEFAULT now(),
  delivered_at timestamptz, attempts integer NOT NULL DEFAULT 0, last_error text,
  UNIQUE(workspace_id,producer,aggregate_kind,aggregate_id,aggregate_version,event_type)
);
CREATE INDEX processing_outbox_ready_idx ON processing.outbox(available_at,id) WHERE delivered_at IS NULL;
CREATE TABLE processing.inbox_dedupe (
  consumer text NOT NULL, event_id uuid NOT NULL REFERENCES processing.outbox(id),
  processed_at timestamptz NOT NULL DEFAULT now(), PRIMARY KEY(consumer,event_id)
);
CREATE TABLE processing.jobs (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), workspace_id uuid NOT NULL REFERENCES registry.workspaces(id),
  job_kind text NOT NULL, scope_key text NOT NULL, desired_revision bigint NOT NULL DEFAULT 0,
  status text NOT NULL CHECK(status IN ('PENDING','RUNNING','SUCCEEDED','RETRY','DEAD')),
  priority integer NOT NULL DEFAULT 100, not_before timestamptz NOT NULL DEFAULT now(),
  lease_owner text, lease_until timestamptz, attempts integer NOT NULL DEFAULT 0,
  error_code text, updated_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE(workspace_id,job_kind,scope_key)
);
CREATE INDEX processing_jobs_claim_idx ON processing.jobs(priority,not_before) WHERE status IN ('PENDING','RETRY');
