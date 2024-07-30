using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EziOrm
{
    public enum EntityState
    {
        Detached,
        Unchanged,
        Added,
        Modified,
        Deleted
    }
}
