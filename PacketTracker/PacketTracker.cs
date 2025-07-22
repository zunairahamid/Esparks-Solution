using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

class PacketTracker
{
    //status flags defined
    static readonly string[] STATUS_FLAGS = new[]
{
    "Treatment Active",
    "Cleaning Mode",
    "Standby Mode",
    "Alarm Active",
    "Post-Alarm",
    "Info Active",
    "Post-Info"
};
//tag mappings defined
 private static readonly Dictionary<ushort, string> TAG_MAPPINGS = new()
    {
        { 0x0000, "Not Used" },
        { 0x0001, "Mode" },
        { 0x0004, "Time Remaining (UF)" },
        { 0x0005, "Not Used" },
        { 0x0006, "UF Vol." },
        { 0x0007, "UF Rate" },
        { 0x0012, "dBV" },
        { 0x0013, "Bic. Condo." },
        { 0x0016, "Dialysate Temp." },
        { 0x0019, "Water Press. (Upper)" },
        { 0x001A, "Water Press. (Lower)" },
        { 0x0020, "Treatment Mode" },
        // Add more mappings as needed
    };

    
//method for parsing status byte and flags
    public static (byte RawStatus, bool LogPresent, Dictionary<String, bool> Flags) ParseStatus(byte[] data) {
        byte statusByte = data[18];
        var flags = new Dictionary<string, bool>();
        for (int i = 0; i < STATUS_FLAGS.Length; i++)
        {
            flags[STATUS_FLAGS[i]] = (statusByte & (1 << i)) != 0;
        }
        return (statusByte, (statusByte & 0x80) != 0, flags);
    }
    //method for determining packet type based on command ID
    public static string DeterminePacketType(ushort commandId)
    {
        return commandId switch
        {
            0x0005 => "Log",
            0x0102 or 0x1002 or 0x0200 => "Monitor",
            _ => "Status Unknown"
        };
    }
    //method for parsing payload data based on packet type
    private static List<(ushort TagId, string TagName, short value)> ParsePayLoadData(byte[] data, string packetType, int payloadStart)
    {
        var payload = new List<(ushort, string, short)>();
        if (packetType == "Monitor")
        {
            for (int i = payloadStart; i + 3 < data.Length; i += 4)
            {
                ushort tagId = BitConverter.ToUInt16(data, i);
                short value = BitConverter.ToInt16(data, i + 2);
                payload.Add((tagId, TAG_MAPPINGS.GetValueOrDefault(tagId, $"Unknown (0x{tagId:X4})"), value));

            }
        }
        else if (packetType == "Log")
        {
            payload.Add((0, "Log Note", 0));
        }
        return payload;
    }
// method for parsing the packet and generating a human-readable string
    public static string ParsePacket(string hexString)
    {
        //defining bytes from hexString
        byte[] data = Convert.FromHexString(hexString);
        //defining var status, packetType and payload
        var status=ParseStatus(data);
        string packetType = DeterminePacketType(BitConverter.ToUInt16(data, 14));
        var payload = ParsePayLoadData(data, packetType, 18);
//adding the packet data to a StringBuilder
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("==PACKET HEADER==\n");
        sb.AppendLine($"$Identifier:0x{data[1]}('@')\n");
        sb.AppendLine($"Header Size:{data[2]}bytes\n");
        sb.AppendLine($"$Source Device ID:0x{data[3]}({(char)data[3]}) Dialysis machine international\n");
        sb.AppendLine("Device Version: " + BitConverter.ToUInt16(data, 4) + "\n");
        sb.AppendLine("Serial Number: " + Encoding.ASCII.GetString(new byte[] { data[6], data[8], data[10], data[12] }) + "\n");
        sb.AppendLine("Sequence Number: " + BitConverter.ToUInt16(data, 14) + "\n");
        sb.AppendLine("Command ID: " + BitConverter.ToUInt16(data, 14) + "? Log Data" + "\n");
        sb.AppendLine("==STATUS==\n");
        sb.AppendLine("Raw Status Byte: 0x" + data[18] + "\n");
        sb.AppendLine($"-Log Present: {(status.LogPresent?"Yes":"No")}\n");
        sb.AppendLine("-Device States: ");
        foreach (var flag in status.Flags)
        {
            sb.AppendLine($"  {flag.Key}: {flag.Value}");
        }
        sb.AppendLine("==PATIENT==\n");
        sb.AppendLine("Patient ID (ASCII, 8 bytes)\n");
        sb.AppendLine("---Packet Data---\n");
        sb.AppendLine("Type: " + packetType + "\n");
        if (packetType == "Monitor")
        {
            sb.AppendLine("Monitor Data:");
            foreach (var item in payload)
            {
                sb.AppendLine($"{item.TagName} 0x{item.TagId:X4} : {item.value}");
            }
        }
        else if (packetType == "Log")
        {
            sb.AppendLine("Log Data:");
            foreach (var item in payload)
            {
                sb.AppendLine($"{item.TagName} : {item.value}");
            }
        }
        else
        {
            sb.AppendLine("Unknown Packet Type");
        }
        sb.AppendLine("End Code: " + data[19]);
        return sb.ToString();
    }
    

}
