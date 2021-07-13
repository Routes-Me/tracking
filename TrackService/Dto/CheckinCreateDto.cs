namespace TrackService.Dtos
{
    public class CheckinCreateDto
    {
        public string ConnectionId { get; set; }
        public string VehicleId { get; set; }
        public string InstitutionId { get; set; }
        public string Kind { get; set; }
        public double CheckedAt { get; set; }
    }
}