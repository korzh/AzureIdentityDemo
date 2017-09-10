using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;


namespace AzureIdentityDemo.Models
{
    public class ApplicationUser : TableEntity
    {
        [IgnoreProperty]
        public string Id {
            get { return RowKey; }
            set { RowKey = value; }
        }

        [IgnoreProperty]
        public string UserName {
            get { return Email; }
            set { Email = value; }
        }

        public string Email { get; set; }

        public string NormalizedEmail { get; set; }

        public bool EmailConfirmed { get; set; }

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string RolesStr { get; set; } = "";

        public string PasswordHash { get; set; }

        public string PhoneNumber { get; set; }

        public bool TwoFactorEnabled { get; set; }
    }
}
