using System;
using Synapse.Dns.Core;
using NUnit.Framework;

namespace Synapse.Dns.Tests
{
    [TestFixture]
    public class DnsTests
    {
        [Test]
        public void CreateARecord_Without_Hostname_Throw_Exception()
        {
            // Arrange 
            string hostname = "";
            string ipAddress = "";

            // Act
            Exception ex = Assert.Throws<Exception>( () => DnsServices.CreateARecord( hostname, ipAddress ) );

            // Assert
            Assert.That( ex.Message, Is.EqualTo( "Fully qualified host name is not specified." ) );
        }

        [Test]
        public void CreateARecord_Without_IP_Address_Throw_Exception()
        {
            // Arrange 
            string hostname = "XXXXXX";
            string ipAddress = "";

            // Act
            Exception ex = Assert.Throws<Exception>( () => DnsServices.CreateARecord( hostname, ipAddress ) );

            // Assert
            Assert.That( ex.Message, Is.EqualTo( "IP address is not specified." ) );
        }

        [Test]
        public void CreateARecord_With_Invalid_Dns_Server_Throw_Exception()
        {
            // Arrange 
            string hostname = "XXXXXX";
            string ipAddress = "10.2.0.9";
            string dnsServerName = "XXXXXX";

            // Act
            Exception ex = Assert.Throws<Exception>( () => DnsServices.CreateARecord( hostname, ipAddress, dnsServerName: dnsServerName ) );

            // Assert
            Assert.IsTrue( ex.Message.Contains( "The RPC server is unavailable." ) );
        }

        [Test]
        public void CreateARecord_With_Invalid_Dns_Zone_Throw_Exception()
        {
            // Arrange 
            string hostname = "XXXXXX";
            string ipAddress = "10.2.0.9";
            string dnsServerName = ".";
            string dnsZone = "XXXXXX";

            // Act
            Exception ex = Assert.Throws<Exception>( () => DnsServices.CreateARecord( hostname, ipAddress, dnsZone, dnsServerName ) );

            // Assert
            Assert.IsTrue( ex.Message.Contains( "Generic failure" ) );
        }


        [Test]
        public void CreateARecord_With_Valid_Details_Succeed()
        {
            // Arrange 
            string hostname = Utility.GenerateToken( 8 ) + "." + DnsServices.GetJoinedDomainName();
            string ipAddress = Utility.GetRandomIpAddress();
            string dnsServerName = ".";
            string dnsZone = DnsServices.GetJoinedDomainName();

            // Act
            DnsServices.CreateARecord( hostname, ipAddress, dnsZone, dnsServerName );

            // Assert
            Assert.IsTrue(DnsServices.IsExistingARecord(hostname, ipAddress));

            // Cleanup
            DnsServices.DeleteARecord(hostname, ipAddress);

        }


        [Test]
        public void CreatePtrRecord_Without_Hostname_Throw_Exception()
        {
            // Arrange 
            string hostname = "";
            string ipAddress = "";
            string dnsServerName = ".";
            string dnsZone = "";

            // Act
            Exception ex = Assert.Throws<Exception>( () => DnsServices.CreatePtrRecord( hostname, ipAddress, dnsZone, dnsServerName ) );

            // Assert
            Assert.That( ex.Message, Is.EqualTo( "Fully qualified host name is not specified." ) );
        }

        [Test]
        public void CreatePtrRecord_Without_IP_Address_Throw_Exception()
        {
            // Arrange 
            string hostname = $"{Utility.GenerateToken( 8 )}.{DnsServices.GetJoinedDomainName()}";
            string ipAddress = "";
            string dnsServerName = ".";
            string dnsZone = "";

            // Act
            Exception ex = Assert.Throws<Exception>( () => DnsServices.CreatePtrRecord( hostname, ipAddress, dnsZone, dnsServerName ) );

            // Assert
            Assert.That( ex.Message, Is.EqualTo( "IP address is not specified." ) );
        }

        [Test]
        public void CreatePtrRecord_Without_Dns_Zone_Throw_Exception()
        {
            // Arrange 
            string hostname = $"{Utility.GenerateToken( 8 )}.{DnsServices.GetJoinedDomainName()}";
            string ipAddress = "10.2.0.97";
            string dnsServerName = ".";
            string dnsZone = "";

            // Act
            Exception ex = Assert.Throws<Exception>( () => DnsServices.CreatePtrRecord( hostname, ipAddress, dnsZone, dnsServerName ) );

            // Assert
            Assert.That( ex.Message, Is.EqualTo( "DNS zone is not specified." ) );
        }


        [Test]
        public void CreatePtrRecord_With_Invalid_Dns_Server_Throw_Exception()
        {
            // Arrange 
            string hostname = $"{Utility.GenerateToken( 8 )}.{DnsServices.GetJoinedDomainName()}";
            string ipAddress = Utility.GetRandomIpAddress();
            string dnsServerName = "XXXXXX";
            string dnsZone = "XXXXXX";

            // Act
            Exception ex = Assert.Throws<Exception>( () => DnsServices.CreatePtrRecord( hostname, ipAddress, dnsZone, dnsServerName ) );

            // Assert
            Assert.IsTrue( ex.Message.Contains( "The RPC server is unavailable." ) );
        }

        [Test]
        public void CreatePtrRecord_With_Invalid_Dns_Zone_Throw_Exception()
        {
            // Arrange 
            string hostname = $"{Utility.GenerateToken( 8 )}.{DnsServices.GetJoinedDomainName()}";
            string ipAddress = "10.2.0.97";
            string dnsServerName = ".";
            string dnsZone = "XXXXXX";

            // Act
            Exception ex = Assert.Throws<Exception>( () => DnsServices.CreatePtrRecord( hostname, ipAddress, dnsZone, dnsServerName ) );

            // Assert
            Assert.IsTrue( ex.Message.Contains( "Generic failure" ) );
        }

        [Test]
        public void CreatePtrRecord_With_Valid_Details_Succeed()
        {
            // Arrange 
            string hostname = $"{Utility.GenerateToken( 8 )}.{DnsServices.GetJoinedDomainName()}";
            string ipAddress = "10.2.0.97";
            string dnsServerName = ".";
            string dnsZone = "0.2.10.in-addr.arpa";

            // Act
            DnsServices.CreatePtrRecord( hostname, ipAddress, dnsZone, dnsServerName );

            // Assert
            Assert.IsTrue(DnsServices.IsExistingPtrRecord(hostname, ipAddress));

            // Cleanup
            DnsServices.DeletePtrRecord(hostname, ipAddress);
        }

        [Test]
        public void DeleteARecord_Without_Hostname_Throw_Exception()
        {
            // Arrange 
            string hostname = "";
            string ipAddress = "";

            // Act
            Exception ex = Assert.Throws<Exception>( () => DnsServices.DeleteARecord( hostname, ipAddress ) );

            // Assert
            Assert.That( ex.Message, Is.EqualTo( "Fully qualified host name is not specified." ) );
        }

        [Test]
        public void DeleteARecord_With_NonExistent_Hostname_Throw_Exception()
        {
            // Arrange 
            string hostname = "XXXXXX";
            string ipAddress = "";

            // Act
            Exception ex = Assert.Throws<Exception>( () => DnsServices.DeleteARecord( hostname, ipAddress ) );

            // Assert
            Assert.That( ex.Message, Is.EqualTo( "DNS A record is not found." ) );
        }

        [Test]
        public void DeleteARecord_Valid_Hostname_Succeed()
        {
            // Arrange 
            string hostname = Utility.GenerateToken( 8 ) + "." + DnsServices.GetJoinedDomainName();
            string ipAddress = Utility.GetRandomIpAddress();
            string dnsServerName = ".";
            string dnsZone = DnsServices.GetJoinedDomainName();

            // Act
            DnsServices.CreateARecord( hostname, ipAddress, dnsZone, dnsServerName );
            DnsServices.DeleteARecord( hostname, ipAddress );

            // Assert
            Assert.IsFalse(DnsServices.IsExistingARecord(hostname, ipAddress));
        }

        [Test]
        public void DeletePtrRecord_Without_Hostname_Throw_Exception()
        {
            // Arrange 
            string hostname = "";
            string ipAddress = "";

            // Act
            Exception ex = Assert.Throws<Exception>( () => DnsServices.DeletePtrRecord( hostname, ipAddress ) );

            // Assert
            Assert.That( ex.Message, Is.EqualTo( "Fully qualified host name is not specified." ) );
        }

        [Test]
        public void DeletePtrRecord_Without_IP_Address_Throw_Exception()
        {
            // Arrange 
            string hostname = Utility.GenerateToken( 8 ) + "." + DnsServices.GetJoinedDomainName();
            string ipAddress = "";

            // Act
            Exception ex = Assert.Throws<Exception>( () => DnsServices.DeletePtrRecord( hostname, ipAddress ) );

            // Assert
            Assert.That( ex.Message, Is.EqualTo( "IP address is not specified." ) );
        }

        [Test]
        public void DeletePtrRecord_With_NonExistent_Hostname_IP_Address_Throw_Exception()
        {
            // Arrange 
            string hostname = Utility.GenerateToken( 8 ) + "." + DnsServices.GetJoinedDomainName();
            string ipAddress = Utility.GetRandomIpAddress();

            // Act
            Exception ex = Assert.Throws<Exception>( () => DnsServices.DeletePtrRecord( hostname, ipAddress ) );

            // Assert
            Assert.That( ex.Message, Is.EqualTo( "DNS PTR record is not found." ) );
        }

        [Test]
        public void DeletePtrRecord_With_Valid_Details_Succeed()
        {
            // Arrange 
            string hostname = $"{Utility.GenerateToken( 8 )}.{DnsServices.GetJoinedDomainName()}";
            string ipAddress = "10.2.0.97"; // TODO: Generate dynamic ip address
            string dnsServerName = ".";
            string dnsZone = "0.2.10.in-addr.arpa";

            // Act
            DnsServices.CreatePtrRecord( hostname, ipAddress, dnsZone, dnsServerName );
            DnsServices.DeletePtrRecord( hostname, ipAddress );
            
            // Assert
            Assert.IsFalse(DnsServices.IsExistingPtrRecord(hostname, ipAddress));
        }
    }
}
