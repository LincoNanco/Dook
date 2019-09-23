# Dook!     
.NET Core light object mapper for SQL databases.

## Features

1. Class to database table mapping using decorations (optional).
2. Built in transactions.
3. LINQ to SQL query translation (still working on this).
4. It uses [FastMember](https://github.com/mgravell/fast-member) to assign query column values to object properties.
5. Easy to setup.


## Getting started

### Prerequisites

1. This package was made with .NET Standard 2.0, so .NET Core 2.0+ is required for it to work. You could use this package with other frameworks as .NET Framework, but only its .NET Core implementation will be covered here. You can check framework compatibilities [here](https://docs.microsoft.com/en-us/dotnet/standard/net-standard). 
2. For now, only SQL Server and MySQL databases are supported.

### Installing

You can install this package via [Nuget](https://www.nuget.org/packages/Dook/). 

### Application Setup

First of all, lets create a class. This class will be associated with a table in the database:


```csharp
public class Example : IEntity
{
    public int Id { get; set; }
    public int IntProperty { get; set; }
    public string StringProperty { get; set; }
}
```

You can decorate your class to map it according to your database naming conventions:

```csharp
[TableName("examples")]
public class Example : IEntity
{
    [TableName("id")]
    public int Id { get; set; }
    [TableName("int_property")]
    public int IntProperty { get; set; }
    [TableName("string_property")]
    public string StringProperty { get; set; }
}
```
Second, lets derive a *Context* class. This class is intended to manage the **connection** and **transactions** with a specific database.

```csharp
public class MyContext : Context
{
        public DispatcherContext(DookConfigurationOptions<MyContext> configurationOptions) : base(configurationOptions)
        {

        }
}
```
Lets add an *EntitySet* to *MyContext*. The *EntitySet* class is responsible for making **CRUD** operations over a single database table. In this case, we will add an *EntitySet* to manage *Example* table.

```csharp
public class MyContext : Context
{
        //Always declare your EntitySets as properties
        public EntitySet<Example> ExampleRepository { get; set; }

        public DispatcherContext(DookConfigurationOptions<MyContext> configurationOptions) : base(configurationOptions)
        {
            ExampleRepository = new EntitySet<Example>(QueryProvider);
        }
}
```
Now, lets configure *Dook* into our application. In *Startup.cs*, add the following inside the *ConfigureServices* method:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    //..other configuration code..
    services.AddDookContext<MyContext>(options => {
        options.ConnectionString = "YourConnectionString";
        //Here you choose which kind of database will you use.
        options.DatabaseType = DbType.MySql;
    });
    //.. more configuration code here
}
```

With all the above done, you are ready to write queries and commands to database.

#### Querying

```csharp
List<Example> examples = _context.ExampleRepository.Where(x => x.IntProperty >= 3).OrderByDescending(x => x.StringProperty).ToList();
```
**Important**: the alias you use in the lambda expressions (int this case, *x*) will be the same used in the resulting query. Keep it consistent!

#### Inserting data

```csharp
_context.ExampleRepository.Insert(example);
_context.SaveChanges();
```
#### Updating data

```csharp
_context.ExampleRepository.Update(example);
_context.SaveChanges();
```

#### Deleting data

```csharp
_context.ExampleRepository.Delete(example);
_context.SaveChanges();
```
**Important**: **insert**, **update** and **delete** commands are run inside a transaction. Always remember to call the *SaveChanges()*. Otherwise, the transaction will be rolled back.

### 

## Versioning

Using [SemVer](http://semver.org/) for versioning. <!-- For the versions available, see the [tags on this repository](https://github.com/your/project/tags). -->

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

Part of the code is based on Matt Warren's work: https://blogs.msdn.microsoft.com/mattwar/2008/11/18/linq-building-an-iqueryable-provider-series/.
