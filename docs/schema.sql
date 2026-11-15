CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "AspNetRoles" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetRoles" PRIMARY KEY,
    "Name" TEXT NULL,
    "NormalizedName" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL
);

CREATE TABLE "AspNetUsers" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetUsers" PRIMARY KEY,
    "DisplayName" TEXT NOT NULL,
    "IsPublic" INTEGER NOT NULL,
    "UserName" TEXT NULL,
    "NormalizedUserName" TEXT NULL,
    "Email" TEXT NULL,
    "NormalizedEmail" TEXT NULL,
    "EmailConfirmed" INTEGER NOT NULL,
    "PasswordHash" TEXT NULL,
    "SecurityStamp" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL,
    "PhoneNumber" TEXT NULL,
    "PhoneNumberConfirmed" INTEGER NOT NULL,
    "TwoFactorEnabled" INTEGER NOT NULL,
    "LockoutEnd" INTEGER NULL,
    "LockoutEnabled" INTEGER NOT NULL,
    "AccessFailedCount" INTEGER NOT NULL
);

CREATE TABLE "AspNetRoleClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY AUTOINCREMENT,
    "RoleId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY AUTOINCREMENT,
    "UserId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT NULL,
    "UserId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserRoles" (
    "UserId" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserTokens" (
    "UserId" TEXT NOT NULL,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT NULL,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Routines" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Routines" PRIMARY KEY,
    "OwnerId" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Kind" INTEGER NOT NULL,
    "TargetMinutes" INTEGER NOT NULL,
    "DaysOfWeek" INTEGER NOT NULL,
    "Archived" INTEGER NOT NULL,
    "Revision" INTEGER NOT NULL,
    "CreatedOn" TEXT NOT NULL,
    CONSTRAINT "CK_Routine_Days" CHECK (DaysOfWeek BETWEEN 1 AND 127),
    CONSTRAINT "CK_Routine_Minutes" CHECK (TargetMinutes BETWEEN 1 AND 600),
    CONSTRAINT "FK_Routines_AspNetUsers_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CheckIns" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_CheckIns" PRIMARY KEY,
    "RoutineId" TEXT NOT NULL,
    "Date" TEXT NOT NULL,
    "Minutes" INTEGER NOT NULL,
    "Notes" TEXT NOT NULL,
    CONSTRAINT "CK_CheckIn_Minutes" CHECK (Minutes BETWEEN 1 AND 600),
    CONSTRAINT "FK_CheckIns_Routines_RoutineId" FOREIGN KEY ("RoutineId") REFERENCES "Routines" ("Id") ON DELETE CASCADE
);

CREATE TABLE "RoutineSteps" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_RoutineSteps" PRIMARY KEY,
    "RoutineId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Sets" INTEGER NOT NULL,
    "Repetitions" INTEGER NOT NULL,
    "Position" INTEGER NOT NULL,
    CONSTRAINT "CK_Step_Reps" CHECK (Repetitions BETWEEN 1 AND 500),
    CONSTRAINT "CK_Step_Sets" CHECK (Sets BETWEEN 1 AND 30),
    CONSTRAINT "FK_RoutineSteps_Routines_RoutineId" FOREIGN KEY ("RoutineId") REFERENCES "Routines" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");

CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");

CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");

CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");

CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");

CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");

CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");

CREATE INDEX "IX_CheckIns_Date" ON "CheckIns" ("Date");

CREATE UNIQUE INDEX "IX_CheckIns_RoutineId_Date" ON "CheckIns" ("RoutineId", "Date");

CREATE INDEX "IX_Routines_OwnerId_Archived" ON "Routines" ("OwnerId", "Archived");

CREATE INDEX "IX_RoutineSteps_RoutineId" ON "RoutineSteps" ("RoutineId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261114161124_InitialCreate', '8.0.34');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "AspNetUsers" ADD "WeeklyGoal" INTEGER NOT NULL DEFAULT 5;

CREATE TABLE "ef_temp_AspNetUsers" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetUsers" PRIMARY KEY,
    "AccessFailedCount" INTEGER NOT NULL,
    "ConcurrencyStamp" TEXT NULL,
    "DisplayName" TEXT NOT NULL,
    "Email" TEXT NULL,
    "EmailConfirmed" INTEGER NOT NULL,
    "IsPublic" INTEGER NOT NULL,
    "LockoutEnabled" INTEGER NOT NULL,
    "LockoutEnd" INTEGER NULL,
    "NormalizedEmail" TEXT NULL,
    "NormalizedUserName" TEXT NULL,
    "PasswordHash" TEXT NULL,
    "PhoneNumber" TEXT NULL,
    "PhoneNumberConfirmed" INTEGER NOT NULL,
    "SecurityStamp" TEXT NULL,
    "TwoFactorEnabled" INTEGER NOT NULL,
    "UserName" TEXT NULL,
    "WeeklyGoal" INTEGER NOT NULL DEFAULT 5,
    CONSTRAINT "CK_User_WeeklyGoal" CHECK (WeeklyGoal BETWEEN 1 AND 100)
);

INSERT INTO "ef_temp_AspNetUsers" ("Id", "AccessFailedCount", "ConcurrencyStamp", "DisplayName", "Email", "EmailConfirmed", "IsPublic", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName", "WeeklyGoal")
SELECT "Id", "AccessFailedCount", "ConcurrencyStamp", "DisplayName", "Email", "EmailConfirmed", "IsPublic", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName", "WeeklyGoal"
FROM "AspNetUsers";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;

DROP TABLE "AspNetUsers";

ALTER TABLE "ef_temp_AspNetUsers" RENAME TO "AspNetUsers";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;

CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");

CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261115122014_AddWeeklyGoal', '8.0.34');

COMMIT;

