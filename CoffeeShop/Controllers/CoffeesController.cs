using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CoffeeShop.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CoffeeShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoffeesController : ControllerBase
    {
        private readonly IConfiguration _config;

        public CoffeesController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] string beanType,
            [FromQuery] string title

            )
        {
            if (beanType == null) beanType = "";
            if (title == null) title = "";


            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, Title, BeanType FROM Coffee
                        WHERE BeanType LIKE '%' + @beanType + '%'
                        AND Title LIKE '%' + @title + '%'";
                    cmd.Parameters.Add(new SqlParameter("@beanType", beanType));
                    cmd.Parameters.Add(new SqlParameter("@title", title));

                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    List<Coffee> coffees = new List<Coffee>();

                    while (await reader.ReadAsync())
                    {
                        Coffee coffee = new Coffee
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Title = reader.GetString(reader.GetOrdinal("Title")),
                            BeanType = reader.GetString(reader.GetOrdinal("BeanType"))
                        };

                        coffees.Add(coffee);
                    }
                    reader.Close();

                    return Ok(coffees);
                }
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    Coffee coffee = null;
                    cmd.CommandText = @"SELECT Id, Title, BeanType FROM Coffee WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        coffee = new Coffee
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Title = reader.GetString(reader.GetOrdinal("Title")),
                            BeanType = reader.GetString(reader.GetOrdinal("BeanType"))
                        };


                    }
                    reader.Close();

                    if (coffee == null) return NotFound($"Coffee with id {id} not found.");

                    return Ok(coffee);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Coffee coffee)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Coffee (Title, BeanType)
                                        OUTPUT INSERTED.Id
                                        VALUES (@title, @beanType)";
                    cmd.Parameters.Add(new SqlParameter("@title", coffee.Title));
                    cmd.Parameters.Add(new SqlParameter("@beanType", coffee.BeanType));

                    try
                    {
                        int newId = (int)await cmd.ExecuteScalarAsync();
                        coffee.Id = newId;
                        return CreatedAtRoute("GetCoffee", new { id = newId });
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, $"An error occurred: \n{ex.Message}");
                    }

                }
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Coffee coffee)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Coffee
                                            SET Title = @title
                                                BeanType = @beanType
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@title", coffee.Title));
                        cmd.Parameters.Add(new SqlParameter("@beanType", coffee.BeanType));
                        cmd.Parameters.Add(new SqlParameter("@id", coffee.Id));

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected.");

                    }
                }
            }
            catch (Exception)
            {
                if (!CoffeeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM Coffee WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!CoffeeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }



        private bool CoffeeExists(int id)
        {
            using (SqlConnection conn =Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id, Title, BeanType
                                            FROM Coffee
                                            WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }
    }
}