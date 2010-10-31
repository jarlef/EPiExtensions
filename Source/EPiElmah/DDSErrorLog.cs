using System;
using System.Collections;
using System.Linq;
using Elmah;
using EPiServer.Data.Dynamic;
using JarleF.EPiElmah.Domain;

namespace JarleF.EPiElmah
{
    public class DDSErrorLog : ErrorLog
    {
        public DDSErrorLog(IDictionary config) : this()
        {
        }

        public DDSErrorLog()
        {
        }

        public override string Log(Error error)
        {
            var entry = new DDSErrorLogEntry(error);
 
            var factory = new EPiServerDynamicDataStoreFactory();
            using (var store = factory.CreateStore(typeof(DDSErrorLogEntry)))
            {
                store.Save(entry);
                return entry.Id.ExternalId.ToString();
            }
        }

        public override ErrorLogEntry GetError(string id)
        {
            var factory = new EPiServerDynamicDataStoreFactory();
            using (var store = factory.CreateStore(typeof(DDSErrorLogEntry)))
            {
                var guidID = new Guid(id);
                var query = from e in store.Items<DDSErrorLogEntry>()
                            where e.Id.ExternalId == guidID
                            select e;

                var ddsLogEntry = query.ToList().FirstOrDefault();

                return new ErrorLogEntry(this, id, ddsLogEntry.Entry);
            }
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

            var factory = new EPiServerDynamicDataStoreFactory();
            using(var store = factory.CreateStore(typeof(DDSErrorLogEntry)))
            {

                var query = from e in store.Items<DDSErrorLogEntry>()
                            orderby e.Entry.Time descending
                            select e;

                var ddsLogEntries = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();

                foreach (var ddsLogEntry in ddsLogEntries)
                {
                    errorEntryList.Add(new ErrorLogEntry(this, ddsLogEntry.Id.ExternalId.ToString(), ddsLogEntry.Entry));
                }

                return query.Count();
            }
        }
    }

}
