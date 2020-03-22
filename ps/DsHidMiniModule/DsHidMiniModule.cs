using System;
using System.Management.Automation;

namespace DsHidMiniModule
{
    [Cmdlet(VerbsCommon.Get, "FireShockDevice")]
    [OutputType(typeof(int))]
    public class GetFireShockDevice : Cmdlet
    {
        protected override void ProcessRecord()
        {
            
        }
    }
}
