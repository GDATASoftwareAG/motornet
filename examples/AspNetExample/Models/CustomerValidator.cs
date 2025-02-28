namespace AspNetExample.Models;

public interface ICustomerValidator
{
    public bool Validate(Customer customer);
}

public class AtLeast18Validator : ICustomerValidator
{
    public bool Validate(Customer customer) => customer.Age >= 18;
}
