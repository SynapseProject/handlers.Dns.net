using System;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;

namespace handlers.Dns.net
{
    public partial class DnsServices
    {
        public static void CreateARecord(string hostname, string ipAddress, string dnsZone = "", string dnsServerName = ".", bool isDryRun = false)
        {
            if ( string.IsNullOrWhiteSpace( hostname ) )
            {
                throw new Exception( "Fully qualified host name is not specified." );
            }

            if ( string.IsNullOrWhiteSpace( ipAddress ) )
            {
                throw new Exception( "IP address is not specified." );
            }

            if ( string.IsNullOrWhiteSpace( dnsZone ) )
            {
                dnsZone = GetJoinedDomainName();
            }

            try
            {
                ManagementScope mgmtScope = new ManagementScope( @"\\" + dnsServerName + "\\root\\MicrosoftDNS" );

                ManagementClass mgmtClass = new ManagementClass( mgmtScope, new ManagementPath( "MicrosoftDNS_AType" ), null );

                ManagementBaseObject mgmtParams = mgmtClass.GetMethodParameters( "CreateInstanceFromPropertyData" );
                mgmtParams["ContainerName"] = dnsZone;
                mgmtParams["OwnerName"] = $"{hostname.ToLower()}";
                mgmtParams["IPAddress"] = ipAddress;

                if ( !isDryRun )
                {
                    mgmtClass.InvokeMethod( "CreateInstanceFromPropertyData", mgmtParams, null );
                }
            }
            catch ( Exception ex )
            {
                throw new Exception( ex.Message );
            }
        }

        public static void CreatePtrRecord(string hostname, string ipAddress, string dnsZone, string dnsServerName = ".", bool isDryRun = false)
        {
            if ( string.IsNullOrWhiteSpace( hostname ) )
            {
                throw new Exception( "Fully qualified host name is not specified." );
            }

            if ( string.IsNullOrWhiteSpace( ipAddress ) )
            {
                throw new Exception( "IP address is not specified." );
            }

            if ( string.IsNullOrWhiteSpace( dnsZone ) )
            {
                throw new Exception( "DNS zone is not specified." );
            }

            try
            {
                ManagementScope mgmtScope = new ManagementScope( @"\\" + dnsServerName + "\\root\\MicrosoftDNS" );

                ManagementClass mgmtClass = new ManagementClass( mgmtScope, new ManagementPath( "MicrosoftDNS_PTRType" ), null );

                ManagementBaseObject mgmtParams = mgmtClass.GetMethodParameters( "CreateInstanceFromPropertyData" );

                mgmtParams["ContainerName"] = dnsZone;
                mgmtParams["OwnerName"] = $"{ReverseIpAddress( ipAddress )}.in-addr.arpa";
                mgmtParams["PTRDomainName"] = hostname;

                if ( !isDryRun )
                {
                    mgmtClass.InvokeMethod( "CreateInstanceFromPropertyData", mgmtParams, null );
                }
            }
            catch ( Exception ex )
            {
                throw new Exception( ex.Message );
            }
        }

        public static ManagementObjectCollection QueryDnsRecord(string queryStatement, string dnsServerName = ".")
        {
            ManagementObjectCollection col = null;
            ObjectQuery query = new ObjectQuery( queryStatement );
            ManagementScope scope = new ManagementScope( @"\\" + dnsServerName + "\\root\\MicrosoftDNS" );
            scope.Connect();
            ManagementObjectSearcher s = new ManagementObjectSearcher( scope, query );
            try
            {
                col = s.Get();
            }
            catch ( Exception ex )
            {
                Console.WriteLine( ex.Message );
            }
            return col;
        }

        public static string GetJoinedDomainName()
        {
            return IPGlobalProperties.GetIPGlobalProperties().DomainName;
        }

        public static void DeleteARecord(string hostname, string dnsServerName = ".", bool isDryRun = false)
        {
            if ( string.IsNullOrWhiteSpace( hostname ) )
            {
                throw new Exception( "Fully qualified host name is not specified." );
            }

            try
            {
                ObjectQuery query = new ObjectQuery( $"SELECT * FROM MicrosoftDNS_AType WHERE OwnerName = '{hostname}'" );
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
                else
                {
                    throw new Exception( "DNS A record is not found." );
                }
            }
            catch ( Exception ex )
            {
                throw new Exception( ex.Message );
            }
        }

        public static void DeletePtrRecord(string hostname, string ipAddress, string dnsServerName = ".", bool isDryRun = false)
        {
            if ( string.IsNullOrWhiteSpace( hostname ) )
            {
                throw new Exception( "Fully qualified host name is not specified." );
            }

            if ( string.IsNullOrWhiteSpace( ipAddress ) )
            {
                throw new Exception( "IP address is not specified." );
            }

            if ( string.IsNullOrWhiteSpace( dnsServerName ) )
            {
                throw new Exception( "DNS server name is not specified." );
            }

            try
            {
                ObjectQuery query = new ObjectQuery( $"SELECT * FROM MicrosoftDNS_PTRType WHERE OwnerName = '{ReverseIpAddress( ipAddress )}.in-addr.arpa' AND PTRDomainName = '{hostname}.'" );
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
                else
                {
                    throw new Exception( "DNS PTR record is not found." );
                }
            }
            catch ( Exception ex )
            {
                throw new Exception( ex.Message );
            }
        }

        public static bool IsExistingARecord()
        {
            return true;
        }

        public static bool IsExistingPtrRecord()
        {
            return true;
        }

        public static string ReverseIpAddress(string s)
        {
            return string.Join( ".", s.Split( '.' ).Reverse() );
        }
    }
}
