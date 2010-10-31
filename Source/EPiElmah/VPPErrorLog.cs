using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Hosting;
using System.Xml;
using Elmah;
using EPiServer.Web.Hosting;
using System.Linq;

namespace JarleF.EPiElmah
{
    public class VPPErrorLog : ErrorLog
    {
        public string LogPath { get; private set; }

        public VPPErrorLog(IDictionary config)
        {
            LogPath = config["logPath"] as string ?? config["LogPath"] as string ?? config["logpath"] as string ?? string.Empty;

            if (LogPath.Length == 0)
            {
                throw new Elmah.ApplicationException("Log path is missing for the Virtual XML file-based error log.");
            }
        }

        public VPPErrorLog(string logPath)
        {
            LogPath = logPath;

            if (LogPath.Length == 0)
            {
                throw new Elmah.ApplicationException("Log path is missing for the Virtual XML file-based error log.");
            }
        }

        public override string Log(Error error)
        {
            var logRoot = GetLogRoot();

            var entryId = Guid.NewGuid().ToString();
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddHHmmssZ", CultureInfo.InvariantCulture);

            string errorFilePath = Path.Combine(LogPath, string.Format("error-{0}-{1}.xml", timestamp, entryId));
            
            UnifiedFile file = logRoot.CreateFile(errorFilePath);
            
            WriteError(entryId, file, error);

            return entryId;
        }
        
        public override ErrorLogEntry GetError(string id)
        {
            string entryId;

            try
            {
                entryId = new Guid(id).ToString();
            }
            catch (FormatException exception)
            {
                throw new ArgumentException(exception.Message, id, exception);
            }

            var logRoot = GetLogRoot();

            var filePattern = string.Format("error-*-{0}.xml", entryId);
            var fileQuery = new UnifiedSearchQuery {FileNamePattern = filePattern, FreeTextQuery = "" };
            fileQuery.MatchSummary.Add("*", "");

            var fileHits = logRoot.Search(fileQuery);

            if (fileHits.Count < 1)
            {
                throw new FileNotFoundException(string.Format("Cannot locate error file for error with ID {0}.", entryId));
            }

            return ReadError(fileHits[0]);
        }
        
        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {
            if (pageIndex < 0)
            {
                throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);
            }
            if (pageSize < 0)
            {
                throw new ArgumentOutOfRangeException("pageSize", pageSize, null);
            }

            var logRoot = GetLogRoot();

            var fileQuery = new UnifiedSearchQuery  { FileNamePattern = "error*", FreeTextQuery = ""};
            fileQuery.MatchSummary.Add("*", "");
            var fileHits = logRoot.Search(fileQuery);

            int totalCount = fileHits.Count;

            if (totalCount < 1)
            {
                return 0;
            }

            var errors = fileHits.OfType<UnifiedSearchHit>().Take(pageSize).Skip(pageIndex * pageSize).Select(ReadError).ToList();

            foreach(var error in errors)
            {
                if(error != null)
                {
                    errorEntryList.Add(error);
                }
            }

            return totalCount;
        }

        private UnifiedDirectory GetLogRoot()
        {
            if (HostingEnvironment.VirtualPathProvider.DirectoryExists(LogPath) == false)
            {
                UnifiedDirectory.CreateDirectory(LogPath);
            }
           
            return HostingEnvironment.VirtualPathProvider.GetDirectory(LogPath) as UnifiedDirectory;
        }


        private ErrorLogEntry ReadError(UnifiedSearchHit hit)
        {
            var file = HostingEnvironment.VirtualPathProvider.GetFile(hit.Path) as UnifiedFile;

            return ReadError(file);
        }

        private ErrorLogEntry ReadError(UnifiedFile file)
        {
            using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var reader = new XmlTextReader(stream);

                if (reader.IsStartElement("error") == false)
                {
                    return null;
                }

                var entryId = reader.GetAttribute("errorId");
                var error = ErrorXml.Decode(reader);
                return new ErrorLogEntry(this, entryId, error);
            }
        }

        private void WriteError(string entryId, UnifiedFile file, Error error)
        {
            using (var fileStream = file.Open(FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var writer = new XmlTextWriter(fileStream, Encoding.UTF8) { Formatting = Formatting.Indented };
                writer.WriteStartElement("error");
                writer.WriteAttributeString("errorId", entryId);
                ErrorXml.Encode(error, writer);
                writer.WriteEndElement();
                writer.Flush();
            }
        }

    }
}