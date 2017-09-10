using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;


    namespace Microsoft.AspNetCore.Identity
    {
        public static class UserClaimsExtensions
        {
            public static string GetFullName(this ClaimsPrincipal principal) {
                string fullName = principal.FindFirstValue(ClaimTypes.GivenName);
                string lastName = principal.FindFirstValue(ClaimTypes.Surname);
                if (!string.IsNullOrEmpty(lastName)) {
                    if (!string.IsNullOrEmpty(fullName))
                        fullName += " ";
                    fullName += lastName;
                }
                return fullName;
            }
        }
    }

