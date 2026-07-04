IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE [name] = 'SolicitantePublicador')
BEGIN
    INSERT INTO dbo.Roles ([name]) VALUES ('SolicitantePublicador');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE [name] = 'PublicadorRechazado')
BEGIN
    INSERT INTO dbo.Roles ([name]) VALUES ('PublicadorRechazado');
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PublisherRequests' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PublisherRequests
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PublisherRequests PRIMARY KEY,
        userId INT NOT NULL,
        [status] NVARCHAR(30) NOT NULL CONSTRAINT DF_PublisherRequests_Status DEFAULT ('Pendiente'),
        requestedAt DATETIME2 NOT NULL CONSTRAINT DF_PublisherRequests_RequestedAt DEFAULT (SYSUTCDATETIME()),
        reviewedAt DATETIME2 NULL,
        reviewedByUserId INT NULL,
        decisionNotes NVARCHAR(1000) NULL,
        identificationImageData VARBINARY(MAX) NOT NULL,
        identificationImageContentType NVARCHAR(100) NOT NULL,
        CONSTRAINT FK_PublisherRequests_Users
            FOREIGN KEY (userId) REFERENCES dbo.Users(id),
        CONSTRAINT FK_PublisherRequests_ReviewedByUser
            FOREIGN KEY (reviewedByUserId) REFERENCES dbo.Users(id)
    );

    CREATE INDEX IX_PublisherRequests_UserId
        ON dbo.PublisherRequests(userId);

    CREATE INDEX IX_PublisherRequests_ReviewedByUserId
        ON dbo.PublisherRequests(reviewedByUserId);

    CREATE UNIQUE INDEX UX_PublisherRequests_User_Pending
        ON dbo.PublisherRequests(userId)
        WHERE [status] = N'Pendiente';
END;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PublisherRequests' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    IF COL_LENGTH('dbo.PublisherRequests', 'identificationImageData') IS NULL
    BEGIN
        ALTER TABLE dbo.PublisherRequests
        ADD identificationImageData VARBINARY(MAX) NULL;
    END;

    IF COL_LENGTH('dbo.PublisherRequests', 'identificationImageContentType') IS NULL
    BEGIN
        ALTER TABLE dbo.PublisherRequests
        ADD identificationImageContentType NVARCHAR(100) NULL;
    END;
END;
