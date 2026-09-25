CREATE TABLE IF NOT EXISTS elements (
    id           BIGSERIAL PRIMARY KEY,
    attr_value   TEXT NOT NULL,
    element_html TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_elements_attr_value ON elements (attr_value);
