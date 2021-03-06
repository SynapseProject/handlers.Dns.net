﻿public class DnsActionDetail
{
    public string Action { get; set; }

    public string RecordType { get; set; }

    public string Hostname { get; set; }

    public string IpAddress { get; set; }

    public string DnsZone { get; set; }

    public string RequestOwner { get; set; }

    public string Note { get; set; }

    public override string ToString()
    {
        return $"Action: {Action}, RecordType: {RecordType}, Hostname: {Hostname}, IpAddress: {IpAddress}, RequestOwner:{RequestOwner}, Note: {Note}";
    }
}
