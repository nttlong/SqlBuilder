using System;
using System.Collections.Generic;
using System.Text;

namespace XSQL
{
    public interface IExcutor
    {
        XSqlCommand GetCommand(object Sql);
    }
}
