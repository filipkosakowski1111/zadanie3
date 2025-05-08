namespace WebApplication1.Modules


{
    public class ClientTripDto
    {
        public TripDetailsDto Trip { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool PaymentMade { get; set; }
    }
}