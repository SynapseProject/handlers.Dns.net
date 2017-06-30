using System;
using System.Management;
using System.Net.NetworkInformation;

namespace handlers.Dns.net
{
    public partial class DnsServices
    {
        public static void CreateARecord(string dnsZone, string hostName, string ipAddress, string dnsServerName = ".", bool isDryRun = false)
        {
            ManagementScope mgmtScope = new ManagementScope( @"\\.\Root\MicrosoftDNS" );

            ManagementClass mgmtClass = new ManagementClass( mgmtScope, new ManagementPath( "MicrosoftDNS_AType" ), null );

            ManagementBaseObject mgmtParams = mgmtClass.GetMethodParameters( "CreateInstanceFromPropertyData" );
            mgmtParams["DnsServerName"] = Environment.MachineName;
            mgmtParams["ContainerName"] = dnsZone;
            mgmtParams["OwnerName"] = $"{hostName.ToLower()}.{dnsZone}";
            mgmtParams["IPAddress"] = ipAddress;

            mgmtClass.InvokeMethod( "CreateInstanceFromPropertyData", mgmtParams, null );
        }

        public static string GetJoinedDomainName()
        {
            return IPGlobalProperties.GetIPGlobalProperties().DomainName;
        }

        public static void DeleteARecord(string aRecordName, string dnsServerName = ".", string dnsZone = "", bool isDryRun = false)
        {
            if ( string.IsNullOrWhiteSpace( aRecordName ) )
            {
                throw new Exception( "A record name is not specified.");
            }

            if ( string.IsNullOrWhiteSpace( dnsServerName ) )
            {
                throw new Exception( "DNS server name is not specified.");
            }

            if ( string.IsNullOrWhiteSpace( dnsServerName ) )
            {
                dnsZone = GetJoinedDomainName();
            }

            try
            {
                ObjectQuery query = new ObjectQuery( $"SELECT * FROM MicrosoftDNS_AType WHERE OwnerName = '{aRecordName}.{dnsZone}'" );
                ManagementScope scope = new ManagementScope( @"\\" + dnsServerName + "\\root\\MicrosoftDNS" );
                scope.Connect();
                ManagementObjectSearcher s = new ManagementObjectSearcher( scope, query );
                ManagementObjectCollection col = s.Get();
                if ( col.Count > 0 )
                {
                    if ( !isDryRun )
                    {
                        foreach ( ManagementBaseObject o in col )
                        {
                            ManagementObject obj = (ManagementObject)o;
                            obj.Delete();
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                throw new Exception( $"Encountered exception while trying to delete A record: {ex.Message}" );
            }
        }

        public static void DeletePtrRecord(string ipAddress, string dnsServerName = ".", string dnsZone = "", bool isDryRun = false)
        {
            if ( string.IsNullOrWhiteSpace( ipAddress ) )
            {
                throw new Exception( "IP address is not specified.");
            }

            if ( string.IsNullOrWhiteSpace( dnsServerName ) )
            {
                throw new Exception( "DNS server name is not specified.");
            }

            if ( string.IsNullOrWhiteSpace( dnsServerName ) )
            {
                dnsZone = GetJoinedDomainName();
            }

            try
            {
                ObjectQuery query = new ObjectQuery( $"SELECT * FROM MicrosoftDNS_PTRType WHERE OwnerName LIKE '{ipAddress}%'" );
                ManagementScope scope = new ManagementScope( @"\\" + dnsServerName + "\\root\\MicrosoftDNS" );
                scope.Connect();
                ManagementObjectSearcher s = new ManagementObjectSearcher( scope, query );
                ManagementObjectCollection col = s.Get();
                if ( col.Count > 0 )
                {
                    if ( !isDryRun )
                    {
                        foreach ( ManagementBaseObject o in col )
                        {
                            ManagementObject obj = (ManagementObject)o;
                            obj.Delete();
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                throw new Exception( $"Encountered exception while trying to delete PTR record: {ex.Message}" );
            }
        }

    }
}
