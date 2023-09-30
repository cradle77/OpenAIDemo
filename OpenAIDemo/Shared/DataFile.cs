using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAIDemo.Shared
{
    public class DataFile
    {
        public DataFile()
        {

        }

        public DataFile(Guid id)
        {
            this.Id = id;
        }

        public string FileName { get; set; }

        public string FilePath => $"raw/{this.FileName}";

        public string Url { get; set; }

        public Guid Id { get; set; }
    }
}
