CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "Categories" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Categories" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "CreatedAt" TEXT NOT NULL
);

CREATE TABLE "Warehouses" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Warehouses" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Location" TEXT NULL,
    "IsActive" INTEGER NOT NULL DEFAULT 1,
    "CreatedAt" TEXT NOT NULL,
    "DeactivatedAt" TEXT NULL
);

CREATE TABLE "WarehouseSections" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_WarehouseSections" PRIMARY KEY,
    "WarehouseId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    CONSTRAINT "FK_WarehouseSections_Warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES "Warehouses" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Products" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Products" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Sku" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Size" TEXT NULL,
    "Type" TEXT NULL,
    "ExpirationDate" TEXT NULL,
    "ImageUrl" TEXT NULL,
    "ImageMetadata" TEXT NULL,
    "WarehouseSectionId" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Products_WarehouseSections_WarehouseSectionId" FOREIGN KEY ("WarehouseSectionId") REFERENCES "WarehouseSections" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "ExpirationAlerts" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ExpirationAlerts" PRIMARY KEY,
    "ProductId" TEXT NOT NULL,
    "AlertDate" TEXT NOT NULL,
    "DaysUntilExpiration" INTEGER NOT NULL,
    "IsAcknowledged" INTEGER NOT NULL DEFAULT 0,
    "AcknowledgedAt" TEXT NULL,
    CONSTRAINT "FK_ExpirationAlerts_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ProductCategories" (
    "ProductId" TEXT NOT NULL,
    "CategoryId" TEXT NOT NULL,
    CONSTRAINT "PK_ProductCategories" PRIMARY KEY ("ProductId", "CategoryId"),
    CONSTRAINT "FK_ProductCategories_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ProductCategories_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_Categories_Name" ON "Categories" ("Name");

CREATE INDEX "IX_ExpirationAlerts_AlertDate" ON "ExpirationAlerts" ("AlertDate");

CREATE INDEX "IX_ExpirationAlerts_IsAcknowledged" ON "ExpirationAlerts" ("IsAcknowledged");

CREATE INDEX "IX_ExpirationAlerts_ProductId" ON "ExpirationAlerts" ("ProductId");

CREATE INDEX "IX_ProductCategories_CategoryId" ON "ProductCategories" ("CategoryId");

CREATE INDEX "IX_Products_ExpirationDate" ON "Products" ("ExpirationDate");

CREATE INDEX "IX_Products_Name" ON "Products" ("Name");

CREATE UNIQUE INDEX "IX_Products_Sku" ON "Products" ("Sku");

CREATE INDEX "IX_Products_WarehouseSectionId" ON "Products" ("WarehouseSectionId");

CREATE INDEX "IX_Warehouses_IsActive" ON "Warehouses" ("IsActive");

CREATE UNIQUE INDEX "IX_Warehouses_Name" ON "Warehouses" ("Name");

CREATE UNIQUE INDEX "IX_WarehouseSections_WarehouseId_Name" ON "WarehouseSections" ("WarehouseId", "Name");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260509132534_InitialCreate', '10.0.0');

COMMIT;

BEGIN TRANSACTION;
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260509164004_AddAcknowledgedAtToExpirationAlert', '10.0.0');

COMMIT;

