using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;

var factory = new OrderbookContextFactory();
using var context = factory.CreateDbContext(args);

var allLinesOrders = await File.ReadAllLinesAsync(args[2]);
var linesOrders = allLinesOrders.Skip(1).Select(l => l.Split('\t')).ToList();

var allLinesCustomers = await File.ReadAllLinesAsync(args[1]);
var linesCustomers = allLinesCustomers.Skip(1).Select(l => l.Split('\t')).ToList();

if (args[0] == "import")
{
    for (int i = 0; i < linesCustomers.Count(); i++)
    {
        var customer = new Customer { Name = linesCustomers[i][0], CreditLimit = Convert.ToDecimal(linesCustomers[i][1]), Orders = linesOrders.Where(l => l[0] == linesCustomers[i][0]).Select(l => new Order { OrderDate = Convert.ToDateTime(l[1]), OrderValue = Convert.ToDecimal(l[2])}).ToList() };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
    }
    
}
if(args[0] == "clean")
{
    context.Customers.RemoveRange(context.Customers);
    context.Orders.RemoveRange(context.Orders);

    await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT('Orders', RESEED, 0)");
    await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT('Customers', RESEED, 0)");

    await context.SaveChangesAsync();
}
if (args[0] == "check")
{
    Console.WriteLine("Print where Order Values are greater then credit limit");

    var creditLimit = context.Customers.Where(l => l.Orders.Sum(x => x.OrderValue) > l.CreditLimit);
    foreach (var item in creditLimit)
    {
        Console.WriteLine($"{item.Name}");
    }
}
if (args[0] == "full")
{
    //clean
    context.Customers.RemoveRange(context.Customers);
    context.Orders.RemoveRange(context.Orders);

    await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT('Orders', RESEED, 0)");
    await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT('Customers', RESEED, 0)");

    await context.SaveChangesAsync();
    //import
    for (int i = 0; i < linesCustomers.Count(); i++)
    {
        var customer = new Customer { Name = linesCustomers[i][0], CreditLimit = Convert.ToDecimal(linesCustomers[i][1]), Orders = linesOrders.Where(l => l[0] == linesCustomers[i][0]).Select(l => new Order { OrderDate = Convert.ToDateTime(l[1]), OrderValue = Convert.ToDecimal(l[2]) }).ToList() };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
    }
    //check
    Console.WriteLine("Print where Order Values are greater then credit limit");

    var creditLimit = context.Customers.Where(l => l.Orders.Sum(x => x.OrderValue) > l.CreditLimit);
    foreach (var item in creditLimit)
    {
        Console.WriteLine($"{item.Name}");
    }

}



Console.ReadKey();

//Create the model class
class Customer
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(8, 2)")]
    public decimal CreditLimit { get; set; }

    public List<Order> Orders { get; set; } = new();
}

class Order
{
    public int Id { get; set; }
    public Customer? Customer { get; set; }
    public int CustomerId { get; set; }

    public DateTime OrderDate { get; set; }

    [Column(TypeName = "decimal(8, 2)")]
    public decimal OrderValue { get; set; }

}

class OrderbookContext : DbContext
{
    public DbSet<Order> Orders { get; set; }

    public DbSet<Customer> Customers { get; set; }
    public OrderbookContext(DbContextOptions<OrderbookContext> options)

    : base(options)
    {

    }
}

class OrderbookContextFactory : IDesignTimeDbContextFactory<OrderbookContext>
{
    public OrderbookContext CreateDbContext(string[]? args = null)
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        var optionsBuilder = new DbContextOptionsBuilder<OrderbookContext>();
        optionsBuilder
            // Uncomment the following line if you want to print generated
            // SQL statements on the console.
           // .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
            .UseSqlServer(configuration["ConnectionStrings:DefaultConnection"]);

        return new OrderbookContext(optionsBuilder.Options);
    }
}
