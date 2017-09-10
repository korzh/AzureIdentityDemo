using AzureIdentityDemo.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AzureIdentityDemo.Services
{
    public class MyUserClaimsPrincipalFactory<TUser, TRole> : UserClaimsPrincipalFactory<TUser, TRole>
    where TUser : class
    where TRole : class
    {
        public MyUserClaimsPrincipalFactory(
            UserManager<TUser> userManager,
            RoleManager<TRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor) {
        }


        public override async Task<ClaimsPrincipal> CreateAsync(TUser usr) {
            var principal = await base.CreateAsync(usr);
            ApplicationUser user = usr as ApplicationUser;
            var identity = principal.Identities.First();
            identity.AddClaim(new Claim(ClaimTypes.GivenName, user.FirstName ?? ""));
            identity.AddClaim(new Claim(ClaimTypes.Surname, user.LastName ?? ""));

            return principal;
        }

    }
}
