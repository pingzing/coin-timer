namespace CoinTimer
{
    public class CoinbaseResponse
    {
        public CoinbaseResponseData Data { get; set; }
    }

    public class CoinbaseResponseData
    {
        public string Amount { get; set; }
        public string Currency { get; set; }
    }
}
