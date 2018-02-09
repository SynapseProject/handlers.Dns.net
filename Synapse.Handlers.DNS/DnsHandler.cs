using Newtonsoft.Json;
using Synapse.Core;
using Synapse.Dns.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class DnsHandler : HandlerRuntimeBase
{
    private DnsHandlerConfig _config;
    private readonly ExecuteResult _result = new ExecuteResult()
    {
        Status = StatusType.None,
        BranchStatus = StatusType.None,
        Sequence = int.MaxValue
    };
    private string _mainProgressMsg = "";
    private string _subProgressMsg = "";

    public override object GetConfigInstance()
    {
        return new DnsHandlerConfig()
        {
            DnsServers = new List<DnsServer>()
            {
                new DnsServer() { DomainSuffix = "xxx.com", ServerName = "abc.xxx.com" },
                new DnsServer() { DomainSuffix = "yyy.com", ServerName = "def.yyy.com" }
            }
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
        try
        {
            _config = DeserializeOrNew<DnsHandlerConfig>( values ) ?? new DnsHandlerConfig();
        }
        catch ( Exception ex )
        {
            OnLogMessage( "Initialization", "Encountered exception while deserializing handler config.", LogLevel.Error, ex );
        }

        return this;
    }

    public override ExecuteResult Execute(HandlerStartInfo startInfo)
    {
        int sequenceNumber = 0;
        string context = "Execute";
        DnsResponse response = new DnsResponse
        {
            Results = new List<ActionResult>()
        };

        try
        {
            _mainProgressMsg = "Deserializing incoming request...";
            _result.Status = StatusType.Initializing;
            ++sequenceNumber;
            OnProgress( context, _mainProgressMsg, _result.Status, sequenceNumber );
            OnLogMessage( context, _subProgressMsg );
            DnsRequest parms = DeserializeOrNew<DnsRequest>( startInfo.Parameters );


            _mainProgressMsg = "Processing individual child request...";
            _result.Status = StatusType.Running;
            ++sequenceNumber;
            OnProgress( context, _mainProgressMsg, _result.Status, sequenceNumber );

            if ( parms?.DnsActions != null )
            {
                foreach ( DnsActionDetail request in parms.DnsActions )
                {
                    bool subTaskSucceed = false;
                    try
                    {
                        _subProgressMsg = "Verifying child request parameters...";
                        OnLogMessage( context, _subProgressMsg );
                        OnLogMessage( context, request.ToString() );

                        if ( ValidateActionParameters( request ) )
                        {
                            _subProgressMsg = "Executing request" + (startInfo.IsDryRun ? " in dry run mode..." : "...");
                            OnLogMessage( context, _subProgressMsg );
                            subTaskSucceed = ExecuteDnsActions( request, _config, startInfo.IsDryRun );
                            _subProgressMsg = "Processed child request.";
                            OnLogMessage( context, _subProgressMsg );
                        }
                    }
                    catch ( Exception ex )
                    {
                        _subProgressMsg = ex.Message;
                        OnLogMessage( context, _subProgressMsg );
                        subTaskSucceed = false;
                    }
                    finally
                    {
                        response.Results.Add( new ActionResult()
                        {
                            Action = request.Action,
                            RecordType = request.RecordType,
                            Hostname = request.Hostname,
                            IpAddress = request.IpAddress,
                            ExitCode = subTaskSucceed ? 0 : -1,
                            Note = _subProgressMsg
                        } );
                    }
                }
                _result.Status = StatusType.Complete;
            }
            else
            {
                _result.Status = StatusType.Failed;
                _mainProgressMsg = "No DNS action is found from the incoming request.";
                OnLogMessage( context, _mainProgressMsg, LogLevel.Error );
            }
        }
        catch ( Exception ex )
        {
            _result.Status = StatusType.Failed;
            _mainProgressMsg = $"Execution has been aborted due to: {ex.Message}";
            OnLogMessage( context, _mainProgressMsg, LogLevel.Error );
        }

        _mainProgressMsg = startInfo.IsDryRun ? "Dry run execution is completed." : "Execution is completed.";
        response.Summary = _mainProgressMsg;
        _result.ExitData = JsonConvert.SerializeObject( response );

        OnProgress( context, _mainProgressMsg, _result.Status, int.MaxValue );

        return _result;
    }

    public bool ValidateActionParameters(DnsActionDetail request)
    {
        bool areValid = true;

        if ( string.IsNullOrWhiteSpace( request.Action ) || request.Action.ToLower() != "add" && request.Action.ToLower() != "delete" )
        {
            OnLogMessage( "Execute", "Allowed action type is 'add' or 'delete' only.", LogLevel.Error );
            areValid = false;
        }
        else if ( string.IsNullOrWhiteSpace( request.RecordType ) || request.RecordType.ToLower() != "atype" && request.RecordType.ToLower() != "ptrtype" )
        {
            OnLogMessage( "Execute", "Allowed record types are 'AType' or 'PTRType' only.", LogLevel.Error );
            areValid = false;
        }
        else if ( string.IsNullOrWhiteSpace( request.RequestOwner ) )
        {
            OnLogMessage( "Execute", "Request owner must be specified.", LogLevel.Error );
            areValid = false;
        }
        else if ( string.IsNullOrWhiteSpace( request.Note ) )
        {
            OnLogMessage( "Execute", "Request note must be specified.", LogLevel.Error );
            areValid = false;
        }
        else if ( string.IsNullOrWhiteSpace( request.Hostname ) || string.IsNullOrWhiteSpace( request.IpAddress ) )
        {
            OnLogMessage( "Execute", "Both hostname and ip address must be specified.", LogLevel.Error );
            areValid = false;
        }

        return areValid;
    }

    public bool ExecuteDnsActions(DnsActionDetail request, DnsHandlerConfig config, bool isDryRun = false)
    {
        if ( request == null || config == null ) return false;

        string domainSuffix = GetDomainSuffix( request.Hostname );
        if ( string.IsNullOrWhiteSpace( domainSuffix ) )
            throw new Exception( $"Domain suffix {domainSuffix} is not valid." );

        string dnsServer = GetDnsServer( config, domainSuffix );
        if ( string.IsNullOrWhiteSpace( dnsServer ) )
            throw new Exception( $"Unable to find a matching DNS server for domain suffix {domainSuffix}." );

        bool isSuccess = false;
        if ( request.Action.ToLower() == "add" && request.RecordType.ToLower() == "atype" )
        {
            OnLogMessage( "Execute", "Adding type A DNS record..." );
            DnsServices.CreateARecord( request.Hostname, request.IpAddress, "", dnsServer, isDryRun );
            OnLogMessage( "Execute", "Operation is successful." );
            isSuccess = true;
        }

        if ( request.Action.ToLower() == "delete" && request.RecordType.ToLower() == "atype" )
        {
            OnLogMessage( "Execute", "Deleting type A DNS record and its associated PTR record..." );
            DnsServices.DeleteARecord( request.Hostname, request.IpAddress, dnsServer, isDryRun );
            OnLogMessage( "Execute", "Operation is successful." );
            isSuccess = true;
        }

        if ( request.Action.ToLower() == "add" && request.RecordType.ToLower() == "ptrtype" )
        {
            OnLogMessage( "Execute", "Adding type PTR DNS record..." );
            DnsServices.CreatePtrRecord( request.Hostname, request.IpAddress, request.DnsZone, dnsServer, isDryRun );
            OnLogMessage( "Execute", "Operation is successful." );
            isSuccess = true;
        }

        if ( request.Action.ToLower() == "delete" && request.RecordType.ToLower() == "ptrtype" )
        {
            OnLogMessage( "Execute", "Deleting type PTR DNS record..." );
            DnsServices.DeletePtrRecord( request.Hostname, request.IpAddress, dnsServer, isDryRun );
            OnLogMessage( "Execute", "Operation is successful." );
            isSuccess = true;
        }

        return isSuccess;
    }

    #region Utility Methods

    public string GetDomainSuffix(string serverName)
    {
        string domainSuffix = "";

        if ( !string.IsNullOrWhiteSpace( serverName ) )
        {
            int idx = serverName.IndexOf( '.' );
            if ( idx != -1 )
            {
                domainSuffix = serverName.Substring( idx + 1 );
            }
        }
        return domainSuffix;
    }

    public string GetDnsServer(DnsHandlerConfig config, string domainSuffix)
    {
        string dnsServer = "";

        if ( config?.DnsServers != null && !string.IsNullOrWhiteSpace( domainSuffix ) )
        {
            foreach ( DnsServer ds in config.DnsServers )
            {
                if ( string.Compare( ds.DomainSuffix, domainSuffix, StringComparison.OrdinalIgnoreCase ) == 0 )
                {
                    dnsServer = ds.ServerName;
                    break;
                }
            }
        }
        return dnsServer;
    }

    private static string RemoveParameterSingleQuote(string input)
    {
        string output = "";
        if ( !string.IsNullOrWhiteSpace( input ) )
        {
            Regex pattern = new Regex( ":\\s*'" );
            output = pattern.Replace( input, ": " );
            pattern = new Regex( "'\\s*(\r\n|\r|\n|$)" );
            output = pattern.Replace( output, Environment.NewLine );
        }
        return output;
    }
    #endregion
}