using System.Collections.Generic;
using System.Linq;
using AspNetExample.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspNetExample.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CustomerController(ICustomerValidator customerValidator) : ControllerBase
{
    private readonly List<Customer> _customers = [];

    [HttpGet]
    public ActionResult<IEnumerable<Customer>> GetAll() => Ok(_customers);

    [HttpGet("{id:int}")]
    public ActionResult<Customer> GetById(long id)
    {
        var customer = _customers.FirstOrDefault(p => p.Id == id);
        return customer == null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public ActionResult<Customer> Create([FromBody] Customer customer)
    {
        if (!customerValidator.Validate(customer))
        {
            return BadRequest(customer);
        }
        customer.Id = _customers.Max(p => p.Id) + 1;
        _customers.Add(customer);
        return CreatedAtAction(nameof(Create), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(long id, [FromBody] Customer updatedCustomer)
    {
        var customer = _customers.FirstOrDefault(p => p.Id == id);
        if (customer == null)
        {
            return NotFound();
        }

        customer.FirstName = updatedCustomer.FirstName;
        customer.LastName = updatedCustomer.LastName;
        customer.Age = updatedCustomer.Age;
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var customer = _customers.FirstOrDefault(p => p.Id == id);
        if (customer == null)
        {
            return NotFound();
        }

        _customers.Remove(customer);
        return NoContent();
    }
}
