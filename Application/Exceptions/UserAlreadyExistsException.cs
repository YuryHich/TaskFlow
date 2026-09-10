using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Application.Exceptions;

[Serializable]

    public class UserAlreadyExistsException : Exception
    {
       public UserAlreadyExistsException()
    {
    }

    public UserAlreadyExistsException(string? message) : base(message)
    {
    }

    public UserAlreadyExistsException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
    }
