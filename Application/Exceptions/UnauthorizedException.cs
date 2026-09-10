using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Exceptions;

[Serializable]
    public class UnauthorizedException : Exception
    {
         public UnauthorizedException()
    {
    }

    public UnauthorizedException(string? message) : base(message)
    {
    }

    public UnauthorizedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
    }