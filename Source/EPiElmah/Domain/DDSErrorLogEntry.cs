using System;
using Elmah;
using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace JarleF.EPiElmah.Domain
{
    public class DDSErrorLogEntry : IDynamicData, IComparable<DDSErrorLogEntry>
    {
        public DDSErrorLogEntry()
        {
            Initialize();
        }

        public DDSErrorLogEntry(Error entry)
        {
            Initialize();

            Entry = entry;
        }

        private void Initialize()
        {
            Id = Identity.NewIdentity(Guid.NewGuid());
        }

        // Properties
        public Identity Id { get; set; }
        public Error Entry { get; set; }

        public int CompareTo(DDSErrorLogEntry other)
        {
            if(Entry.Time.CompareTo(other.Entry.Time) == 0)
            {
                return Id.ToString().CompareTo(other.Id.ToString());
            }
            
            return Entry.Time.CompareTo(other.Entry.Time);
        }
    }
}
