-- Migration: 002_trip_item_custom_location.sql
-- 讓行程項目可儲存自訂座標（非資料庫景點，例如 Google Maps 地點）
ALTER TABLE trip_items
  ADD COLUMN IF NOT EXISTS custom_latitude  DECIMAL(9,6),
  ADD COLUMN IF NOT EXISTS custom_longitude DECIMAL(9,6);
