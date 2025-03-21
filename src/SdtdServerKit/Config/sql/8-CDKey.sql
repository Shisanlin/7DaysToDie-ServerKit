PRAGMA FOREIGN_KEYS = ON;

--CdKey
CREATE TABLE IF NOT EXISTS CdKey(
	Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,	--Id
	CreatedAt TEXT NOT NULL,						--Created At
	[Key] TEXT NOT NULL UNIQUE,						--Key
	RedeemCount INTEGER NOT NULL,					--Redeem Count
	MaxRedeemCount INTEGER NOT NULL,				--Max Redeem Count
	ExpiryAt TEXT NULL,								--Expiry At
	Description TEXT NULL							--Description
);
CREATE UNIQUE INDEX IF NOT EXISTS Index_CdKey_0 ON CdKey([Key]);

--CdKey Redeem Record
CREATE TABLE IF NOT EXISTS CdKeyRedeemRecord(
	Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,	--Id
	CreatedAt TEXT NOT NULL,						--Created At
	[Key] TEXT NOT NULL,							--Key
	PlayerId TEXT NOT NULL,							--Player Id
	PlayerName TEXT NOT NULL,						--Player Name
	FOREIGN KEY ([Key]) REFERENCES CdKey([Key]) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS Index_CdKeyRedeemRecord_0 ON CdKeyRedeemRecord([Key], PlayerId);

CREATE TABLE IF NOT EXISTS CdKeyItem(
	CdKeyId INTEGER NOT NULL,						
	ItemId INTEGER NOT NULL,						
	PRIMARY KEY (CdKeyId, ItemId),
	FOREIGN KEY (CdKeyId) REFERENCES CdKey(Id) ON DELETE CASCADE,
	FOREIGN KEY (ItemId) REFERENCES T_ItemList(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS CdKeyCommand(
	CdKeyId INTEGER NOT NULL,						
	CommandId INTEGER NOT NULL,						
	PRIMARY KEY (CdKeyId, CommandId),
	FOREIGN KEY (CdKeyId) REFERENCES CdKey(Id) ON DELETE CASCADE,
	FOREIGN KEY (CommandId) REFERENCES T_CommandList(Id) ON DELETE CASCADE
);