using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YA.ServiceTemplate.Application.Enums
{
    public enum ApiErrorCodes
    {
        DEFAULT_ERROR = 0,
        UNKNOWN_ERROR = 1,
        INTERNAL_SERVER_ERROR = 20,
        AUTHENTICATION_REQUIRED = 30,
        UNAUTHORIZED_ACCESS = 40,
        MISSING_REQUIRED_HEADER = 80,
        UNSUPPORTED_HEADER = 85,
        INVALID_HEADER_VALUE = 90,
        DUPLICATE_API_CALL = 100,
        RESOURCE_ALREADY_EXISTS = 120
    }
}
