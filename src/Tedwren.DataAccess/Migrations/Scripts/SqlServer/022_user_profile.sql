-- Self-service profile fields on the console user: mobile number + avatar image reference (SF-20). SQL Server. Idempotent.

IF COL_LENGTH('dbo.Users', 'Mobile') IS NULL
    ALTER TABLE dbo.Users ADD Mobile NVARCHAR(32) NULL;

IF COL_LENGTH('dbo.Users', 'AvatarImageReference') IS NULL
    ALTER TABLE dbo.Users ADD AvatarImageReference NVARCHAR(MAX) NULL;
