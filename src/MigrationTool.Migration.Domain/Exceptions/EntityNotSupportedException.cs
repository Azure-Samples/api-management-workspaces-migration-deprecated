using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationTool.Migration.Domain.Exceptions;

public class EntityNotSupportedException : Exception
{
    public EntityNotSupportedException(string message) : base(message) { }
}
