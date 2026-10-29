--------------------------------------------------------------------------------------
-- Installs EMAIL.QueryToHtml, EMAIL.ConCatHtml and EMAIL.CustomSendMailHelp in the
-- current database, replacing an earlier version. Run it in the database you want them
-- in, as a sysadmin: it adds the assembly to the server's trusted assemblies.
--
-- The assembly is SAFE, so it runs on SQL Server 2017 or later on Windows or Linux. Send
-- the HTML with Database Mail (msdb.dbo.sp_send_dbmail), or on Windows with
-- EMAIL.CLRSendMail from SetupSendMail.sql. Version 1 is the v1-article tag.
--
-- SetupHtml.sql is generated from SetupHtml.template.sql by tools/SetupScript; edit the template.
-- Sources: $(SourceHash)
--------------------------------------------------------------------------------------
SET NOCOUNT ON;
GO
--------------------------------------------------------------------------------------
-- Stop here, installing nothing, if the server can't run the assembly
--------------------------------------------------------------------------------------
IF CAST(SERVERPROPERTY('EngineEdition') AS int) = 5
BEGIN
    RAISERROR(N'Azure SQL Database doesn''t run CLR assemblies. QueryToHtml needs SQL Server 2017 or later, or Azure SQL Managed Instance.', 16, 1);
    SET NOEXEC ON;
END
ELSE IF ISNULL(CAST(SERVERPROPERTY('ProductMajorVersion') AS int), 0) < 14
BEGIN
    RAISERROR(N'QueryToHtml needs SQL Server 2017 or later. For earlier versions, use the v1-article tag of the repository.', 16, 1);
    SET NOEXEC ON;
END
ELSE IF IS_SRVROLEMEMBER(N'sysadmin') = 0
BEGIN
    RAISERROR(N'SetupHtml.sql adds the assembly to the server''s trusted assemblies, which needs a member of sysadmin.', 16, 1);
    SET NOEXEC ON;
END
ELSE IF (SELECT value_in_use FROM sys.configurations WHERE name = N'clr enabled') = 0
    PRINT N'CLR is disabled on this server. The functions are installed, but run only after: EXEC sp_configure ''clr enabled'', 1; RECONFIGURE;';
GO
--------------------------------------------------------------------------------------
-- Remove an earlier version: this assembly, and the HTML functions version 1 had in
-- its assembly SimpleTalk.SQLCLR.SendMail
--------------------------------------------------------------------------------------
DECLARE @drop nvarchar(max) =
(
    SELECT STRING_AGG(CAST(N'DROP FUNCTION ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + N'.' + QUOTENAME(o.name) + N';' AS nvarchar(max)), NCHAR(10))
    FROM sys.assembly_modules AS m
         JOIN sys.objects AS o ON o.object_id = m.object_id
         JOIN sys.assemblies AS a ON a.assembly_id = m.assembly_id
    WHERE a.name = N'SimpleTalk.SQLCLR.Html'
          OR (a.name = N'SimpleTalk.SQLCLR.SendMail' AND o.name IN (N'QueryToHtml', N'ConCatHtml', N'CustomSendMailHelp', N'CleanMemory'))
);
IF @drop IS NOT NULL
    EXEC sys.sp_executesql @drop;
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = N'SimpleTalk.SQLCLR.Html')
    DROP ASSEMBLY [SimpleTalk.SQLCLR.Html];
IF SCHEMA_ID(N'EMAIL') IS NULL
    EXEC sys.sp_executesql N'CREATE SCHEMA EMAIL';
GO
--------------------------------------------------------------------------------------
-- Trust this build of the assembly. SQL Server 2017's "clr strict security" loads only
-- trusted assemblies, and a trusted assembly needs no key, certificate or login. Earlier
-- builds' hashes are removed once no database uses them.
--------------------------------------------------------------------------------------
DECLARE @hash varbinary(64) = $(AssemblyHash);

-- The builds of the assembly each database has
DECLARE @used TABLE (hash varbinary(64));
DECLARE @sql nvarchar(max) =
(
    SELECT STRING_AGG(CAST(N'SELECT HASHBYTES(''SHA2_512'', f.content) FROM ' + QUOTENAME(name) + N'.sys.assemblies AS a JOIN '
                           + QUOTENAME(name) + N'.sys.assembly_files AS f ON f.assembly_id = a.assembly_id AND f.file_id = 1'
                           + N' WHERE a.name = N''SimpleTalk.SQLCLR.Html''' AS nvarchar(max)), N' UNION ALL ')
    FROM sys.databases
    WHERE state = 0 AND HAS_DBACCESS(name) = 1
);
INSERT INTO @used EXEC sys.sp_executesql @sql;

DECLARE @stale nvarchar(max) =
(
    SELECT STRING_AGG(CAST(N'EXEC sys.sp_drop_trusted_assembly @hash = ' + CONVERT(nvarchar(max), t.hash, 1) + N';' AS nvarchar(max)), NCHAR(10))
    FROM sys.trusted_assemblies AS t
    WHERE t.description LIKE N'simpletalk.sqlclr.html,%'
          AND t.hash <> @hash
          AND NOT EXISTS (SELECT * FROM @used AS u WHERE u.hash = t.hash)
);
IF @stale IS NOT NULL
    EXEC sys.sp_executesql @stale;

IF NOT EXISTS (SELECT * FROM sys.trusted_assemblies WHERE hash = @hash)
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'$(AssemblyDescription)';
GO
--------------------------------------------------------------------------------------
-- The assembly and its functions
--------------------------------------------------------------------------------------
CREATE ASSEMBLY [SimpleTalk.SQLCLR.Html]
    AUTHORIZATION [dbo]
    FROM $(AssemblyBits)
    WITH PERMISSION_SET = SAFE;
GO
CREATE FUNCTION [EMAIL].[QueryToHtml]
(
    @query [nvarchar](max),
    @Params [nvarchar](4000),
    @caption [nvarchar](200),
    @footer [nvarchar](1000),
    @rm [smallint],
    @rco [smallint],
    @style [nvarchar](4000)
)
RETURNS [nvarchar](max)
AS EXTERNAL NAME [SimpleTalk.SQLCLR.Html].[SimpleTalk.SqlClr.Html.UserDefinedFunctions].[QueryToHtml];
GO
CREATE FUNCTION [EMAIL].[ConCatHtml] (@mHtml [nvarchar](max), @sHtml [nvarchar](max))
RETURNS [nvarchar](max)
AS EXTERNAL NAME [SimpleTalk.SQLCLR.Html].[SimpleTalk.SqlClr.Html.UserDefinedFunctions].[ConCatHtml];
GO
CREATE FUNCTION [EMAIL].[CustomSendMailHelp] (@ProcedureName [nvarchar](max))
RETURNS TABLE
(
    [ParameterName] [nvarchar](100) NULL,
    [ParameterDescription] [nvarchar](max) NULL
)
AS EXTERNAL NAME [SimpleTalk.SQLCLR.Html].[SimpleTalk.SqlClr.Html.UserDefinedFunctions].[CustomSendMailHelp];
GO
SET NOEXEC OFF;
GO
