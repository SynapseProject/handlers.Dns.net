using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Synapse.Core;
using Synapse.Dns.Core;

public class DnsHandler : HandlerRuntimeBase
{
    public object GetConfigInstance()
    {
        return null;
    }

    public object GetParametersInstance()
    {
        return new DnsRequest()
        {
            Action = "delete",
            Hostname = "XXXXXX.XXX.COM",
            IpAddress = "XXX.XXX.XXX.XXX",
            Note = "Mandatory notes on the purpose of the request.",
            RecordType = "PTRType",
            RequestOwner = "XXXXXX"
        };
    }

    public override ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        //int cheapSequence = 0; //used to order message flowing out from the Handler
        const string context = "Execute";
        ExecuteResult result = new ExecuteResult()
        {
            Status = StatusType.Running,
            Sequence = int.MaxValue
        };
        Exception exception = null;
        DnsResponse response = new DnsResponse { Note = "", ExitCode = -1 };

        string msg = "";
        try
        {
            msg = msg + "Deserializing request parameters...\n";
            DnsRequest parms = DeserializeOrNew<DnsRequest>( startInfo.Parameters );

            msg = msg + "Verifying request parameterss...\n";
            if ( string.IsNullOrWhiteSpace( parms.Action) || parms.Action.ToLower() != "delete")
            {
                msg = msg + "Allowed action type is 'delete' only.\n";
                result.Status = StatusType.Failed;
            }
            else if ( string.IsNullOrWhiteSpace( parms.RecordType ) || parms.RecordType.ToLower() != "atype" || parms.Action.ToLower() != "ptrtype" )
            {
                msg = msg + "Allowed record types are 'AType' or 'PTRType' only.\n";
                result.Status = StatusType.Failed;
            }
            else if ( string.IsNullOrWhiteSpace( parms.RequestOwner ) )
            {
                msg = msg + "Request owner must be specified.\n";
                result.Status = StatusType.Failed;
            }
            else if ( string.IsNullOrWhiteSpace(parms.Note))
            {
                msg = msg + "Request note must be specified.\n";
                result.Status = StatusType.Failed;
            }
            else if ( string.IsNullOrWhiteSpace( parms.Hostname ) || string.IsNullOrWhiteSpace( parms.IpAddress ) )
            {
                msg = msg + "Both hostname and ip address must be specified.\n";
                result.Status = StatusType.Failed;
            }
            else
            {
                msg = msg + "Executing request" + ( startInfo.IsDryRun ? " in dry run mode...\n" : "...\n");
                if ( parms.Action.ToLower() == "delete" && parms.RecordType.ToLower() == "atype" )
                {
                    msg = msg + "Deleting type A DNS record...";
                    DnsServices.DeleteARecord( parms.Hostname, parms.IpAddress, ".", startInfo.IsDryRun );
                    msg = msg + "Operation is successful.";
                    result.Status = StatusType.Complete;
                }

                if ( parms.Action.ToLower() == "delete" && parms.RecordType.ToLower() == "ptrtype" )
                {
                    msg = msg + "Deleting type A DNS record...";
                    DnsServices.DeletePtrRecord(parms.Hostname, parms.IpAddress, ".", startInfo.IsDryRun );
                    msg = msg + "Operation is successful.";
                    result.Status = StatusType.Complete;
                }
            }
        }
        catch ( Exception ex )
        {
            exception = ex;
            msg = msg + $"Processing has been aborted due to: {ex.Message}";
            result.Status = StatusType.Failed;
        }

        response.ExitCode = result.Status == StatusType.Complete ? 0 : -1;
        response.Note = msg;
        result.ExitData = JsonConvert.SerializeObject( response );

        // Final runtime notification, return sequence=Int32.MaxValue by convention to supercede any other status message
        OnProgress( context, msg, result.Status, sequence: int.MaxValue, ex: exception );

        return result;
    }
}

public class DnsRequest
{
    public string Action { get; set; }

    public string RecordType { get; set; }

    public string Hostname { get; set; }

    public string IpAddress { get; set; }

    public string RequestOwner { get; set; }

    public string Note { get; set; }
}


public class DnsResponse
{
    public string Note { get; set; }

    public int ExitCode { get; set; }
}