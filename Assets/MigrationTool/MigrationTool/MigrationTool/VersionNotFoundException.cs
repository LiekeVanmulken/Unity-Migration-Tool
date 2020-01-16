using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationTool.MigrationTool
{
    class VersionNotFoundException:Exception
    {
        public VersionNotFoundException(string message):base(message)
        {
        }
    }
}
