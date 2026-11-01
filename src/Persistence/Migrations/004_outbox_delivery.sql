ALTER TABLE processing.outbox
  ADD COLUMN dead_at timestamptz,
  ADD CONSTRAINT outbox_nonnegative_attempts CHECK(attempts>=0),
  ADD CONSTRAINT outbox_terminal_exclusive CHECK(delivered_at IS NULL OR dead_at IS NULL);
CREATE INDEX processing_outbox_dispatch_ready_idx ON processing.outbox(available_at,id)
  WHERE delivered_at IS NULL AND dead_at IS NULL;
