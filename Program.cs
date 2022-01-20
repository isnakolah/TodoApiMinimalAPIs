using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

app.MapGet("/", () =>
{
    return "Hello";
});

app.MapGet("/todoitems", async (TodoDb db) =>
{
    var todoItems = await db.Todos
        .Select(x => x.ToDTO())
        .ToArrayAsync();

    return todoItems;
});


app.MapGet("/todoitems/complete", async (TodoDb db) =>
{
    var todoItems = await db.Todos
        .Where(i => i.IsComplete)
        .Select(i => i.ToDTO())
        .ToArrayAsync();

    return todoItems;
});


app.MapGet("/todoitems/{id}", async (int id, TodoDb db) =>
{
    return await db.Todos.FindAsync(id) is Todo todo ? Results.Ok(todo.ToDTO()) : Results.NotFound();
});

app.MapPost("/todoitems", async (TodoItemDTO todoItemDTO, TodoDb db) =>
{
    if (string.IsNullOrWhiteSpace(todoItemDTO.Name))
        return Results.BadRequest();

    var todoItem = new Todo(todoItemDTO.Name, todoItemDTO.IsComplete);

    db.Todos.Add(todoItem);

    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todoItem.Id}", todoItem.ToDTO());
});

app.MapPut("/todoitems/{id}", async (int id, TodoItemDTO inputTodo, TodoDb db) =>
{
    if (await db.Todos.FindAsync(id) is not Todo todo)
        return Results.NotFound();

    if (!string.IsNullOrWhiteSpace(inputTodo.Name))
        todo.Name = inputTodo.Name;

    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/todoitems/{id}", async (int id, TodoDb db) =>
{
    if (await db.Todos.FindAsync(id) is not Todo todo)
        return Results.NotFound();

    db.Todos.Remove(todo);

    await db.SaveChangesAsync();

    return Results.Ok(todo.ToDTO());
});

app.Run();

sealed record class Todo
{
    public Todo()
    {
    }

    public Todo(string name, bool isComplete)
    {
        Name = name;
        IsComplete = isComplete;
    }

    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
    public string? Secret { get; set; }

    public TodoItemDTO ToDTO() => new(this);
}

sealed record class TodoItemDTO(int Id, string? Name, bool IsComplete)
{
    public TodoItemDTO(Todo todoItem) : this(todoItem.Id, todoItem.Name, todoItem.IsComplete)
    {
    }
}

sealed class TodoDb : DbContext
{
    public TodoDb(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Todo> Todos => Set<Todo>();
}