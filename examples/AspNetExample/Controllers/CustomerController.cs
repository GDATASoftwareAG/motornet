using System.Collections.Generic;
using AspNetExample.Models;
using AspNetExample.Persistency;
using Microsoft.AspNetCore.Mvc;

namespace AspNetExample.Controllers;

[ApiController]
[Route("api/v1/customers")]
public class CustomerController(ICustomerValidator customerValidator, IStorage<Customer> db) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<Customer>> GetAll() => Ok(db.GetAll());

    [HttpGet("{id:int}")]
    public ActionResult<Customer> GetById(long id)
    {
        var customer = db.Get(id);
        return customer == null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public ActionResult<Customer> Create([FromBody] Customer customer)
    {
        if (!customerValidator.Validate(customer))
        {
            return BadRequest(customer);
        }
        var addedId = db.Add(customer);
        return CreatedAtAction(nameof(Create), new { id = addedId }, customer);
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(long id, [FromBody] Customer updatedCustomer)
    {
        var numAffected = db.Update(
            customer => customer.Id == id,
            customer =>
            {
                customer.FirstName = updatedCustomer.FirstName;
                customer.LastName = updatedCustomer.LastName;
                customer.Age = updatedCustomer.Age;
            }
        );

        return numAffected == 0 ? NotFound() : NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var numAffected = db.Delete(id);
        return numAffected == 0 ? NotFound() : NoContent();
    }
}
