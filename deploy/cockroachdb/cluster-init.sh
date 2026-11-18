#!/usr/bin/env bash
set -e

echo "=== Initializing CockroachDB Cluster ==="
cockroach init --insecure --host=roach1:26257 || true

echo "=== Creating CDMS Database ==="
cockroach sql --insecure --host=roach1:26257 -e "CREATE DATABASE IF NOT EXISTS cdms;"

echo "=== Applying Migrations ==="
cockroach sql --insecure --host=roach1:26257 --database=cdms < ./src/CDMS.Infrastructure/Migrations/001_InitialSchema.sql

echo "=== Cluster Initialization Complete ==="
