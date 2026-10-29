using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace SimpleTalk.SqlClr.SendMail
{
    public enum LoggingLevel
    {
        Minimal = 0,
        Usual = 1,
        Maximum = 2
    }

    /// <summary>
    /// A row of EMAIL.Configurations: what may be attached, what is logged and what is kept.
    /// </summary>
    internal sealed class MailConfiguration
    {
        /// <summary>The configuration used when @configName is null.</summary>
        public static MailConfiguration BuiltIn() => new MailConfiguration();

        /// <summary>The configuration's name, or null for the built-in one.</summary>
        public string? Name { get; private set; }

        /// <summary>Maximum file size in bytes. Default 1MB</summary>
        public int MaxFileSize { get; private set; } = 1000000;

        /// <summary>File extensions that are not allowed, comma separated</summary>
        public string ProhibitedExtensions { get; private set; } = "exe,dll,vbs,js";

        public LoggingLevel LoggingLevel { get; private set; } = LoggingLevel.Minimal;

        /// <summary>Do we save e-mails in EMAIL.MailItems?</summary>
        public bool SaveEmails { get; private set; } = true;

        /// <summary>True for no "Sent successfully!" message</summary>
        public bool NoPiping { get; private set; } = true;

        /// <summary>Do we save attachments in EMAIL.MailAttachments?</summary>
        public bool SaveAttachments { get; private set; } = true;

        /// <summary>The folders files may be attached from, semicolon separated; none when null.</summary>
        public string? AttachmentFolders { get; private set; }

        public bool IsProhibited(string extension)
        {
            foreach (var prohibited in ProhibitedExtensions.Split(','))
                if (string.Equals(prohibited.Trim().TrimStart('.'), extension.TrimStart('.'), StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        /// <summary>The attachment folders as full paths ending with a separator.</summary>
        public List<string> Folders()
        {
            var folders = new List<string>();
            if (AttachmentFolders == null)
                return folders;

            foreach (var folder in AttachmentFolders.Split(';'))
            {
                if (folder.Trim().Length == 0)
                    continue;
                var full = Path.GetFullPath(folder.Trim());
                folders.Add(full.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? full : full + Path.DirectorySeparatorChar);
            }

            return folders;
        }

        public static MailConfiguration Load(SqlConnection cnn, string name)
        {
            using (var command = new SqlCommand(@"
                SELECT TOP (1) Name, MaxFileSize, ProhibitedExtensions, LoggingLevel, SaveEmails, NoPiping, SaveAttachments, AttachmentFolders
                FROM EMAIL.Configurations
                WHERE Name = @name;", cnn))
            {
                command.Parameters.Add("@name", SqlDbType.Char, 20).Value = name;
                using (var dr = command.ExecuteReader())
                {
                    if (!dr.Read())
                        throw new SendMailException($"The configuration '{name}' is not in EMAIL.Configurations.");

                    var level = Convert.ToInt32(dr["LoggingLevel"]);
                    if (!Enum.IsDefined(typeof(LoggingLevel), level))
                        throw new SendMailException($"The configuration '{name}' has LoggingLevel {level}: use 0, 1 or 2.");

                    return new MailConfiguration
                    {
                        Name = ((string)dr["Name"]).Trim(),
                        MaxFileSize = (int)dr["MaxFileSize"],
                        ProhibitedExtensions = (string)dr["ProhibitedExtensions"],
                        LoggingLevel = (LoggingLevel)level,
                        SaveEmails = (bool)dr["SaveEmails"],
                        NoPiping = (bool)dr["NoPiping"],
                        SaveAttachments = (bool)dr["SaveAttachments"],
                        AttachmentFolders = dr["AttachmentFolders"] as string
                    };
                }
            }
        }
    }
}
