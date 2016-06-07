using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OneDriveBot.Models
{
    [Serializable]
    public class File
    {
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public string ID { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTimeOffset LastModifiedDate { get; set; }
        public string Name { get; set; }
        public string WebURL { get; set; }

        public override string ToString()
        {
            
            return this.Name;
        }
    }
}