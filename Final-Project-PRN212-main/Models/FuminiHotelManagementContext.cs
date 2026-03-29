using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Main_Project.Models;

public partial class FuminiHotelManagementContext : DbContext
{

    public static FuminiHotelManagementContext INSTANCE = new FuminiHotelManagementContext();
    
    public FuminiHotelManagementContext()
    {
        if(INSTANCE == null)
        {
            INSTANCE = this;
        }
    }

    public FuminiHotelManagementContext(DbContextOptions<FuminiHotelManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BookingDetail> BookingDetails { get; set; }

    public virtual DbSet<BookingReservation> BookingReservations { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<RoomInformation> RoomInformations { get; set; }

    public virtual DbSet<RoomType> RoomTypes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(ResolveWorkingConnectionString(config));
        }
    }

    private static string ResolveWorkingConnectionString(IConfigurationRoot config)
    {
        var baseConnection = config.GetConnectionString("value")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:value in appsettings.json.");

        var baseBuilder = new SqlConnectionStringBuilder(baseConnection)
        {
            ConnectTimeout = 2
        };

        var databaseName = baseBuilder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("ConnectionStrings:value must include Initial Catalog.");
        }

        var servers = config
            .GetSection("ConnectionStrings:servers")
            .GetChildren()
            .Select(s => s.Value)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        var candidates = servers
            .Concat(new[] { Environment.MachineName })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var server in candidates)
        {
            var candidateBuilder = new SqlConnectionStringBuilder(baseBuilder.ConnectionString)
            {
                DataSource = server
            };

            if (CanConnectToDatabase(candidateBuilder, databaseName))
            {
                return candidateBuilder.ConnectionString;
            }
        }

        throw new InvalidOperationException(
            "Cannot find SQL Server instance containing database 'FUMiniHotelManagement'. " +
            "Update ConnectionStrings:servers in appsettings.json or create the database from FUMiniHotelManagement.sql.");
    }

    private static bool CanConnectToDatabase(SqlConnectionStringBuilder builder, string databaseName)
    {
        try
        {
            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();

            using var command = new SqlCommand("SELECT DB_ID(@dbName)", connection);
            command.Parameters.AddWithValue("@dbName", databaseName);

            var result = command.ExecuteScalar();
            return result != DBNull.Value && result != null;
        }
        catch
        {
            return false;
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookingDetail>(entity =>
        {
            entity.HasKey(e => new { e.BookingReservationId, e.RoomId });

            entity.ToTable("BookingDetail");

            entity.Property(e => e.BookingReservationId).HasColumnName("BookingReservationID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.ActualPrice).HasColumnType("money");

            entity.HasOne(d => d.BookingReservation).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.BookingReservationId)
                .HasConstraintName("FK_BookingDetail_BookingReservation");

            entity.HasOne(d => d.Room).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK_BookingDetail_RoomInformation");
        });

        modelBuilder.Entity<BookingReservation>(entity =>
        {
            entity.ToTable("BookingReservation");

            entity.Property(e => e.BookingReservationId)
                .ValueGeneratedNever()
                .HasColumnName("BookingReservationID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.TotalPrice).HasColumnType("money");

            entity.HasOne(d => d.Customer).WithMany(p => p.BookingReservations)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_BookingReservation_Customer");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customer");

            entity.HasIndex(e => e.EmailAddress, "UQ__Customer__49A147406DE9798A").IsUnique();

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.CustomerFullName).HasMaxLength(50);
            entity.Property(e => e.EmailAddress).HasMaxLength(50);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.Telephone).HasMaxLength(12);
        });

        modelBuilder.Entity<RoomInformation>(entity =>
        {
            entity.HasKey(e => e.RoomId);

            entity.ToTable("RoomInformation");

            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.RoomDetailDescription).HasMaxLength(220);
            entity.Property(e => e.RoomNumber).HasMaxLength(50);
            entity.Property(e => e.RoomPricePerDay).HasColumnType("money");
            entity.Property(e => e.RoomTypeId).HasColumnName("RoomTypeID");

            entity.HasOne(d => d.RoomType).WithMany(p => p.RoomInformations)
                .HasForeignKey(d => d.RoomTypeId)
                .HasConstraintName("FK_RoomInformation_RoomType");
        });

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.ToTable("RoomType");

            entity.Property(e => e.RoomTypeId).HasColumnName("RoomTypeID");
            entity.Property(e => e.RoomTypeName).HasMaxLength(50);
            entity.Property(e => e.TypeDescription).HasMaxLength(250);
            entity.Property(e => e.TypeNote).HasMaxLength(250);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
