using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaywrightAutomation.Exceptions;

public sealed class AutomationException : Exception
{
    public AutomationFailureCategory Category { get; }
    public bool Retryable { get; }

    public AutomationException(
        string message,
        AutomationFailureCategory category,
        bool retryable,
        Exception? inner = null) : base(message, inner)
    {
        Category = category;
        Retryable = retryable;
    }
}

/// <summary>
/// 重试策略
/// </summary>
public enum AutomationFailureCategory
{
    Unknown,
    PageTimeout,
    ElementNotFound,
    ValidationFailed,
    BusinessRuleRejected,
    LoginExpired,
    ExternalSystemError,
    NetworkError,
    DuplicateOrAlreadyExists
}
