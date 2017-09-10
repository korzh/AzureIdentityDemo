using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

using Microsoft.WindowsAzure.Storage.Table;
using Korzh.WindowsAzure.Storage;

using AzureIdentityDemo.Models;

namespace AzureIdentityDemo.Services
{

    public class MyAzureUserStore : IUserRoleStore<ApplicationUser>, IUserPasswordStore<ApplicationUser>, IQueryableUserStore<ApplicationUser>, IUserEmailStore<ApplicationUser>
    {
        private TableStorageService<ApplicationUser> userTable;
        private readonly ILookupNormalizer _normalizer;

        public IQueryable<ApplicationUser> Users => GetAllUsersAsync().Result.AsQueryable();

        private string _defaultPartitionKey = "Users";


        public MyAzureUserStore(DefaultAzureStorageContext context, ILookupNormalizer normalizer) {
            this.userTable = new TableStorageService<ApplicationUser>(context, "UsersTable");
            this._normalizer = normalizer;
        }

        public Task<IEnumerable<ApplicationUser>> GetAllUsersAsync() {
            return userTable.GetEntitiesByFilterAsync();
        }


        //------------ IUserRoleStore -------------
        public async Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) {
            return await Task.FromResult(user.RowKey);
        }

        public async Task<string> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) {
            return await Task.FromResult(user.Email);
        }

        public Task<ApplicationUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken)) {
            return userTable.GetEntityByKeysAsync(_defaultPartitionKey, userId);
        }

        public async Task<ApplicationUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) {
            var users = await userTable.ListEntitiesByFilterAsync(new Dictionary<string, object> {
                {"PartitionKey", _defaultPartitionKey },
                { nameof(ApplicationUser.NormalizedEmail), normalizedUserName }
            });

            return users.FirstOrDefault();
        }

        public async Task<string> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) {
            return await Task.FromResult(user.NormalizedEmail);
        }

        private void NormalizeUserBeforeInsertOrUpdate(ApplicationUser user) {
            if (user.RowKey == null) {
                user.RowKey = Guid.NewGuid().ToString();
            }

            user.PartitionKey = _defaultPartitionKey;
        }

        public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken = default(CancellationToken)) {
            try {
                NormalizeUserBeforeInsertOrUpdate(user);
                await userTable.InsertOrMergeEntityAsync(user);
                return IdentityResult.Success;
            }
            catch (Exception ex) {
                return IdentityResult.Failed(new IdentityError {
                    Code = ex.GetType().Name,
                    Description = ex.Message
                });
            }
        }

        public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default(CancellationToken)) {
            try {
                await userTable.DeleteEntityAsync(user);
                return IdentityResult.Success;
            }
            catch (Exception ex) {
                return IdentityResult.Failed(new IdentityError {
                    Code = ex.GetType().Name,
                    Description = ex.Message
                });
            }
        }

        public async Task SetNormalizedUserNameAsync(ApplicationUser user, string normalizedName, CancellationToken cancellationToken) {
            user.NormalizedEmail = normalizedName;
            await userTable.InsertOrMergeEntityAsync(user);
        }

        public async Task SetUserNameAsync(ApplicationUser user, string userName, CancellationToken cancellationToken) {
            user.Email = userName;
            await userTable.InsertOrMergeEntityAsync(user);
        }

        public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) {
            try {
                NormalizeUserBeforeInsertOrUpdate(user);
                await userTable.InsertOrMergeEntityAsync(user);
                return IdentityResult.Success;
            }
            catch (Exception ex) {
                return IdentityResult.Failed(new IdentityError { Code = ex.GetType().Name, Description = ex.Message });
            }
        }


        public async Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) {
            if (string.IsNullOrEmpty(roleName))
                throw new ArgumentException("Empty rolename", nameof(roleName));

            var roles = user.RolesStr != null ? user.RolesStr.Split(',').ToHashSet() : new HashSet<string>();
            roles.Add(roleName);
            user.RolesStr = string.Join(",", roles);

            NormalizeUserBeforeInsertOrUpdate(user);
            await userTable.InsertOrMergeEntityAsync(user);
        }

        public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken) {
            var roles = user.RolesStr != null ? user.RolesStr.Split(',').ToList() : new List<string>();

            return Task.FromResult<IList<string>>(roles);
        }


        public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken) {
            return (await userTable.GetEntitiesByFilterAsync(""))
                .Where(u => u.PartitionKey == _defaultPartitionKey && u.RolesStr.IndexOf(roleName) >= 0)
                .ToList();
        }

        public Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) {
            return Task.FromResult(!string.IsNullOrEmpty(user.RolesStr) && user.RolesStr.Contains(roleName));
        }

        public async Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) {
            if (!string.IsNullOrEmpty(roleName)) {
                var roles = user.RolesStr != null ? user.RolesStr.Split(',').ToHashSet() : new HashSet<string>();
                roles.Remove(roleName);
                user.RolesStr = string.Join(",", roles);

                NormalizeUserBeforeInsertOrUpdate(user);
                await userTable.InsertOrMergeEntityAsync(user);
            }
        }

        public void Dispose() {
        }


        // --------------- IUserPasswordStore -------------
        public async Task SetPasswordHashAsync(ApplicationUser user, string passwordHash, CancellationToken cancellationToken) {
            user.PasswordHash = passwordHash;

            NormalizeUserBeforeInsertOrUpdate(user);
            await userTable.InsertOrMergeEntityAsync(user);
        }

        public Task<string> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken) {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken) {
            return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        }

        //--------------- IUserEmailStore -------------
        public async Task<ApplicationUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken)) {
            var users = await userTable.ListEntitiesByFilterAsync(new Dictionary<string, object>() {
                {"PartitionKey", _defaultPartitionKey },
                {nameof(ApplicationUser.NormalizedEmail), normalizedEmail}
            });

            return users.FirstOrDefault();
        }

        public async Task SetEmailAsync(ApplicationUser user, string email, CancellationToken cancellationToken) {
            user.Email = email;
            NormalizeUserBeforeInsertOrUpdate(user);
            await userTable.InsertOrMergeEntityAsync(user);
        }

        public Task<string> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken) {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken) {
            return Task.FromResult(user.EmailConfirmed);
        }

        public async Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken) {
            user.EmailConfirmed = confirmed;
            NormalizeUserBeforeInsertOrUpdate(user);
            await userTable.InsertOrMergeEntityAsync(user);
        }


        public Task<string> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken) {
            return Task.FromResult(user.NormalizedEmail);
        }

        public async Task SetNormalizedEmailAsync(ApplicationUser user, string normalizedEmail, CancellationToken cancellationToken) {
            user.NormalizedEmail = normalizedEmail;

            NormalizeUserBeforeInsertOrUpdate(user);
            await userTable.InsertOrMergeEntityAsync(user);
        }
    }


    public static class EnumarableExtensions
    {
        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source) => source.ToHashSet(comparer: null);

        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            // Don't pre-allocate based on knowledge of size, as potentially many elements will be dropped.
            return new HashSet<TSource>(source, comparer);
        }
    }

}