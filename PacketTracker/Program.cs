 class Program
{
    static void Main()
    {
        string samplePacket = "0108000241424344454647480102030405060708090A0B0C0D0E0F";
        Console.WriteLine(PacketTracker.ParsePacket(samplePacket));
    }
}