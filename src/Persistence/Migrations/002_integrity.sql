-- Additive integrity enforcement. Existing invalid data causes rollback, never deletion.
CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE winery.batch_vessel_occupancy
  ADD CONSTRAINT vessel_occupancy_no_overlap
  EXCLUDE USING gist (
    workspace_id WITH =,
    vessel_id WITH =,
    tstzrange(valid_from,valid_to,'[)') WITH &&
  );

ALTER TABLE vineyard.operations
  ADD CONSTRAINT vineyard_operation_workspace_identity UNIQUE(workspace_id,id),
  DROP CONSTRAINT operations_corrects_operation_id_fkey,
  ADD CONSTRAINT vineyard_correction_same_workspace
    FOREIGN KEY(workspace_id,corrects_operation_id) REFERENCES vineyard.operations(workspace_id,id),
  ADD CONSTRAINT vineyard_correction_not_self CHECK(corrects_operation_id IS NULL OR corrects_operation_id<>id);

ALTER TABLE winery.operations
  ADD CONSTRAINT winery_operation_workspace_identity UNIQUE(workspace_id,id),
  DROP CONSTRAINT operations_corrects_operation_id_fkey,
  ADD CONSTRAINT winery_correction_same_workspace
    FOREIGN KEY(workspace_id,corrects_operation_id) REFERENCES winery.operations(workspace_id,id),
  ADD CONSTRAINT winery_correction_not_self CHECK(corrects_operation_id IS NULL OR corrects_operation_id<>id);

ALTER TABLE observations.measurements
  ADD CONSTRAINT observation_workspace_identity UNIQUE(workspace_id,id),
  DROP CONSTRAINT measurements_corrected_measurement_id_fkey,
  ADD CONSTRAINT observation_correction_same_workspace
    FOREIGN KEY(workspace_id,corrected_measurement_id) REFERENCES observations.measurements(workspace_id,id),
  ADD CONSTRAINT observation_correction_not_self CHECK(corrected_measurement_id IS NULL OR corrected_measurement_id<>id);

-- Stored rows are versioned publications, not editable drafts. Active selection
-- can change while the payload referenced by historical runs remains immutable.
CREATE FUNCTION decisions.guard_definition_version() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP='DELETE' THEN
    RAISE EXCEPTION USING ERRCODE='23514', MESSAGE='Versioned prediction definitions cannot be deleted; retire using active=false';
  END IF;
  IF ROW(NEW.code,NEW.version,NEW.model_kind,NEW.input_dependencies,NEW.materiality_policy,NEW.output_contract)
     IS DISTINCT FROM ROW(OLD.code,OLD.version,OLD.model_kind,OLD.input_dependencies,OLD.materiality_policy,OLD.output_contract)
  THEN
    RAISE EXCEPTION USING ERRCODE='23514', MESSAGE='Versioned prediction payload is immutable; publish a new version';
  END IF;
  RETURN NEW;
END;
$$;
CREATE TRIGGER prediction_definition_version_immutable
  BEFORE UPDATE OR DELETE ON decisions.prediction_definitions
  FOR EACH ROW EXECUTE FUNCTION decisions.guard_definition_version();
