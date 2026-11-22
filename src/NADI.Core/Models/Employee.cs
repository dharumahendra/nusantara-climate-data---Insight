namespace NADI.Core.Models;

public class Employee
{
    public int EmployeeID { get; set; }
    public string LoginName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Definisikan Constructor tanpa parameter
    public Employee() { }

    // Constructor dengan parameter (Lab 7.2)
    public Employee(string loginName, string password)
    {
        LoginName = loginName;
        Password = password;
    }

    // Method Login dengan return type boolean
    public bool Login()
    {
        if (LoginName == "Jono" && Password == "wkkw")
        {
            EmployeeID = 1;
            return true;
        }
        else if (LoginName == "Jones" && Password == "haha")
        {
            EmployeeID = 2;
            return true;
        }
        else
        {
            return false;
        }
    }
}
