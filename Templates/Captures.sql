CREATE TABLE Captures(
	id INTEGER PRYMARY KEY AUTOINCREMENT,
	drive_model TEXT,
	drive_sn TEXT,
	drive_nickname TEXT,
	partition_number INTEGER,
	capture_date TEXT,
	capture BLOB
)