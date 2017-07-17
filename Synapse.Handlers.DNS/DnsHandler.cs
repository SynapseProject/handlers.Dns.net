using Newtonsoft.Json;
using Synapse.Core;
using Synapse.Dns.Core;
using System;
using System.Collections.Generic;
using NLog;
using Synapse.Handlers.DNS;
using LogLevel = NLog.LogLevel;

public class DnsHandler : HandlerRuntimeBase
{
    private static Logger _logger = LogManager.GetCurrentClassLogger();
    private DnsHandlerConfig _config;
    private readonly ExecuteResult _result = new ExecuteResult()
    {
        Status = StatusType.None,
        BranchStatus = StatusType.None,
        Sequence = int.MaxValue
    };
    private string _progressMsg = "";

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
                    Note = "Notes related to the request.",
                    RecordType = "AType",
                    RequestOwner = "XXXXXX"
                },
                new DnsActionDetail()
                {
                    Action = "delete",
                    Hostname = "FullQualifiedDomainName.com",
                    IpAddress = "xxx.xxx.xxx.xxx",
                    Note = "Notes related to the request.",
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
            Results = new List<ActionResult>()
        };


        try
        {
            OnLogMessage( "Deserializing incoming request...", LogLevel.Info );
            _result.Status = StatusType.Initializing;
            DnsRequest parms = DeserializeOrNew<DnsRequest>( startInfo.Parameters );

            _result.Status = StatusType.Running;
            if ( parms?.DnsActions != null )
            {
                bool subTaskNoError = true;
                foreach ( DnsActionDetail request in parms.DnsActions )
                {
                    try
                    {
                        OnLogMessage( "Verifying request parameters...", LogLevel.Info, true );
                        OnLogMessage( request.ToString(), LogLevel.Info );
                        if ( AreActionParametersValid( request ) )
                        {
                            OnLogMessage( "Executing request" + (startInfo.IsDryRun ? " in dry run mode..." : "..."), LogLevel.Info );
                            subTaskNoError = ExecuteDnsActionsWithoutError( request, startInfo.IsDryRun );
                        }
                    }
                    catch ( Exception ex )
                    {
                        OnLogMessage( ex.Message, LogLevel.Error );
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
                            Note = _progressMsg
                        } );
                    }
                }
                _result.Status = subTaskNoError ? StatusType.Complete : StatusType.CompletedWithErrors;
            }
            else
            {
                _result.Status = StatusType.Failed;
                OnLogMessage( "No DNS action is found from the incoming request.", LogLevel.Warn );
            }
        }
        catch ( Exception ex )
        {
            exception = ex;
            OnLogMessage( "DNS request execution has been aborted due to: {ex.Message}", LogLevel.Error );
            _result.Status = StatusType.Failed;
        }

        _result.ExitData = JsonConvert.SerializeObject( response );

        // Final runtime notification, return sequence=Int32.MaxValue by convention to supercede any other status message
        OnProgress( context, _progressMsg, _result.Status, sequence: int.MaxValue, ex: exception );

        return _result;
    }

    public bool AreActionParametersValid(DnsActionDetail request)
    {
        bool areValid = true;

        if ( string.IsNullOrWhiteSpace( request.Action ) || request.Action.ToLower() != "add" && request.Action.ToLower() != "delete" )
        {
            OnLogMessage( "Allowed action type is 'add' or 'delete' only.", LogLevel.Error );
            areValid = false;
        }
        else if ( string.IsNullOrWhiteSpace( request.RecordType ) || request.RecordType.ToLower() != "atype" && request.RecordType.ToLower() != "ptrtype" )
        {
            OnLogMessage( "Allowed record types are 'AType' or 'PTRType' only.", LogLevel.Error );
            areValid = false;
        }
        else if ( string.IsNullOrWhiteSpace( request.RequestOwner ) )
        {
            OnLogMessage( "Request owner must be specified.", LogLevel.Error );
            areValid = false;
        }
        else if ( string.IsNullOrWhiteSpace( request.Note ) )
        {
            OnLogMessage( "Request note must be specified.", LogLevel.Error );
            areValid = false;
        }
        else if ( string.IsNullOrWhiteSpace( request.Hostname ) || string.IsNullOrWhiteSpace( request.IpAddress ) )
        {
            OnLogMessage( "Both hostname and ip address must be specified.", LogLevel.Error );
            areValid = false;
        }

        return areValid;
    }

    public bool ExecuteDnsActionsWithoutError(DnsActionDetail request, bool isDryRun = false)
    {
        bool noError = false;

        if ( request.Action.ToLower() == "add" && request.RecordType.ToLower() == "atype" )
        {
            OnLogMessage( "Adding type A DNS record...", LogLevel.Info );
            DnsServices.CreateARecord( request.Hostname, request.IpAddress, "", _config.DnsServer, isDryRun );
            OnLogMessage( "Operation is successful.", LogLevel.Info );
            noError = true;
        }

        if ( request.Action.ToLower() == "delete" && request.RecordType.ToLower() == "atype" )
        {
            OnLogMessage( "Deleting type A DNS record and its associated PTR record...", LogLevel.Info );
            DnsServices.DeleteARecord( request.Hostname, request.IpAddress, _config.DnsServer, isDryRun );
            _progressMsg = _progressMsg + "Operation is successful.";
            noError = true;
        }

        if ( request.Action.ToLower() == "add" && request.RecordType.ToLower() == "ptrtype" )
        {
            OnLogMessage( "Adding type PTR DNS record...", LogLevel.Info );
            DnsServices.CreatePtrRecord( request.Hostname, request.IpAddress, request.DnsZone, _config.DnsServer, isDryRun );
            OnLogMessage( "Operation is successful.", LogLevel.Info );
            noError = true;

        }

        if ( request.Action.ToLower() == "delete" && request.RecordType.ToLower() == "ptrtype" )
        {
            OnLogMessage( "Deleting type PTR DNS record...", LogLevel.Info );
            DnsServices.DeletePtrRecord( request.Hostname, request.IpAddress, _config.DnsServer, isDryRun );
            OnLogMessage( "Operation is successful.", LogLevel.Info );
            noError = true;
        }

        return noError;
    }

    private void OnLogMessage(string message, LogLevel logLevel, bool resetMessage = false)
    {
        if ( string.IsNullOrWhiteSpace( message ) )
        {
            return;
        }

        if ( resetMessage )
        {
            _progressMsg = "";
        }

        if ( logLevel == LogLevel.Debug )
        {
            _logger.Debug( message );
        }
        else if ( logLevel == LogLevel.Error )
        {
            _logger.Error( message );
        }
        else if ( logLevel == LogLevel.Fatal )
        {
            _logger.Fatal( message );
        }
        else if ( logLevel == LogLevel.Warn )
        {
            _logger.Warn( message );
        }
        else if ( logLevel == LogLevel.Info )
        {
            _logger.Info( message );
        }

        _progressMsg = _progressMsg + "\n" + message;
    }
}