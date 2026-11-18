-- =============================================================================
-- CDMS - Multi-Tenant Change Data Management System
-- Database Schema for CockroachDB / PostgreSQL Cluster
-- Migration: 001_InitialSchema.sql
-- =============================================================================

-- Enable UUID extension if using standard PostgreSQL
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- 1. Tenants Table
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(256) NOT NULL,
    shopify_store_domain VARCHAR(512),
    shopify_access_token VARCHAR(512),
    vietful_tenant_id VARCHAR(128),
    vietful_access_token VARCHAR(512),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE INDEX IF NOT EXISTS ix_tenants_vietful_tenant_id ON tenants(vietful_tenant_id);
CREATE INDEX IF NOT EXISTS ix_tenants_shopify_store_domain ON tenants(shopify_store_domain);

-- -----------------------------------------------------------------------------
-- 2. Product Base Table (Shared cross-tenant master catalog)
-- Keyed on GTIN/barcode to deduplicate manufacturer and regulatory info
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS product_bases (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    gtin VARCHAR(64),
    barcode VARCHAR(128),
    manufacturer VARCHAR(512),
    country_of_origin VARCHAR(4),
    brand VARCHAR(256),
    manufacturer_part_number VARCHAR(256),
    standard_category VARCHAR(128),
    standard_category_name VARCHAR(512),
    regulatory_info_json JSONB,
    certifications_json JSONB,
    physical_attributes_json JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_product_bases_gtin ON product_bases(gtin) WHERE gtin IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_product_bases_brand ON product_bases(brand);
CREATE INDEX IF NOT EXISTS ix_product_bases_manufacturer ON product_bases(manufacturer);

-- -----------------------------------------------------------------------------
-- 3. Products Table (Tenant-specific product inventory)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    product_base_id UUID REFERENCES product_bases(id) ON DELETE SET NULL,
    sku VARCHAR(128) NOT NULL,
    vietful_product_id VARCHAR(256),
    shopify_product_id VARCHAR(256),
    shopify_variant_id VARCHAR(256),
    name VARCHAR(512) NOT NULL,
    description TEXT,
    price NUMERIC(18, 4) NOT NULL DEFAULT 0.0000,
    currency VARCHAR(8) NOT NULL DEFAULT 'VND',
    quantity INT NOT NULL DEFAULT 0,
    unit VARCHAR(64),
    category VARCHAR(512),
    brand VARCHAR(256),
    barcode VARCHAR(64),
    images_json JSONB,
    attributes_json JSONB,
    sync_status VARCHAR(32) NOT NULL DEFAULT 'Pending',
    last_synced_at TIMESTAMPTZ,
    last_sync_error TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_products_tenant_sku ON products(tenant_id, sku);
CREATE INDEX IF NOT EXISTS ix_products_tenant_vietful_id ON products(tenant_id, vietful_product_id);
CREATE INDEX IF NOT EXISTS ix_products_tenant_shopify_id ON products(tenant_id, shopify_product_id);
CREATE INDEX IF NOT EXISTS ix_products_tenant_sync_status ON products(tenant_id, sync_status);
CREATE INDEX IF NOT EXISTS ix_products_barcode ON products(barcode);

-- -----------------------------------------------------------------------------
-- 4. Worker Configurations Table
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS worker_configurations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    worker_type VARCHAR(32) NOT NULL,
    worker_name VARCHAR(128),
    polling_interval_seconds INT NOT NULL DEFAULT 60,
    batch_size INT NOT NULL DEFAULT 100,
    max_retry_attempts INT NOT NULL DEFAULT 3,
    retry_backoff_seconds INT NOT NULL DEFAULT 5,
    is_enabled BOOLEAN NOT NULL DEFAULT true,
    extended_config_json JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE INDEX IF NOT EXISTS ix_worker_configs_tenant_type ON worker_configurations(tenant_id, worker_type);

-- -----------------------------------------------------------------------------
-- 5. Worker Checkpoints Table (Resumable checkpoint tracking)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS worker_checkpoints (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    worker_id VARCHAR(256) NOT NULL,
    tenant_id UUID NOT NULL,
    worker_type VARCHAR(32) NOT NULL,
    last_processed_at TIMESTAMPTZ,
    last_cursor VARCHAR(512),
    last_message_offset BIGINT,
    metadata_json JSONB,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_worker_checkpoints_worker_id ON worker_checkpoints(worker_id);
CREATE INDEX IF NOT EXISTS ix_worker_checkpoints_tenant_type ON worker_checkpoints(tenant_id, worker_type);

-- -----------------------------------------------------------------------------
-- 6. Sync Metrics Table (Throughput, latency, error telemetry)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS sync_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    worker_type VARCHAR(32) NOT NULL,
    window_start TIMESTAMPTZ NOT NULL,
    window_end TIMESTAMPTZ NOT NULL,
    records_processed BIGINT NOT NULL DEFAULT 0,
    records_failed BIGINT NOT NULL DEFAULT 0,
    records_invalid BIGINT NOT NULL DEFAULT 0,
    avg_latency_ms NUMERIC(18, 2) NOT NULL DEFAULT 0.00,
    throughput_per_second NUMERIC(18, 2) NOT NULL DEFAULT 0.00,
    peak_memory_mb DOUBLE PRECISION,
    peak_cpu_percent DOUBLE PRECISION,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE INDEX IF NOT EXISTS ix_sync_metrics_tenant_type_window ON sync_metrics(tenant_id, worker_type, window_start);
CREATE INDEX IF NOT EXISTS ix_sync_metrics_created_at ON sync_metrics(created_at);

-- -----------------------------------------------------------------------------
-- 7. Data Quality Logs Table (Invalid data records, dead-letter tracking)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS data_quality_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    worker_type VARCHAR(32) NOT NULL,
    product_id UUID,
    external_product_id VARCHAR(256),
    sku VARCHAR(128),
    sync_status VARCHAR(32) NOT NULL,
    error_code VARCHAR(128) NOT NULL,
    error_message TEXT NOT NULL,
    field_name VARCHAR(256),
    field_value VARCHAR(1024),
    raw_payload_json JSONB,
    is_resolved BOOLEAN NOT NULL DEFAULT false,
    resolved_at TIMESTAMPTZ,
    resolution_notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE INDEX IF NOT EXISTS ix_data_quality_logs_tenant_resolved ON data_quality_logs(tenant_id, is_resolved);
CREATE INDEX IF NOT EXISTS ix_data_quality_logs_error_code ON data_quality_logs(error_code);
CREATE INDEX IF NOT EXISTS ix_data_quality_logs_created_at ON data_quality_logs(created_at);
