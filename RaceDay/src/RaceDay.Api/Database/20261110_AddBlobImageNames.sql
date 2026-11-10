-- Run once against the RaceDay database before using image uploads.
ALTER TABLE [Events] ADD [BannerBlobName] nvarchar(500) NULL;
ALTER TABLE [Users] ADD [ProfileImageBlobName] nvarchar(500) NULL;
