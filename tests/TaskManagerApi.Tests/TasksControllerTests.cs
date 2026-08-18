using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagerApi.Controllers;
using TaskManagerApi.Data;
using TaskManagerApi.Models;

namespace TaskManagerApi.Tests;

public class TasksControllerTests
{
    // كل تست بياخد داتابيز InMemory جديدة تمامًا وباسم مختلف
    // عشان التستات متأثرش في بعضها ولا تشترك في نفس البيانات
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoTasksExist()
    {
        using var context = CreateContext();
        var controller = new TasksController(context);

        var result = await controller.GetAll();

        var okResult = Assert.IsType<ActionResult<IEnumerable<TaskItem>>>(result, exactMatch: false);
        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskItem>>(okResult.Value);
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task GetAll_ReturnsAllTasks_OrderedByNewestFirst()
    {
        using var context = CreateContext();
        context.Tasks.AddRange(
            new TaskItem { Title = "قديمة", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new TaskItem { Title = "حديثة", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        var result = await controller.GetAll();

        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskItem>>(result.Value).ToList();
        Assert.Equal(2, tasks.Count);
        Assert.Equal("حديثة", tasks.First().Title); // الأحدث لازم يظهر أول واحد
    }

    [Fact]
    public async Task GetById_ReturnsTask_WhenTaskExists()
    {
        using var context = CreateContext();
        var task = new TaskItem { Title = "مهمة موجودة" };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        var result = await controller.GetById(task.Id);

        Assert.Equal("مهمة موجودة", result.Value?.Title);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenTaskDoesNotExist()
    {
        using var context = CreateContext();
        var controller = new TasksController(context);

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_AddsNewTask_AndReturnsCreatedAtAction()
    {
        using var context = CreateContext();
        var controller = new TasksController(context);
        var newTask = new TaskItem { Title = "مهمة جديدة", Description = "وصف" };

        var result = await controller.Create(newTask);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var savedTask = Assert.IsType<TaskItem>(createdResult.Value);
        Assert.Equal("مهمة جديدة", savedTask.Title);
        Assert.NotEqual(0, savedTask.Id); // الداتابيز لازم تكون ولّدت Id تلقائي
        Assert.Single(context.Tasks); // اتأكد إنها فعلاً اتخزنت في الداتابيز
    }

    [Fact]
    public async Task Create_IgnoresClientProvidedId_AndAssignsNewOne()
    {
        // اختبار مهم أمنيًا: التأكد إن حد مش هيقدر يفرض Id معين عن قصد
        using var context = CreateContext();
        var controller = new TasksController(context);
        var maliciousTask = new TaskItem { Id = 9999, Title = "محاولة فرض Id" };

        var result = await controller.Create(maliciousTask);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var savedTask = Assert.IsType<TaskItem>(createdResult.Value);
        Assert.NotEqual(9999, savedTask.Id);
    }

    [Fact]
    public async Task Update_ModifiesExistingTask_WhenIdsMatch()
    {
        using var context = CreateContext();
        var task = new TaskItem { Title = "قبل التعديل", IsCompleted = false };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        var updatedTask = new TaskItem { Id = task.Id, Title = "بعد التعديل", IsCompleted = true };
        var result = await controller.Update(task.Id, updatedTask);

        Assert.IsType<NoContentResult>(result);
        var fromDb = await context.Tasks.FindAsync(task.Id);
        Assert.Equal("بعد التعديل", fromDb!.Title);
        Assert.True(fromDb.IsCompleted);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenIdsDoNotMatch()
    {
        using var context = CreateContext();
        var controller = new TasksController(context);
        var task = new TaskItem { Id = 1, Title = "مهمة" };

        var result = await controller.Update(2, task); // id في الـroute مختلف عن id في الـbody

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenTaskDoesNotExist()
    {
        using var context = CreateContext();
        var controller = new TasksController(context);
        var task = new TaskItem { Id = 999, Title = "غير موجودة" };

        var result = await controller.Update(999, task);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_RemovesTask_WhenTaskExists()
    {
        using var context = CreateContext();
        var task = new TaskItem { Title = "هتتمسح" };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        var controller = new TasksController(context);

        var result = await controller.Delete(task.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.Tasks);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenTaskDoesNotExist()
    {
        using var context = CreateContext();
        var controller = new TasksController(context);

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }
}
