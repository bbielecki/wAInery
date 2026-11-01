-- Fencing metadata; do not manufacture tokens or repair inconsistent old leases.
ALTER TABLE processing.jobs
  ADD COLUMN lease_token uuid,
  ADD COLUMN claimed_revision bigint,
  ADD CONSTRAINT jobs_nonnegative_revision CHECK(desired_revision>=0),
  ADD CONSTRAINT jobs_nonnegative_attempts CHECK(attempts>=0),
  ADD CONSTRAINT jobs_valid_lease CHECK(
    (status='RUNNING' AND lease_owner IS NOT NULL AND lease_until IS NOT NULL
      AND lease_token IS NOT NULL AND claimed_revision IS NOT NULL
      AND claimed_revision>=0 AND claimed_revision<=desired_revision)
    OR
    (status<>'RUNNING' AND lease_owner IS NULL AND lease_until IS NULL
      AND lease_token IS NULL AND claimed_revision IS NULL)
  );
CREATE INDEX processing_jobs_expired_lease_idx ON processing.jobs(lease_until,id) WHERE status='RUNNING';
