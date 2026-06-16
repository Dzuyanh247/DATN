/*
Database health-check script for DATN_PCStore.
Run in SQL Server Management Studio against the target database after restore or migration.
*/

-- 1. Existing user tables
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    t.create_date AS CreatedAt,
    t.modify_date AS ModifiedAt
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
ORDER BY s.name, t.name;

-- 2. Foreign keys
SELECT
    fk.name AS ForeignKeyName,
    SCHEMA_NAME(parent.schema_id) AS ParentSchema,
    parent.name AS ParentTable,
    parentColumn.name AS ParentColumn,
    SCHEMA_NAME(referenced.schema_id) AS ReferencedSchema,
    referenced.name AS ReferencedTable,
    referencedColumn.name AS ReferencedColumn,
    fk.delete_referential_action_desc AS OnDelete,
    fk.update_referential_action_desc AS OnUpdate,
    fk.is_disabled AS IsDisabled,
    fk.is_not_trusted AS IsNotTrusted
FROM sys.foreign_keys fk
INNER JOIN sys.tables parent ON parent.object_id = fk.parent_object_id
INNER JOIN sys.tables referenced ON referenced.object_id = fk.referenced_object_id
INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.columns parentColumn ON parentColumn.object_id = fkc.parent_object_id AND parentColumn.column_id = fkc.parent_column_id
INNER JOIN sys.columns referencedColumn ON referencedColumn.object_id = fkc.referenced_object_id AND referencedColumn.column_id = fkc.referenced_column_id
ORDER BY ParentTable, ForeignKeyName, fkc.constraint_column_id;

-- 3. Indexes
SELECT
    SCHEMA_NAME(t.schema_id) AS SchemaName,
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    i.is_primary_key AS IsPrimaryKey,
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal, ic.index_column_id) AS Columns
FROM sys.indexes i
INNER JOIN sys.tables t ON t.object_id = i.object_id
INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.name IS NOT NULL
GROUP BY SCHEMA_NAME(t.schema_id), t.name, i.name, i.type_desc, i.is_unique, i.is_primary_key
ORDER BY t.name, i.name;

-- 4. Foreign keys whose referenced table cannot be resolved from metadata.
-- SQL Server normally prevents this, so any row returned here indicates catalog corruption or disabled/incomplete deployment scripts.
SELECT
    fk.name AS ForeignKeyName,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ParentSchema,
    OBJECT_NAME(fk.parent_object_id) AS ParentTable,
    fk.referenced_object_id AS MissingReferencedObjectId
FROM sys.foreign_keys fk
LEFT JOIN sys.tables referenced ON referenced.object_id = fk.referenced_object_id
WHERE referenced.object_id IS NULL;

-- 5. Project-specific identity table check.
SELECT
    CASE WHEN OBJECT_ID('dbo.Users', 'U') IS NULL THEN 'MISSING' ELSE 'OK' END AS UsersTableStatus,
    CASE WHEN OBJECT_ID('dbo.AspNetUsers', 'U') IS NULL THEN 'NOT_USED_OR_MISSING' ELSE 'EXISTS' END AS AspNetUsersTableStatus;
