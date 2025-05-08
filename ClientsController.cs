using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using WebApplication1.Modules;

namespace WebApplication1.Models
{
    [ApiController]
    [Route("api/clients")]
    public class ClientsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ClientsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public IActionResult CreateClient([FromBody] Client client)
        {
            if (string.IsNullOrWhiteSpace(client.FirstName) ||
                string.IsNullOrWhiteSpace(client.LastName) ||
                string.IsNullOrWhiteSpace(client.Email) ||
                string.IsNullOrWhiteSpace(client.Telephone) ||
                string.IsNullOrWhiteSpace(client.Pesel))
            {
                return BadRequest("All fields are required.");
            }

            if (!Regex.IsMatch(client.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequest("Invalid email format.");

            if (!Regex.IsMatch(client.Pesel, @"^\d{11}$"))
                return BadRequest("Invalid PESEL format.");

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            var cmd = new SqlCommand(@"INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                                       OUTPUT INSERTED.IdClient
                                       VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)", conn);

            cmd.Parameters.AddWithValue("@FirstName", client.FirstName);
            cmd.Parameters.AddWithValue("@LastName", client.LastName);
            cmd.Parameters.AddWithValue("@Email", client.Email);
            cmd.Parameters.AddWithValue("@Telephone", client.Telephone);
            cmd.Parameters.AddWithValue("@Pesel", client.Pesel);

            int newId = (int)cmd.ExecuteScalar();
            return Created($"/api/clients/{newId}", new { IdClient = newId });
        }

        [HttpPut("{id}/trips/{tripId}")]
        public IActionResult RegisterToTrip(int id, int tripId)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Client WHERE IdClient = @id", conn);
            checkCmd.Parameters.AddWithValue("@id", id);
            if ((int)checkCmd.ExecuteScalar() == 0) return NotFound("Client not found");

            checkCmd = new SqlCommand("SELECT COUNT(*) FROM Trip WHERE IdTrip = @tripId", conn);
            checkCmd.Parameters.AddWithValue("@tripId", tripId);
            if ((int)checkCmd.ExecuteScalar() == 0) return NotFound("Trip not found");

            var existsCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", conn);
            existsCmd.Parameters.AddWithValue("@IdClient", id);
            existsCmd.Parameters.AddWithValue("@IdTrip", tripId);
            if ((int)existsCmd.ExecuteScalar() > 0) return Conflict("Client already registered to this trip.");

            var maxCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @tripId", conn);
            maxCmd.Parameters.AddWithValue("@tripId", tripId);
            int currentCount = (int)maxCmd.ExecuteScalar();

            var getMax = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @tripId", conn);
            getMax.Parameters.AddWithValue("@tripId", tripId);
            int maxPeople = (int)getMax.ExecuteScalar();

            if (currentCount >= maxPeople) return Conflict("Trip full");

            var insertCmd = new SqlCommand("INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) VALUES (@IdClient, @IdTrip, GETDATE())", conn);
            insertCmd.Parameters.AddWithValue("@IdClient", id);
            insertCmd.Parameters.AddWithValue("@IdTrip", tripId);
            insertCmd.ExecuteNonQuery();

            return Ok("Client registered to trip.");
        }

        [HttpDelete("{id}/trips/{tripId}")]
        public IActionResult UnregisterFromTrip(int id, int tripId)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId", conn);
            checkCmd.Parameters.AddWithValue("@id", id);
            checkCmd.Parameters.AddWithValue("@tripId", tripId);

            if ((int)checkCmd.ExecuteScalar() == 0)
                return NotFound("Client not registered for this trip.");

            var deleteCmd = new SqlCommand("DELETE FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId", conn);
            deleteCmd.Parameters.AddWithValue("@id", id);
            deleteCmd.Parameters.AddWithValue("@tripId", tripId);
            deleteCmd.ExecuteNonQuery();

            return Ok("Client removed from trip.");
        }
    }
}
