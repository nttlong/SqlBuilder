using System;
using System.Collections.Generic;
using System.Text;
using XSQL;
namespace GnolMembership.Models
{
    [DbTable("sys_users")]
    public class SysUsers
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
