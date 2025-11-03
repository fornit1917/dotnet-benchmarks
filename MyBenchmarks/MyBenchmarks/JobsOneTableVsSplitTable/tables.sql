CREATE TABLE IF NOT EXISTS test_jobs_full (
	id UUID NOT NULL PRIMARY KEY,
	job_name TEXT NOT NULL,
	job_param TEXT DEFAULT NULL,
	status int NOT NULL,
	error TEXT DEFAULT NULL,
	created_at timestamptz NOT NULL,
	scheduled_start_at timestamptz NOT NULL,
	started_count int NOT NULL DEFAULT 0,
	next_job_id UUID DEFAULT NULL
);

CREATE INDEX ON test_jobs_full(status, scheduled_start_at);


CREATE TABLE IF NOT EXISTS test_jobs_split_ro (
	id UUID NOT NULL PRIMARY KEY,
	job_name TEXT NOT NULL,
	job_param TEXT DEFAULT NULL,
	created_at timestamptz NOT NULL,
	next_job_id UUID DEFAULT NULL
);

CREATE TABLE IF NOT EXISTS test_jobs_split_w (
	id UUID NOT NULL PRIMARY KEY,
	status int NOT NULL,
	error TEXT DEFAULT NULL,
	scheduled_start_at timestamptz NOT NULL,
	started_count int NOT NULL DEFAULT 0,
);

CREATE INDEX ON test_jobs_split_w(status, scheduled_start_at)