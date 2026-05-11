-- ============================================================
-- TripPlanner Database Schema
-- Migration: 001_initial_schema.sql
-- ============================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- ============================================================
-- users
-- ============================================================
CREATE TABLE users (
    id            UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
    email         VARCHAR(255)  NOT NULL UNIQUE,
    username      VARCHAR(100)  NOT NULL UNIQUE,
    password_hash VARCHAR(512)  NOT NULL,
    avatar_url    VARCHAR(1024),
    is_active     BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- ============================================================
-- refresh_tokens
-- ============================================================
CREATE TABLE refresh_tokens (
    id          UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id     UUID         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token       VARCHAR(512) NOT NULL UNIQUE,
    expires_at  TIMESTAMPTZ  NOT NULL,
    revoked_at  TIMESTAMPTZ,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- ============================================================
-- attractions
-- ============================================================
CREATE TABLE attractions (
    id            UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
    name          VARCHAR(255)  NOT NULL,
    description   TEXT,
    category      VARCHAR(100),
    address       VARCHAR(512),
    city          VARCHAR(100),
    country       VARCHAR(100),
    latitude      DECIMAL(9,6),
    longitude     DECIMAL(9,6),
    cover_image   VARCHAR(1024),
    rating        DECIMAL(3,1)  NOT NULL DEFAULT 0,
    tags          TEXT[]        NOT NULL DEFAULT '{}',
    search_vector TSVECTOR,
    created_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- ============================================================
-- trips
-- ============================================================
CREATE TABLE trips (
    id          UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id     UUID         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title       VARCHAR(255) NOT NULL,
    description TEXT,
    cover_image VARCHAR(1024),
    start_date  DATE,
    end_date    DATE,
    status      VARCHAR(50)  NOT NULL DEFAULT 'Draft',
    is_public   BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_trip_dates CHECK (end_date IS NULL OR start_date IS NULL OR end_date >= start_date)
);

-- ============================================================
-- trip_items
-- ============================================================
CREATE TABLE trip_items (
    id             UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
    trip_id        UUID        NOT NULL REFERENCES trips(id) ON DELETE CASCADE,
    attraction_id  UUID        REFERENCES attractions(id) ON DELETE SET NULL,
    day_number     INTEGER     NOT NULL DEFAULT 1,
    sort_order     INTEGER     NOT NULL DEFAULT 0,
    custom_name    VARCHAR(255),
    notes          TEXT,
    start_time     TIME,
    duration_mins  INTEGER,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_day_number CHECK (day_number > 0),
    CONSTRAINT chk_sort_order CHECK (sort_order >= 0)
);

-- ============================================================
-- favorites
-- ============================================================
CREATE TABLE favorites (
    user_id       UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    attraction_id UUID        NOT NULL REFERENCES attractions(id) ON DELETE CASCADE,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, attraction_id)
);

-- ============================================================
-- share_links
-- ============================================================
CREATE TABLE share_links (
    id          UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    trip_id     UUID         NOT NULL REFERENCES trips(id) ON DELETE CASCADE,
    share_token VARCHAR(64)  NOT NULL UNIQUE,
    permission  VARCHAR(20)  NOT NULL DEFAULT 'View',
    expires_at  TIMESTAMPTZ,
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    view_count  INTEGER      NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- ============================================================
-- Indexes
-- ============================================================
CREATE INDEX idx_refresh_tokens_user_id   ON refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_token     ON refresh_tokens(token);
CREATE INDEX idx_attractions_search       ON attractions USING GIN(search_vector);
CREATE INDEX idx_attractions_category     ON attractions(category);
CREATE INDEX idx_attractions_city         ON attractions(city);
CREATE INDEX idx_attractions_name_trgm    ON attractions USING GIN(name gin_trgm_ops);
CREATE INDEX idx_trips_user_id            ON trips(user_id);
CREATE INDEX idx_trip_items_trip_id       ON trip_items(trip_id);
CREATE INDEX idx_favorites_user_id        ON favorites(user_id);
CREATE INDEX idx_share_links_token        ON share_links(share_token);
CREATE INDEX idx_share_links_trip_id      ON share_links(trip_id);

-- ============================================================
-- Triggers
-- ============================================================
CREATE OR REPLACE FUNCTION update_attraction_search_vector()
RETURNS TRIGGER AS $$
BEGIN
    NEW.search_vector :=
        setweight(to_tsvector('simple', COALESCE(NEW.name, '')), 'A') ||
        setweight(to_tsvector('simple', COALESCE(NEW.description, '')), 'B') ||
        setweight(to_tsvector('simple', COALESCE(NEW.city, '')), 'C') ||
        setweight(to_tsvector('simple', COALESCE(NEW.category, '')), 'C');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_attractions_search_vector
    BEFORE INSERT OR UPDATE ON attractions
    FOR EACH ROW EXECUTE FUNCTION update_attraction_search_vector();

CREATE OR REPLACE FUNCTION update_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION update_updated_at();

CREATE TRIGGER trg_trips_updated_at
    BEFORE UPDATE ON trips FOR EACH ROW EXECUTE FUNCTION update_updated_at();

CREATE TRIGGER trg_attractions_updated_at
    BEFORE UPDATE ON attractions FOR EACH ROW EXECUTE FUNCTION update_updated_at();
