using Newtonsoft.Json;
using Synapse.Core;
using Synapse.Dns.Core;
using System;
using System.Collections.Generic;
using Synapse.Handlers.DNS;

public class DnsHandler : HandlerRuntimeBase
{
    private DnsHandlerConfig _config;
    private ExecuteResult _result = new ExecuteResult()
    {
        Status = StatusType.Running,
        Sequence = int.MaxValue
    };
    private string _mainProgressMsg = "";
    private string _subProgressMsg = "";

    public override object GetConfigInstance()
    {
        return new DnsHandlerConfig()
        {
            DnsServer = "."
        };
    }


    public override object GetParametersInstance()
    {
        return new DnsRequest()
        {
            DnsActions = new List<DnsActionDetail>()
            {
                new DnsActionDetail()
                {
                    Action = "delete",
                    Hostname = "FullQualifiedDomainName.com",
                    IpAddress = "xxx.xxx.xxx.xxx",
                    Note = "Notes related to teh request.",
                    RecordType = "AType",
                    RequestOwner = "XXXXXX"
                },
                new DnsActionDetail()
                {
                    Action = "delete",
                    Hostname = "FullQualifiedDomainName.com",
                    IpAddress = "xxx.xxx.xxx.xxx",
                    Note = "Notes related to teh request.",
                    RecordType = "PTRType",
                    RequestOwner = "XXXXXX"
                },

            }
        };
    }

    public override IHandlerRuntime Initialize(string values)
    {
        //deserialize the Config from the Handler declaration
        _config = DeserializeOrNew<DnsHandlerConfig>( values );
        return this;
    }

    public override ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        string context = "Execute";

        Exception exception = null;
        DnsResponse response = new DnsResponse
        {
            Status = "",
            Results = new List<ActionResult>()
        };


        try
        {
            _mainProgressMsg = _mainProgressMsg + "Deserializing incoming request parameters...\n";
            DnsRequest parms = DeserializeOrNew<DnsRequest>( startInfo.Parameters );

            if ( parms?.DnsActions != null )
            {
                foreach ( DnsActionDetail request in parms.DnsActions )
                {
                    bool subTaskNoError = false;
                    try
                    {
                        _subProgressMsg = "Verifying request parameters...\n";
                        if ( AreActionParametersValid( request ) )
                        {
                            _subProgressMsg = _subProgressMsg + "Executing request" + (startInfo.IsDryRun ? " in dry run mode...\n" : "...\n");
                            subTaskNoError = ExecuteDnsActionsWithoutError( request, startInfo.IsDryRun );
                        }
                    }
                    catch ( Exception ex )
                    {
                        _subProgressMsg = _subProgressMsg + ex.Message;
                        subTaskNoError = false;
                    }
                    finally
                    {
                        response.Results.Add( new ActionResult()
                        {
                            Action = request.Action,
                            RecordType = request.RecordType,
                            Hostname = request.Hostname,
                            IpAddress = request.IpAddress,
                            ExitCode = subTaskNoError ? 0 : -1,
                            Note = _subProgressMsg
                        } );
                    }
                }
            }
            else
            {
                _result.Status = StatusType.None;
                _mainProgressMsg = $"{_mainProgressMsg}No DNS action is found from the incoming request.";
            }
        }
        catch ( Exception ex )
        {
            exception = ex;
            _mainProgressMsg = $"{_mainProgressMsg}DNS request execution has been aborted due to: {ex.Message}";
            _result.Status = StatusType.Failed;
        }

        response.Status = _mainProgressMsg;
        _result.ExitData = JsonConvert.SerializeObject( response );

        // Final runtime notification, return sequence=Int32.MaxValue by convention to supercede any other status message
        OnProgress( context, _mainProgressMsg, _result.Status, sequence: int.MaxValue, ex: exception );

        return _result;
    }

    public bool AreActionParametersValid(DnsActionDetail request)
    {
        bool areValid = true;

        if ( string.IsNullOrWhiteSpace( request.Action ) || request.Action.ToLower() != "add" && request.Action.ToLower() != "delete" )
        {
            _subProgressMsg = _subProgressMsg + "Allowed action type is 'delete' only.\n";
            areValid = false;
        }
        else if ( string.IsNullOrWhiteSpace( request.RecordType ) || request.RecordType.ToLower() != "atype" && request.Action.ToLower() != "ptrtype" )
        {
            _subProgressMsg = _subProgressMsg + "Allowed record types are 'AType' or 'PTRType' only.\n";
            areValid = false;
        }
        else if ( string.IsNullOrWhiteSpace( request.RequestOwner ) )
        {
            _subProgressMsg = _subProgressMsg + "Request owner must be specified.\n";
            areValid = false;
        }
        else if ( string.IsNullOrWhiteSpace( request.Note ) )
        {
            _subProgressMsg = _subProgressMsg + "Request note must be specified.\n";
            areValid = false;
        }
        else if ( string.IsNullOrWhiteSpace( request.Hostname ) || string.IsNullOrWhiteSpace( request.IpAddress ) )
        {
            _subProgressMsg = _subProgressMsg + "Both hostname and ip address must be specified.\n";
            areValid = false;
        }

        return areValid;
    }

    public bool ExecuteDnsActionsWithoutError(DnsActionDetail request, bool isDryRun = false)
    {
        bool noError = false;

        if ( request.Action.ToLower() == "add" && request.RecordType.ToLower() == "atype" )
        {
            _subProgressMsg = _subProgressMsg + "Adding type A DNS record...\n";
            DnsServices.CreateARecord( request.Hostname, request.IpAddress, "", _config.DnsServer, isDryRun );
            _subProgressMsg = _subProgressMsg + "Operation is successful.";
            noError = true;
        }

        if ( request.Action.ToLower() == "delete" && request.RecordType.ToLower() == "atype" )
        {
            _subProgressMsg = _subProgressMsg + "Deleting type A DNS record...\n";
            DnsServices.DeleteARecord( request.Hostname, request.IpAddress, _config.DnsServer, isDryRun );
            _subProgressMsg = _subProgressMsg + "Operation is successful.";
            noError = true;
        }

        if ( request.Action.ToLower() == "add" && request.RecordType.ToLower() == "ptrtype" )
        {
            _subProgressMsg = _subProgressMsg + "Adding type PTR DNS record...\n";
            DnsServices.CreatePtrRecord( request.Hostname, request.IpAddress, "", _config.DnsServer, isDryRun );
            _subProgressMsg = _subProgressMsg + "Operation is successful.";
            noError = true;

        }

        if ( request.Action.ToLower() == "delete" && request.RecordType.ToLower() == "ptrtype" )
        {
            _subProgressMsg = _subProgressMsg + "Deleting type A DNS record...\n";
            DnsServices.DeletePtrRecord( request.Hostname, request.IpAddress, _config.DnsServer, isDryRun );
            _subProgressMsg = _subProgressMsg + "Operation is successful.";
            noError = true;
        }

        return noError;
    }
}