﻿CREATE TABLE Captures(
	id INTEGER PRYMARY KEY AUTOINCREMENT,
	drive_model TEXT,
	drive_sn TEXT,
	drive_size INTEGER,
	drive_nickname TEXT,
	partition_label TEXT,
	partition_number INTEGER,
	partition_size INTEGER,
	partition_free_space INTEGER,
	capture_properties INTEGER,
	capture_datetime TEXT,
	capture_lines_number INTEGER,
	capture BLOB,
	sizes BLOB,
	creation_times BLOB,
	last_access_times BLOB,
	last_mod_times BLOB
)