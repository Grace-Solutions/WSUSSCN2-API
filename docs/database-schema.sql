-- PostgreSQL schema for WSUSSCN2-API

-- API Tokens table
CREATE TABLE IF NOT EXISTS api_tokens (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  token TEXT NOT NULL UNIQUE,
  label TEXT NOT NULL,
  description TEXT,
  permissions TEXT[] NOT NULL,
  created_by TEXT,
  created_at TIMESTAMPTZ DEFAULT now(),
  last_modified_by TEXT,
  last_modified_at TIMESTAMPTZ,
  revoked BOOLEAN DEFAULT FALSE,
  revoked_at TIMESTAMPTZ,
  expires_at TIMESTAMPTZ
);

-- Updates table to store metadata from wsusscn2.cab
CREATE TABLE IF NOT EXISTS updates (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  update_id TEXT NOT NULL UNIQUE,
  title TEXT NOT NULL,
  description TEXT,
  classification TEXT,
  product TEXT,
  product_family TEXT,
  kb_article_id TEXT,
  security_bulletin_id TEXT,
  msrc_severity TEXT,
  categories TEXT[],
  is_superseded BOOLEAN DEFAULT FALSE,
  superseded_by TEXT[],
  release_date TIMESTAMPTZ,
  last_modified TIMESTAMPTZ,
  os_version TEXT,
  year INT,
  month INT,
  metadata JSONB,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

-- CAB files table to track rebuilt CABs
CREATE TABLE IF NOT EXISTS cab_files (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  file_name TEXT NOT NULL,
  group_strategy TEXT NOT NULL,
  group_value TEXT NOT NULL,
  minio_path TEXT NOT NULL,
  size_bytes BIGINT,
  update_count INT,
  created_at TIMESTAMPTZ DEFAULT now(),
  expires_at TIMESTAMPTZ
);

-- Source CAB files table to track original wsusscn2.cab files
CREATE TABLE IF NOT EXISTS source_cabs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  file_name TEXT NOT NULL,
  minio_path TEXT NOT NULL,
  etag TEXT,
  size_bytes BIGINT,
  downloaded_at TIMESTAMPTZ DEFAULT now(),
  processed BOOLEAN DEFAULT FALSE,
  processed_at TIMESTAMPTZ
);

-- Sync history table to track sync operations
CREATE TABLE IF NOT EXISTS sync_history (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  started_at TIMESTAMPTZ DEFAULT now(),
  completed_at TIMESTAMPTZ,
  status TEXT,
  source_cab_id UUID REFERENCES source_cabs(id),
  updates_added INT DEFAULT 0,
  updates_modified INT DEFAULT 0,
  error_message TEXT
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_updates_product ON updates(product);
CREATE INDEX IF NOT EXISTS idx_updates_os_version ON updates(os_version);
CREATE INDEX IF NOT EXISTS idx_updates_year_month ON updates(year, month);
CREATE INDEX IF NOT EXISTS idx_updates_release_date ON updates(release_date);
CREATE INDEX IF NOT EXISTS idx_updates_categories ON updates USING GIN(categories);
CREATE INDEX IF NOT EXISTS idx_cab_files_group ON cab_files(group_strategy, group_value);