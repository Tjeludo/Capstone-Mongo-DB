namespace App_v1.Models;

public class User
{
    public string StaffId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Optional constructor
    public User()
    {
    }

    public User(string staffId, string firstName, string lastName, string email)
    {
        StaffId = staffId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
    }
}
