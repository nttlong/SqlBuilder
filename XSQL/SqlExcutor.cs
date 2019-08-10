using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XSQL
{
    public class SqlExcutor
    {
        static Hashtable Ins = new Hashtable();
        static readonly object objLockInstance = new object();
        public static IExcutor GetInstance(string AssemblyName,string InstanceName)
        {
            if(Ins[InstanceName+"@"+ AssemblyName] != null)
            {
                return Ins[InstanceName + "@" + AssemblyName] as IExcutor;
            }
            else
            {
                lock (objLockInstance)
                {
                    if (Ins[InstanceName + "@" + AssemblyName] == null)
                    {
                        var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssemblyName + ".dll");
                        var asm=  System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
                        var t = asm.GetExportedTypes().FirstOrDefault(p => p.Name == InstanceName);
                        Ins[InstanceName + "@" + AssemblyName] = asm.CreateInstance(t.FullName);
                    }
                }
            }
            return Ins[InstanceName + "@" + AssemblyName] as IExcutor;
        }
    }
}
