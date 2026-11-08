using System.Windows;
using NADI.Core.Models;

namespace NADI.App.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void btnLogin_Click(object sender, RoutedEventArgs e)
    {
        Employee employee = new Employee(tbUsername.Text, tbPassword.Password);
        if (employee.Login())
        {
            MessageBox.Show("Login Berhasil, ID anda adalah " + employee.EmployeeID.ToString());
        }
        else
        {
            MessageBox.Show("Login Gagal");
        }
    }
}
