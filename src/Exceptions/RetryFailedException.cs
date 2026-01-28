using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaywrightAutomation.Exceptions;

public class RetryFailedException : Exception
{
    public RetryFailedException(string message, Exception innerException) : base(message, innerException) { }
}

public class InstallFailedException : Exception
{
    public InstallFailedException(string message) : base(message) { }

    public InstallFailedException(string message, Exception innerException) : base(message, innerException) { }
}
