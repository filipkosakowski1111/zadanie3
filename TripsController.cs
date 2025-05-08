using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebApplication1.Modules;

namespace WebApplication1.Models
{
    [ApiController]
    [Route("api/trips")]
    public class TripsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TripsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult GetTrips()
        {
            var trips = new List<TripDetailsDto>();

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();
            var cmd = new SqlCommand(@"SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                                              c.Name AS Country
                                       FROM Trip t
                                       JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                                       JOIN Country c ON ct.IdCountry = c.IdCountry", conn);

            using var reader = cmd.ExecuteReader();
            var tripDict = new Dictionary<int, TripDetailsDto>();

            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                if (!tripDict.ContainsKey(id))
                {
                    tripDict[id] = new TripDetailsDto
                    {
                        IdTrip = id,
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        DateFrom = reader.GetDateTime(3),
                        DateTo = reader.GetDateTime(4),
                        MaxPeople = reader.GetInt32(5),
                        Countries = new List<string>()
                    };
                }
                tripDict[id].Countries.Add(reader.GetString(6));
            }

            return Ok(tripDict.Values);
        }

        [HttpGet("/api/clients/{id}/trips")]
        public IActionResult GetClientTrips(int id)
        {
            var trips = new List<ClientTripDto>();

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();
            var cmd = new SqlCommand(@"SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                                              ct.RegisteredAt, ct.PaymentDate,
                                              c.Name AS Country
                                       FROM Client_Trip ct
                                       JOIN Trip t ON ct.IdTrip = t.IdTrip
                                       JOIN Country_Trip ctr ON t.IdTrip = ctr.IdTrip
                                       JOIN Country c ON ctr.IdCountry = c.IdCountry
                                       WHERE ct.IdClient = @IdClient", conn);
            cmd.Parameters.AddWithValue("@IdClient", id);

            using var reader = cmd.ExecuteReader();
            var tripDict = new Dictionary<int, ClientTripDto>();

            while (reader.Read())
            {
                int tripId = reader.GetInt32(0);
                if (!tripDict.ContainsKey(tripId))
                {
                    tripDict[tripId] = new ClientTripDto
                    {
                        RegisteredAt = reader.GetDateTime(6),
                        PaymentMade = !reader.IsDBNull(7),
                        Trip = new TripDetailsDto
                        {
                            IdTrip = tripId,
                            Name = reader.GetString(1),
                            Description = reader.GetString(2),
                            DateFrom = reader.GetDateTime(3),
                            DateTo = reader.GetDateTime(4),
                            MaxPeople = reader.GetInt32(5),
                            Countries = new List<string>()
                        }
                    };
                }
                tripDict[tripId].Trip.Countries.Add(reader.GetString(8));
            }

            return Ok(tripDict.Values);
        }
    }
}
