using System.Configuration;
using System.Data;
using System.Windows;
using Main_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Question2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            try
            {
                using (var context = new FuminiHotelManagementContext())
                {
                    context.Database.EnsureCreated();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database initialization warning: {ex.Message}", "Warning");
            }
        }
    }

}
