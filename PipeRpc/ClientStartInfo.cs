using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeRpc
{
    public class ClientStartInfo
    {
        public string FileName { get; }
        public string WorkingDirectory { get; }
        public IList<string> Arguments { get; }

        public ClientStartInfo(string fileName, string workingDirectory, params string[] arguments)
        {
            FileName = fileName;
            WorkingDirectory = workingDirectory;
            Arguments = new ReadOnlyCollection<string>(arguments.ToList());
        }
    }
}
